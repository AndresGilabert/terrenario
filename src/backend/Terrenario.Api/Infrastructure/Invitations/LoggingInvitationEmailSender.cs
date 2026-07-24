namespace Terrenario.Api.Infrastructure.Invitations;

/// <summary>
/// Adaptador provisional mientras no hay proveedor de email contratado: deja traza de que la
/// invitación salió, sin enviar nada. Quien invita recibe el enlace en la respuesta de la API
/// y puede compartirlo por su cuenta.
/// </summary>
public sealed class LoggingInvitationEmailSender(ILogger<LoggingInvitationEmailSender> logger)
    : IInvitationEmailSender
{
    public Task SendAsync(InvitationEmail message, CancellationToken ct = default)
    {
        // El enlace lleva el token en claro y el email es PII: ninguno de los dos se registra
        // (docs/07-seguridad/autenticacion-autorizacion.md).
        logger.LogInformation(
            "Invitación por email preparada para {MaskedEmail} en el Workspace {WorkspaceName}.",
            Mask(message.ToEmail),
            message.WorkspaceName);

        return Task.CompletedTask;
    }

    private static string Mask(string email)
    {
        var separatorIndex = email.IndexOf('@');

        return separatorIndex <= 0
            ? "***"
            : $"{email[0]}***{email[separatorIndex..]}";
    }
}
