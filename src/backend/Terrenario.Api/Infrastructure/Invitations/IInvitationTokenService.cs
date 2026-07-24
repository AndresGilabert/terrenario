namespace Terrenario.Api.Infrastructure.Invitations;

/// <summary>
/// El valor en claro solo existe en la respuesta de creación y en el enlace que recibe la
/// persona invitada; en base de datos vive únicamente su hash.
/// </summary>
public sealed record InvitationToken(string Value, string Hash);

public interface IInvitationTokenService
{
    InvitationToken Generate();

    string Hash(string token);
}
