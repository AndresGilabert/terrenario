using MailKit.Net.Smtp;
using MailKit.Security;

namespace Terrenario.Api.Infrastructure.Invitations;

/// <summary>
/// Adaptador real del <c>email-service</c>: SMTP genérico sobre MailKit (ADR-0010). Las
/// excepciones se propagan a propósito; el caso de uso las traduce a <c>email_sent: false</c> sin
/// invalidar la invitación.
/// </summary>
public sealed class SmtpInvitationEmailSender(
    IOptions<EmailOptions> options,
    ILogger<SmtpInvitationEmailSender> logger) : IInvitationEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public bool IsEnabled => _options.IsConfigured;

    public async Task SendAsync(InvitationEmail message, CancellationToken ct = default)
    {
        if (!IsEnabled)
            throw new InvalidOperationException(
                "No hay cuenta de envío configurada: falta 'Email:Host' o 'Email:FromAddress'.");

        var mail = InvitationEmailComposer.Compose(_options, message);

        using var client = new SmtpClient { Timeout = _options.TimeoutSeconds * 1000 };

        await client.ConnectAsync(_options.Host, _options.Port, ResolveSecurity(_options.SecurityMode), ct);

        // Un relay local de desarrollo puede no pedir credenciales.
        if (!string.IsNullOrWhiteSpace(_options.Username))
            await client.AuthenticateAsync(_options.Username, _options.Password, ct);

        await client.SendAsync(mail, ct);
        await client.DisconnectAsync(quit: true, ct);

        // Ni el destinatario completo ni el enlace: el token es un secreto y el email es PII.
        logger.LogInformation(
            "Invitación enviada a {MaskedEmail} en el Workspace {WorkspaceName}.",
            EmailMasking.Mask(message.ToEmail),
            message.WorkspaceName);
    }

    private static SecureSocketOptions ResolveSecurity(string securityMode) => securityMode switch
    {
        EmailSecurityModes.Ssl => SecureSocketOptions.SslOnConnect,
        EmailSecurityModes.None => SecureSocketOptions.None,
        EmailSecurityModes.Auto => SecureSocketOptions.Auto,
        _ => SecureSocketOptions.StartTls
    };
}
