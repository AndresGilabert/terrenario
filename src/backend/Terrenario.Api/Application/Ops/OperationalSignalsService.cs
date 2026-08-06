using Terrenario.Api.Infrastructure.Telemetry;
using Terrenario.Api.Infrastructure.Telemetry.Alerts;

namespace Terrenario.Api.Application.Ops;

/// <summary>
/// MVP-603 (CA-3) — Reúne en una sola respuesta todo lo que la KB pide mirar en la revisión operativa:
/// los tres SLO, el embudo de login, el uso del producto, el monitoreo de negocio mínimo y las alertas
/// vivas.
///
/// Existe porque «se puede calcular» no es lo mismo que «se puede revisar». Los contadores dan para
/// todo, pero obligar a escribir consultas cada semana convierte una revisión de quince minutos en algo
/// que no se hace.
/// </summary>
public sealed class OperationalSignalsService(
    ITelemetryCounterStore store,
    RollingWindowMetrics window,
    AlertStateStore alerts,
    TimeProvider clock)
{
    public async Task<OperationalSignals> BuildAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        // Los SLO de la KB hablan de 7 y 30 días; el resto de la revisión, de la semana.
        var last30 = Totals(await store.GetRangeAsync(today.AddDays(-29), today, ct));
        var last7 = Totals(await store.GetRangeAsync(today.AddDays(-6), today, ct));
        var live = window.Snapshot(AlertThresholds.Window);

        return new OperationalSignals(
            GeneratedAt: clock.GetUtcNow(),
            Slo: new SloSignals(
                ErrorRate7d: Ratio(last7, TelemetryMetrics.ApiRequests5xx, TelemetryMetrics.ApiRequests),
                ErrorRateObjective: 0.001,
                LatencyP95Ms7d: AlertEvaluator.LatencyP95Ms(last7),
                LatencyP95ObjectiveMs: 300,
                HealthyMinutes30d: last30.GetValueOrDefault(TelemetryMetrics.HealthProbeOk),
                DegradedMinutes30d: last30.GetValueOrDefault(TelemetryMetrics.HealthProbeFailed)),
            LoginFunnel7d: new LoginFunnelSignals(
                ScreenViewed: last7.GetValueOrDefault(TelemetryMetrics.LoginScreenViewed),
                GoogleClicked: last7.GetValueOrDefault(TelemetryMetrics.LoginGoogleClicked),
                Success: last7.GetValueOrDefault(TelemetryMetrics.LoginSuccess),
                Errors: last7.GetValueOrDefault(TelemetryMetrics.LoginError),
                Abandonment: last7.GetValueOrDefault(TelemetryMetrics.LoginAbandonment),
                Conversion: Ratio(last7, TelemetryMetrics.LoginSuccess, TelemetryMetrics.LoginScreenViewed),
                AbandonmentRate: Ratio(last7, TelemetryMetrics.LoginAbandonment, TelemetryMetrics.LoginScreenViewed),
                AverageSuccessMs: Ratio(
                    last7, TelemetryMetrics.LoginSuccessDurationMsSum, TelemetryMetrics.LoginSuccessTimedCount)),
            ProductUsage7d: new ProductUsageSignals(
                Sessions: last7.GetValueOrDefault(TelemetryMetrics.AppSessionStarted),
                SessionsWithDashboard: last7.GetValueOrDefault(TelemetryMetrics.DashboardSessionWithView),
                DashboardUsage: Ratio(
                    last7, TelemetryMetrics.DashboardSessionWithView, TelemetryMetrics.AppSessionStarted),
                ManualRefreshPerSession: Ratio(
                    last7, TelemetryMetrics.DashboardManualRefresh, TelemetryMetrics.DashboardSessionWithView),
                WidgetCoverage: Coverage(last7)),
            Business7d: new BusinessSignals(
                Logins: last7.GetValueOrDefault(TelemetryMetrics.LoginSuccess),
                RecordsCreated: last7.GetValueOrDefault(TelemetryMetrics.ApiCreated),
                VisibleErrorRate: Ratio(last7, TelemetryMetrics.ApiRequests4xx, TelemetryMetrics.ApiRequests)),
            Live: new LiveWindow(
                WindowMinutes: (int)AlertThresholds.Window.TotalMinutes,
                Requests: live.GetValueOrDefault(TelemetryMetrics.ApiRequests),
                ErrorRate: Ratio(live, TelemetryMetrics.ApiRequests5xx, TelemetryMetrics.ApiRequests),
                LatencyP95Ms: AlertEvaluator.LatencyP95Ms(live),
                LoginScreenViewed: live.GetValueOrDefault(TelemetryMetrics.LoginScreenViewed),
                LoginConversion: Ratio(live, TelemetryMetrics.LoginSuccess, TelemetryMetrics.LoginScreenViewed)),
            Alerts: alerts.Current());
    }

    private static Dictionary<string, long> Totals(IReadOnlyList<TelemetryCounter> counters)
    {
        var totals = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var counter in counters)
            totals[counter.Metric] = totals.GetValueOrDefault(counter.Metric) + counter.Value;
        return totals;
    }

    /// <summary>
    /// Cociente, o <c>null</c> si el divisor es cero. Devolver cero sería inventarse una respuesta:
    /// «ninguna sesión abrió el dashboard» y «no hubo sesiones» no son lo mismo, y con cero la revisión
    /// leería un problema donde solo hay una semana sin tráfico.
    /// </summary>
    private static double? Ratio(IReadOnlyDictionary<string, long> totals, string numerator, string denominator)
    {
        var divisor = totals.GetValueOrDefault(denominator);
        return divisor == 0 ? null : (double)totals.GetValueOrDefault(numerator) / divisor;
    }

    private static double? Coverage(IReadOnlyDictionary<string, long> totals)
    {
        var rendered = totals.GetValueOrDefault(TelemetryMetrics.DashboardWidgetRendered);
        var blocked = totals.GetValueOrDefault(TelemetryMetrics.DashboardWidgetBlocked);
        var total = rendered + blocked;

        return total == 0 ? null : (double)rendered / total;
    }
}

