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
    /// <summary>Días de serie diaria cuando no se pide otra cosa: cuatro semanas comparables.</summary>
    public const int DefaultDailyDays = 28;

    /// <summary>
    /// Tope de la serie diaria. Coincide con la retención de los contadores: pedir más solo devolvería
    /// días vacíos.
    /// </summary>
    public const int MaxDailyDays = 400;

    public async Task<OperationalSignals> BuildAsync(int? dailyDays, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var days = Math.Clamp(dailyDays ?? DefaultDailyDays, 1, MaxDailyDays);

        // Una sola lectura para todo: las ventanas de los SLO y la serie diaria salen de las mismas
        // filas. El rango es el más ancho de los tres.
        var rows = await store.GetRangeAsync(today.AddDays(-(Math.Max(30, days) - 1)), today, ct);

        // Los SLO de la KB hablan de 7 y 30 días, y **esas ventanas no se piden por parámetro**: son
        // parte de la definición del SLO. Lo que el parámetro mueve es la serie diaria, que es otra
        // pregunta —«¿mejora o empeora?»— y no redefine ningún objetivo.
        var last30 = Totals(rows.Where(r => r.Date > today.AddDays(-30)));
        var last7 = Totals(rows.Where(r => r.Date > today.AddDays(-7)));
        var live = window.Snapshot(AlertThresholds.Window);

        return new OperationalSignals(
            GeneratedAt: clock.GetUtcNow(),
            Daily: BuildDailySeries(rows, today, days),
            Slo: new SloSignals(
                ErrorRate7d: Ratio(last7, TelemetryMetrics.ApiRequests5xx, TelemetryMetrics.ApiRequests),
                ErrorRateObjective: 0.001,
                LatencyP95Ms7d: AlertEvaluator.LatencyP95Ms(last7),
                LatencyP95ObjectiveMs: 300,
                HealthyMinutes30d: last30.GetValueOrDefault(TelemetryMetrics.HealthProbeOk),
                DegradedMinutes30d: last30.GetValueOrDefault(TelemetryMetrics.HealthProbeFailed),
                InternalRequests7d: last7.GetValueOrDefault(TelemetryMetrics.ApiInternalRequests),
                InternalErrors7d: last7.GetValueOrDefault(TelemetryMetrics.ApiInternalRequests5xx)),
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

    private static Dictionary<string, long> Totals(IEnumerable<TelemetryCounter> counters)
    {
        var totals = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var counter in counters)
            totals[counter.Metric] = totals.GetValueOrDefault(counter.Metric) + counter.Value;
        return totals;
    }

    /// <summary>
    /// MVP-699 (`R-01`) — La serie por día.
    ///
    /// Las ventanas fijas contestan «cómo va la semana» pero no «va mejor o peor que la anterior», ni
    /// «qué día se torció». Y sin eso no se puede hacer lo que pide `kpis.md`: fijar el baseline con
    /// las primeras cuatro semanas, porque fijar un baseline **es** comparar semanas.
    ///
    /// Se emite <b>un día por fecha del rango, aunque no haya nada</b>: un hueco en la serie es
    /// información —ese día no se observó nada— y omitirlo lo escondería. Por eso los recuentos van a
    /// cero y los cocientes a <c>null</c>: «cero accesos» y «no lo sé» siguen sin ser lo mismo.
    /// </summary>
    private static IReadOnlyList<DailySignals> BuildDailySeries(
        IReadOnlyList<TelemetryCounter> rows, DateOnly today, int days)
    {
        var byDate = rows.GroupBy(r => r.Date).ToDictionary(g => g.Key, Totals);

        return [.. Enumerable.Range(0, days)
            .Select(offset => today.AddDays(-(days - 1 - offset)))
            .Select(date =>
            {
                var totals = byDate.GetValueOrDefault(date, []);

                return new DailySignals(
                    Date: date,
                    LoginScreenViewed: totals.GetValueOrDefault(TelemetryMetrics.LoginScreenViewed),
                    LoginSuccess: totals.GetValueOrDefault(TelemetryMetrics.LoginSuccess),
                    LoginAbandonment: totals.GetValueOrDefault(TelemetryMetrics.LoginAbandonment),
                    LoginConversion: Ratio(totals, TelemetryMetrics.LoginSuccess, TelemetryMetrics.LoginScreenViewed),
                    Sessions: totals.GetValueOrDefault(TelemetryMetrics.AppSessionStarted),
                    SessionsWithDashboard: totals.GetValueOrDefault(TelemetryMetrics.DashboardSessionWithView),
                    DashboardUsage: Ratio(
                        totals, TelemetryMetrics.DashboardSessionWithView, TelemetryMetrics.AppSessionStarted),
                    ManualRefresh: totals.GetValueOrDefault(TelemetryMetrics.DashboardManualRefresh),
                    WidgetCoverage: Coverage(totals),
                    Requests: totals.GetValueOrDefault(TelemetryMetrics.ApiRequests),
                    ErrorRate: Ratio(totals, TelemetryMetrics.ApiRequests5xx, TelemetryMetrics.ApiRequests),
                    LatencyP95Ms: AlertEvaluator.LatencyP95Ms(totals),
                    RecordsCreated: totals.GetValueOrDefault(TelemetryMetrics.ApiCreated),
                    HealthyMinutes: totals.GetValueOrDefault(TelemetryMetrics.HealthProbeOk),
                    DegradedMinutes: totals.GetValueOrDefault(TelemetryMetrics.HealthProbeFailed));
            })];
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
    IReadOnlyList<DailySignals> Daily,
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
/// <param name="InternalRequests7d">
/// MVP-699 (`R-03`) — Peticiones **excluidas** del SLO por no ser tráfico de nadie (sonda de salud,
/// consulta de señales, ingesta de telemetría). Se publican para que la exclusión sea visible: un
/// recorte que no se ve se acaba leyendo como si nunca hubiera existido ese tráfico.
/// </param>
public sealed record SloSignals(
    double? ErrorRate7d,
    double ErrorRateObjective,
    int? LatencyP95Ms7d,
    int LatencyP95ObjectiveMs,
    long HealthyMinutes30d,
    long DegradedMinutes30d,
    long InternalRequests7d,
    long InternalErrors7d);

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

/// <summary>
/// Un día de la serie. Los recuentos van a cero cuando no hubo nada y los cocientes a <c>null</c>: un
/// día sin tráfico no es un día con la conversión al 0 %.
/// </summary>
public sealed record DailySignals(
    DateOnly Date,
    long LoginScreenViewed,
    long LoginSuccess,
    long LoginAbandonment,
    double? LoginConversion,
    long Sessions,
    long SessionsWithDashboard,
    double? DashboardUsage,
    long ManualRefresh,
    double? WidgetCoverage,
    long Requests,
    double? ErrorRate,
    int? LatencyP95Ms,
    long RecordsCreated,
    long HealthyMinutes,
    long DegradedMinutes);

public sealed record LiveWindow(
    int WindowMinutes,
    long Requests,
    double? ErrorRate,
    int? LatencyP95Ms,
    long LoginScreenViewed,
    double? LoginConversion);
