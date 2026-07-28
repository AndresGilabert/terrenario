namespace Terrenario.Api.Infrastructure.Tokens;

/// <summary>
/// Token de un solo uso: el valor en claro solo existe en el enlace que recibe la persona
/// destinataria; en base de datos vive únicamente su hash.
/// </summary>
public sealed record OneTimeToken(string Value, string Hash);

/// <summary>
/// Emisión y verificación de tokens de enlace de un solo uso (256 bits de entropía en base64url y
/// SHA-256 en reposo). Nació con las invitaciones (MVP-103) y lo reutiliza el enlace de
/// reactivación de Workspace (MVP-206): mismo esquema, mismas garantías, una sola implementación.
/// </summary>
public interface IOneTimeTokenService
{
    OneTimeToken Generate();

    string Hash(string token);
}
