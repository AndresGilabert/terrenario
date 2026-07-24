namespace Terrenario.Api.Infrastructure.Invitations;

public sealed record InvitationEmail(
    string ToEmail,
    string WorkspaceName,
    string? InviterDisplayName,
    string AcceptUrl);

/// <summary>
/// Puerto hacia el <c>email-service</c> de <c>docs/02-arquitectura/componentes.md</c>. El MVP
/// habla SMTP genérico (ADR-0010); un proveedor con API propia entraría como adaptador nuevo sin
/// tocar el caso de uso.
/// </summary>
public interface IInvitationEmailSender
{
    /// <summary>
    /// <c>false</c> mientras no haya cuenta de envío configurada. La API lo refleja en
    /// <c>email_sent</c> en lugar de dar por enviado un correo que nunca salió.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>Debe lanzar si el envío falla: el caso de uso lo traduce a <c>email_sent: false</c>.</summary>
    Task SendAsync(InvitationEmail message, CancellationToken ct = default);
}
