namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// Nombres canónicos de los eventos del embudo de login definidos en la KB
/// (<c>docs/07-seguridad/autenticacion-autorizacion.md</c>). Centralizarlos evita que el emisor
/// (servicio de telemetría) y el ingestor (endpoint) se desincronicen.
/// </summary>
public static class LoginFunnelEvents
{
    public const string ScreenViewed = "login_screen_viewed";
    public const string GoogleClicked = "login_google_clicked";
    public const string Success = "login_google_success";
    public const string Error = "login_google_error";
    public const string Abandonment = "login_abandonment";

    /// <summary>
    /// Eventos que el cliente puede emitir por el endpoint de ingesta. <c>Success</c> y <c>Error</c>
    /// quedan fuera a propósito: son autoritativos del servidor (se emiten en el intercambio con
    /// Google), de modo que el cliente no puede falsear la conversión ni los errores.
    /// </summary>
    public static readonly IReadOnlySet<string> ClientIngestable =
        new HashSet<string>(StringComparer.Ordinal)
        {
            ScreenViewed,
            GoogleClicked,
            Abandonment,
        };

    public const int FlowIdMaxLength = 64;

    /// <summary>
    /// El <c>flow_id</c> es un correlador aleatorio generado por el cliente. Se restringe a
    /// alfanumérico y longitud acotada: ni es PII ni debe permitir inyectar contenido arbitrario en
    /// la traza (privacidad por diseño, RN-020 / RN-017).
    /// </summary>
    public static bool IsValidFlowId(string? flowId) =>
        !string.IsNullOrEmpty(flowId) &&
        flowId.Length <= FlowIdMaxLength &&
        flowId.All(char.IsAsciiLetterOrDigit);
}
