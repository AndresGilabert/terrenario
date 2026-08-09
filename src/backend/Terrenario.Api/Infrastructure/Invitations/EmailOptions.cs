namespace Terrenario.Api.Infrastructure.Invitations;

/// <summary>
/// Cuenta de envío del correo transaccional. Se habla SMTP genérico
/// ([ADR-0010](docs/02-arquitectura/decisiones/ADR-0010--envio-de-email-transaccional-por-smtp.md)),
/// así que estas mismas claves sirven para Google Workspace, Brevo, Amazon SES, SendGrid o un
/// servidor corporativo: cambiar de proveedor es cambiar configuración, no código.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;

    /// <summary>`starttls` (587), `ssl` (465), `none` (solo servidores de prueba locales) o `auto`.</summary>
    public string SecurityMode { get; set; } = EmailSecurityModes.StartTls;

    /// <summary>
    /// P-100 — Lo que llega de configuración es texto tecleado a mano en una variable de entorno de
    /// App Service: `None` con mayúscula o con un espacio de más es el mismo modo, no otro distinto.
    /// </summary>
    public string NormalizedSecurityMode => SecurityMode.Trim().ToLowerInvariant();

    /// <summary>
    /// P-100 — Un modo desconocido no rompe: cae al defecto (StartTLS) y el síntoma aparece en la
    /// primera entrega fallida, no al arrancar. Por eso el arranque lo comprueba y lo dice.
    /// </summary>
    public bool IsSecurityModeKnown => EmailSecurityModes.All.Contains(NormalizedSecurityMode);

    /// <summary>Vacío en servidores que no exigen autenticación (relay local de desarrollo).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Secreto: nunca en `appsettings`. Ver `docs/05-infraestructura/entornos.md`.</summary>
    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Terrenario";
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Sin servidor ni remitente no hay envío posible. El arranque cae entonces al adaptador de
    /// traza, para que el MVP siga siendo usable compartiendo el enlace a mano.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}

public static class EmailSecurityModes
{
    public const string StartTls = "starttls";
    public const string Ssl = "ssl";
    public const string None = "none";
    public const string Auto = "auto";

    /// <summary>El catálogo completo, para que la comprobación del arranque no repita la lista.</summary>
    public static readonly string[] All = [StartTls, Ssl, None, Auto];
}
