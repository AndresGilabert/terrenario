namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-602 — Señales de uso del producto. Separada de <see cref="ILoginTelemetry"/> a propósito: el
/// embudo de login mide <b>entrar</b> y estas miden <b>usar</b>, y son dos preguntas con dueños y ciclos
/// de vida distintos.
/// </summary>
public interface IUsageTelemetry
{
    void AppSessionStarted(UsageEventContext context);
    void DashboardViewed(UsageEventContext context, bool firstInSession);
    void DashboardManualRefresh(UsageEventContext context);
    void DashboardWidgets(UsageEventContext context, IReadOnlyCollection<DashboardWidgetOutcome> outcomes);
}

/// <summary>Cómo se resolvió un widget del dashboard en una carga concreta.</summary>
public sealed record DashboardWidgetOutcome(string Widget, string Status);

/// <summary>
/// Dimensiones de una señal de uso. Menos que las del embudo: aquí no hay intento que correlacionar,
/// así que no hay <c>flow_id</c>.
/// </summary>
public sealed record UsageEventContext
{
    private UsageEventContext(string sessionId, string deviceType, string channel)
    {
        SessionId = sessionId;
        DeviceType = deviceType;
        Channel = channel;
    }

    public string SessionId { get; }
    public string DeviceType { get; }
    public string Channel { get; }

    public static UsageEventContext Create(string? sessionId, string? deviceType) =>
        new(TelemetryDimensions.NormalizeSessionId(sessionId),
            TelemetryDimensions.NormalizeDeviceType(deviceType),
            TelemetryDimensions.ChannelWeb);
}
