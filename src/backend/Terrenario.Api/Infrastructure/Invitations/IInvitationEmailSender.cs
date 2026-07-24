namespace Terrenario.Api.Infrastructure.Invitations;

public sealed record InvitationEmail(
    string ToEmail,
    string WorkspaceName,
    string? InviterDisplayName,
    string AcceptUrl);

/// <summary>
/// Puerto hacia el <c>email-service</c> de <c>docs/02-arquitectura/componentes.md</c>. El MVP
/// todavía no tiene proveedor decidido, así que la implementación real llegará como adaptador
/// nuevo sin tocar el caso de uso. Debe lanzar si el envío falla.
/// </summary>
public interface IInvitationEmailSender
{
    Task SendAsync(InvitationEmail message, CancellationToken ct = default);
}
