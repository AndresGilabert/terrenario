using System.Security.Cryptography;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Infrastructure.Data;

namespace Terrenario.Api.Infrastructure.Auth;

public sealed class RefreshTokenStore : IRefreshTokenStore
{
    private readonly TerrenarioDbContext _db;
    private readonly RefreshTokenOptions _options;

    public RefreshTokenStore(TerrenarioDbContext db, IOptions<RefreshTokenOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<string> CreateAsync(Guid userId, CancellationToken ct = default)
    {
        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);

        var entity = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(_options.LifetimeSeconds),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(ct);

        return rawToken;
    }

    public async Task<Guid> ValidateAndRotateAsync(string token, CancellationToken ct = default)
    {
        var tokenHash = HashToken(token);

        var entity = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt =>
                rt.TokenHash == tokenHash &&
                rt.RevokedAt == null &&
                rt.ExpiresAt > DateTimeOffset.UtcNow, ct);

        if (entity is null)
            throw new RefreshTokenException(ErrorCodes.AuthRefreshTokenInvalid);

        entity.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return entity.UserId;
    }

    public async Task RevokeAsync(string token, CancellationToken ct = default)
    {
        var tokenHash = HashToken(token);

        var entity = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.RevokedAt == null, ct);

        if (entity is not null)
        {
            entity.RevokedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class RefreshTokenOptions
{
    public const string SectionName = "Auth:RefreshToken";

    public int LifetimeSeconds { get; set; } = 2_592_000; // 30 days
}

public sealed class RefreshTokenException(string errorCode)
    : Exception("Refresh token no válido o expirado.")
{
    public string ErrorCode { get; } = errorCode;
}

public sealed class RefreshTokenEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
