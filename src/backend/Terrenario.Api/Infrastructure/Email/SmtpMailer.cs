using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Infrastructure.Email;

/// <summary>
/// Transporte SMTP genérico compartido por todos los correos transaccionales del producto
/// (ADR-0010). Se extrae del emisor de invitaciones (MVP-103) al aparecer el segundo tipo de correo
/// —los avisos de baja y reactivación de Workspace (MVP-206)— para no duplicar la conexión, la
/// autenticación ni el manejo de modos de seguridad. Las excepciones se propagan a propósito: cada
/// caso de uso decide si un fallo del proveedor invalida la operación o solo se refleja como
/// <c>email_sent: false</c>.
/// </summary>
public sealed class SmtpMailer(IOptions<EmailOptions> options, ILogger<SmtpMailer> logger)
{
    private readonly EmailOptions _options = options.Value;

    public bool IsEnabled => _options.IsConfigured;

    // MVP-715 — Ya no expone `Options`: la usaban los emisores para poner el remitente al componer
    // su mensaje, y de eso se ocupa ahora `ProductEmailTemplate`. Devolverla seguiría dejando que
    // alguien se compusiera un correo por su cuenta, que es justo lo que la plantilla cierra.

    public async Task SendAsync(MimeMessage message, string context, CancellationToken ct = default)
    {
        if (!IsEnabled)
            throw new InvalidOperationException(
                "No hay cuenta de envío configurada: falta 'Email:Host' o 'Email:FromAddress'.");

        using var client = new SmtpClient { Timeout = _options.TimeoutSeconds * 1000 };

        await client.ConnectAsync(_options.Host, _options.Port, ResolveSecurity(_options.SecurityMode), ct);

        // Un relay local de desarrollo puede no pedir credenciales.
        if (!string.IsNullOrWhiteSpace(_options.Username))
            await client.AuthenticateAsync(_options.Username, _options.Password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(quit: true, ct);

        // Ni el destinatario completo ni el enlace: el token es un secreto y el email es PII.
        logger.LogInformation(
            "Correo '{Context}' enviado a {MaskedEmail}.",
            context,
            EmailMasking.Mask(message.To.Mailboxes.FirstOrDefault()?.Address ?? string.Empty));
    }

    private static SecureSocketOptions ResolveSecurity(string securityMode) => securityMode switch
    {
        EmailSecurityModes.Ssl => SecureSocketOptions.SslOnConnect,
        EmailSecurityModes.None => SecureSocketOptions.None,
        EmailSecurityModes.Auto => SecureSocketOptions.Auto,
        _ => SecureSocketOptions.StartTls
    };
}
