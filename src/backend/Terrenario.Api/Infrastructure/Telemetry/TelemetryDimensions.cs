namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-601 — Dimensiones mínimas que la KB exige en cada evento del embudo de login
/// (<c>docs/05-infraestructura/observabilidad.md</c> y <c>docs/07-seguridad/autenticacion-autorizacion.md</c>):
/// <c>timestamp</c>, <c>session_id</c>, <c>flow_id</c>, <c>channel</c>, <c>device_type</c> y
/// <c>error_code</c> cuando aplique.
///
/// Los dos identificadores son **aleatorios y de primera parte**: no se derivan de la cuenta, no
/// viajan a ningún tercero y no permiten reidentificar a nadie (RN-020, RN-042). Aquí solo se
/// **valida** su forma, por la misma razón que en <see cref="LoginFunnelEvents.IsValidFlowId"/>: lo
/// que entra por la red no puede inyectar contenido arbitrario en la traza.
/// </summary>
public static class TelemetryDimensions
{
    public const int IdentifierMaxLength = 64;

    /// <summary>Longitud máxima del nombre de un contador agregado (columna <c>metric</c>).</summary>
    public const int MetricMaxLength = 96;

    /// <summary>Canal de origen del evento. En el MVP solo existe el cliente web.</summary>
    public const string ChannelWeb = "web";

    /// <summary>
    /// Valor con el que se registra una dimensión que el cliente no envió. Se prefiere a omitir el
    /// campo: una traza con huecos y otra con «no lo sé» se cuentan distinto al reconstruir el embudo.
    /// </summary>
    public const string Unknown = "unknown";

    public const string DeviceDesktop = "desktop";
    public const string DeviceMobile = "mobile";
    public const string DeviceTablet = "tablet";

    private static readonly IReadOnlySet<string> DeviceTypes =
        new HashSet<string>(StringComparer.Ordinal) { DeviceDesktop, DeviceMobile, DeviceTablet };

    /// <summary>
    /// Identificador aleatorio de sesión de navegador. Mismo criterio que el <c>flow_id</c>:
    /// alfanumérico y acotado. Un valor ausente o mal formado no invalida el evento —perder el evento
    /// entero por una dimensión sería peor para la medida—: se degrada a <see cref="Unknown"/>.
    /// </summary>
    public static bool IsValidSessionId(string? sessionId) =>
        !string.IsNullOrEmpty(sessionId) &&
        sessionId.Length <= IdentifierMaxLength &&
        sessionId.All(char.IsAsciiLetterOrDigit);

    public static string NormalizeSessionId(string? sessionId) =>
        IsValidSessionId(sessionId) ? sessionId! : Unknown;

    /// <summary>
    /// Taxonomía cerrada de <c>device_type</c>. Cerrada a propósito: si se aceptara el texto libre que
    /// mande el cliente, la dimensión dejaría de poder agruparse y además sería un canal de entrada
    /// de contenido arbitrario en la traza.
    /// </summary>
    public static string NormalizeDeviceType(string? deviceType) =>
        deviceType is not null && DeviceTypes.Contains(deviceType) ? deviceType : Unknown;
}

/// <summary>
/// Dimensiones que acompañan a un evento del embudo. Se construye siempre con
/// <see cref="Create"/> para que ningún valor sin normalizar llegue a la traza.
/// </summary>
public sealed record LoginEventContext
{
    private LoginEventContext(string flowId, string sessionId, string deviceType, string channel)
    {
        FlowId = flowId;
        SessionId = sessionId;
        DeviceType = deviceType;
        Channel = channel;
    }

    public string FlowId { get; }
    public string SessionId { get; }
    public string DeviceType { get; }
    public string Channel { get; }

    public static LoginEventContext Create(string flowId, string? sessionId, string? deviceType) =>
        new(flowId,
            TelemetryDimensions.NormalizeSessionId(sessionId),
            TelemetryDimensions.NormalizeDeviceType(deviceType),
            TelemetryDimensions.ChannelWeb);
}
