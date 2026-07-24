using System.Security.Cryptography;
using System.Text;

namespace Terrenario.Api.Infrastructure.Invitations;

/// <summary>
/// Mismo esquema que los refresh tokens (MVP-101): 256 bits de entropía en base64url y
/// SHA-256 en reposo.
/// </summary>
public sealed class InvitationTokenService : IInvitationTokenService
{
    private const int TokenBytes = 32;

    public InvitationToken Generate()
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return new InvitationToken(rawToken, Hash(rawToken));
    }

    public string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
