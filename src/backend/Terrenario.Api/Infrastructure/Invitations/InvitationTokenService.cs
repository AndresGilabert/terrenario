using System.Security.Cryptography;
using System.Text;
using Terrenario.Api.Infrastructure.Tokens;

namespace Terrenario.Api.Infrastructure.Invitations;

/// <summary>
/// Mismo esquema que los refresh tokens (MVP-101): 256 bits de entropía en base64url y
/// SHA-256 en reposo. Sirve a los dos enlaces de un solo uso del producto —invitación (MVP-103) y
/// reactivación de Workspace (MVP-206)—: implementa el puerto neutro
/// <see cref="IOneTimeTokenService"/> además del histórico <see cref="IInvitationTokenService"/>.
/// </summary>
public sealed class InvitationTokenService : IInvitationTokenService, IOneTimeTokenService
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

    OneTimeToken IOneTimeTokenService.Generate()
    {
        var token = Generate();
        return new OneTimeToken(token.Value, token.Hash);
    }
}
