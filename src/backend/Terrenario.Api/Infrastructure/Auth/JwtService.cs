using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Terrenario.Api.Infrastructure.Auth;

public sealed class JwtService : IJwtService
{
    private readonly JwtOptions _options;
    private readonly RsaSecurityKey _privateKey;
    private readonly RsaSecurityKey _publicKey;

    public JwtService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        var rsaForSign = RSA.Create();
        rsaForSign.ImportFromPem(_options.PrivateKeyPem);
        _privateKey = new RsaSecurityKey(rsaForSign);

        // Build the validation key from the public key parameters of the private key
        var rsaForVerify = RSA.Create();
        rsaForVerify.ImportRSAPublicKey(rsaForSign.ExportRSAPublicKey(), out _);
        _publicKey = new RsaSecurityKey(rsaForVerify);
    }

    public IssuedAccessToken IssueAccessToken(Guid userId, string? displayName, Guid? workspaceId = null)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddSeconds(_options.AccessTokenLifetimeSeconds);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrWhiteSpace(displayName))
            claims.Add(new(JwtRegisteredClaimNames.Name, displayName));

        if (workspaceId.HasValue)
            claims.Add(new(TerrenarioClaims.WorkspaceId, workspaceId.Value.ToString()));

        var signingCredentials = new SigningCredentials(_privateKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new IssuedAccessToken(tokenString, _options.AccessTokenLifetimeSeconds);
    }

    public Guid? ValidateAccessToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = _privateKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        try
        {
            var principal = handler.ValidateToken(token, validationParams, out _);
            // JwtSecurityTokenHandler may map "sub" to the long-form claim type
            var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(subClaim, out var userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }
}

public sealed class JwtOptions
{
    public const string SectionName = "Auth:Jwt";

    public string Issuer { get; set; } = "terrenario-api";
    public string Audience { get; set; } = "terrenario-web";
    public int AccessTokenLifetimeSeconds { get; set; } = 900;
    public string PrivateKeyPem { get; set; } = string.Empty;
    public string PublicKeyPem { get; set; } = string.Empty;
}
