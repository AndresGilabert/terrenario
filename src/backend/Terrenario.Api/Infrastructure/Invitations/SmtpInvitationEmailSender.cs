using Terrenario.Api.Infrastructure.Email;

namespace Terrenario.Api.Infrastructure.Invitations;

/// <summary>
/// Adaptador real del <c>email-service</c> para las invitaciones: compone el mensaje y lo entrega al
/// transporte SMTP común (<see cref="SmtpMailer"/>, ADR-0010). Las excepciones se propagan a
/// propósito; el caso de uso las traduce a <c>email_sent: false</c> sin invalidar la invitación.
/// </summary>
public sealed class SmtpInvitationEmailSender(SmtpMailer mailer, ProductEmailTemplate template)
    : IInvitationEmailSender
{
    public bool IsEnabled => mailer.IsEnabled;

    public async Task SendAsync(InvitationEmail message, CancellationToken ct = default)
    {
        var mail = InvitationEmailComposer.Compose(template, message);
        await mailer.SendAsync(mail, "invitación a Workspace", ct);
    }
}