public sealed record OperationalSignals(
    DateTimeOffset GeneratedAt,
    SloSignals Slo,
    LoginFunnelSignals LoginFunnel7d,
    ProductUsageSignals ProductUsage7d,
    BusinessSignals Business7d,
    LiveWindow Live,
    IReadOnlyList<AlertState> Alerts);

/// <param name="HealthyMinutes30d">
/// Minutos observados sanos. <b>No es uptime</b>: los minutos en los que el proceso estuvo caído no se
/// observan, así que esto mide degradación, no caída. La disponibilidad real la mide la sonda externa.
/// </param>
public sealed record SloSignals(
    double? ErrorRate7d,
    double ErrorRateObjective,
    int? LatencyP95Ms7d,
    int LatencyP95ObjectiveMs,
    long HealthyMinutes30d,
    long DegradedMinutes30d);

public sealed record LoginFunnelSignals(
    long ScreenViewed,
    long GoogleClicked,
    long Success,
    long Errors,
    long Abandonment,
    double? Conversion,
    double? AbandonmentRate,
    double? AverageSuccessMs);

public sealed record ProductUsageSignals(
    long Sessions,
    long SessionsWithDashboard,
    double? DashboardUsage,
    double? ManualRefreshPerSession,
    double? WidgetCoverage);

/// <summary>El «monitoreo de negocio mínimo (fase A)» de <c>observabilidad.md</c>, tal cual lo enumera.</summary>
public sealed record BusinessSignals(
    long Logins,
    long RecordsCreated,
    double? VisibleErrorRate);

public sealed record LiveWindow(
    int WindowMinutes,
    long Requests,
    double? ErrorRate,
    int? LatencyP95Ms,
    long LoginScreenViewed,
    double? LoginConversion);
