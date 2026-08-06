namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-602 — Emite las señales de uso y las suma a los contadores agregados, con el mismo criterio que
/// <see cref="LoginTelemetryService"/>: log estructurado para el caso concreto, contador diario para la
/// serie. Y con la misma garantía: <b>ninguna dimensión identifica a nadie</b>. La señal no lleva el
/// usuario ni el Workspace, aunque el endpoint sea autenticado y el servidor los conozca.
/// </summary>
public sealed class UsageTelemetryService(
    ILogger<UsageTelemetryService> logger,
    ITelemetryCounters counters,
    TimeProvider clock) : IUsageTelemetry
{
    public void AppSessionStarted(UsageEventContext context)
    {
        counters.Add(TelemetryMetrics.AppSessionStarted);
        Emit(UsageEvents.AppSessionStarted, context);
    }

    public void DashboardViewed(UsageEventContext context, bool firstInSession)
    {
        counters.Add(TelemetryMetrics.DashboardViewed);

        // El KPI pregunta por sesiones, no por visitas: quien entra ocho veces sigue siendo una sesión.
        if (firstInSession) counters.Add(TelemetryMetrics.DashboardSessionWithView);

        Emit(UsageEvents.DashboardViewed, context, ("first_in_session", firstInSession));
    }

    public void DashboardManualRefresh(UsageEventContext context)
    {
        counters.Add(TelemetryMetrics.DashboardManualRefresh);
        Emit(UsageEvents.DashboardManualRefresh, context);
    }

    public void DashboardWidgets(
        UsageEventContext context, IReadOnlyCollection<DashboardWidgetOutcome> outcomes)
    {
        foreach (var outcome in outcomes)
        {
            counters.Add(TelemetryMetrics.DashboardWidgetFor(outcome.Widget, outcome.Status));
            counters.Add(outcome.Status == Telemetry.DashboardWidgets.StatusError
                ? TelemetryMetrics.DashboardWidgetBlocked
                : TelemetryMetrics.DashboardWidgetRendered);
        }

        Emit(UsageEvents.DashboardWidgets, context,
            ("widgets", string.Join(' ', outcomes.Select(o => $"{o.Widget}:{o.Status}"))));
    }

    private void Emit(string eventName, UsageEventContext context, (string Key, object Value)? extra = null)
    {
        var timestamp = clock.GetUtcNow().ToString("O");

        if (extra is { } detail)
        {
            logger.LogInformation(
                "product.usage event={Event} timestamp={Timestamp} session_id={SessionId} "
                + "channel={Channel} device_type={DeviceType} detail_key={DetailKey} detail={Detail}",
                eventName, timestamp, context.SessionId, context.Channel, context.DeviceType,
                detail.Key, detail.Value);
        }
        else
        {
            logger.LogInformation(
                "product.usage event={Event} timestamp={Timestamp} session_id={SessionId} "
                + "channel={Channel} device_type={DeviceType}",
                eventName, timestamp, context.SessionId, context.Channel, context.DeviceType);
        }
    }
}
