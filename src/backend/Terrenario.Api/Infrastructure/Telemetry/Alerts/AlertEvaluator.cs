using System.Globalization;

namespace Terrenario.Api.Infrastructure.Telemetry.Alerts;

/// <summary>
/// MVP-603 — Decide si cada alerta está disparada, a partir de una foto de la ventana. Es una función
/// pura a propósito: toda la regla de negocio de la operación —umbrales, volúmenes mínimos, cómo se
/// calcula el P95— queda comprobable sin relojes, sin base de datos y sin servicios de fondo.
/// </summary>
public static class AlertEvaluator
{
    public static IReadOnlyList<AlertVerdict> Evaluate(
        IReadOnlyDictionary<string, long> window, int consecutiveFailedProbes)
        =>
        [
            ServiceDown(consecutiveFailedProbes),
            HighErrorRate(window),
            HighLatency(window),
            LoginAbandonmentSpike(window),
            LoginSuccessDrop(window),
        ];

    private static AlertVerdict ServiceDown(int consecutiveFailedProbes)
    {
        var firing = consecutiveFailedProbes >= AlertThresholds.FailedProbesToAlert;

        return new AlertVerdict(
            AlertNames.ServiceDown,
            AlertSeverity.Critical,
            firing,
            firing
                ? $"La comprobación de salud falla desde hace {consecutiveFailedProbes} minutos."
                : "La comprobación de salud responde.");
    }

    private static AlertVerdict HighErrorRate(IReadOnlyDictionary<string, long> window)
    {
        var requests = window.GetValueOrDefault(TelemetryMetrics.ApiRequests);
        var errors = window.GetValueOrDefault(TelemetryMetrics.ApiRequests5xx);

        if (requests < AlertThresholds.MinRequests)
            return new AlertVerdict(AlertNames.HighErrorRate, AlertSeverity.Critical, false,
                $"Sin volumen suficiente para juzgar ({requests} peticiones).");

        var rate = (double)errors / requests;

        return new AlertVerdict(
            AlertNames.HighErrorRate,
            AlertSeverity.Critical,
            rate > AlertThresholds.ErrorRate,
            $"Tasa 5xx {Percent(rate)} sobre {requests} peticiones "
            + $"(umbral {Percent(AlertThresholds.ErrorRate)}).");
    }

    private static AlertVerdict HighLatency(IReadOnlyDictionary<string, long> window)
    {
        var requests = window.GetValueOrDefault(TelemetryMetrics.ApiRequests);

        if (requests < AlertThresholds.MinRequests)
            return new AlertVerdict(AlertNames.HighLatency, AlertSeverity.Warning, false,
                $"Sin volumen suficiente para juzgar ({requests} peticiones).");

        var p95 = LatencyP95Ms(window);

        return new AlertVerdict(
            AlertNames.HighLatency,
            AlertSeverity.Warning,
            p95 is { } value && value > AlertThresholds.LatencyP95Ms,
            p95 is { } ms
                ? $"P95 {FormatBucket(ms)} sobre {requests} peticiones "
                  + $"(umbral {AlertThresholds.LatencyP95Ms} ms)."
                : "Sin muestras de latencia en la ventana.");
    }

    private static AlertVerdict LoginAbandonmentSpike(IReadOnlyDictionary<string, long> window)
    {
        var screens = window.GetValueOrDefault(TelemetryMetrics.LoginScreenViewed);

        if (screens < AlertThresholds.MinLoginScreens)
            return new AlertVerdict(AlertNames.LoginAbandonmentSpike, AlertSeverity.High, false,
                $"Sin volumen suficiente para juzgar ({screens} pantallas de acceso).");

        var rate = (double)window.GetValueOrDefault(TelemetryMetrics.LoginAbandonment) / screens;

        return new AlertVerdict(
            AlertNames.LoginAbandonmentSpike,
            AlertSeverity.High,
            rate > AlertThresholds.LoginAbandonment,
            $"Abandono {Percent(rate)} sobre {screens} pantallas de acceso "
            + $"(umbral {Percent(AlertThresholds.LoginAbandonment)}).");
    }

    private static AlertVerdict LoginSuccessDrop(IReadOnlyDictionary<string, long> window)
    {
        var screens = window.GetValueOrDefault(TelemetryMetrics.LoginScreenViewed);

        if (screens < AlertThresholds.MinLoginScreens)
            return new AlertVerdict(AlertNames.LoginSuccessDrop, AlertSeverity.High, false,
                $"Sin volumen suficiente para juzgar ({screens} pantallas de acceso).");

        var conversion = (double)window.GetValueOrDefault(TelemetryMetrics.LoginSuccess) / screens;

        return new AlertVerdict(
            AlertNames.LoginSuccessDrop,
            AlertSeverity.High,
            conversion < AlertThresholds.LoginConversion,
            $"Conversión {Percent(conversion)} sobre {screens} pantallas de acceso "
            + $"(umbral {Percent(AlertThresholds.LoginConversion)}).");
    }

    /// <summary>
    /// P95 estimado sobre el histograma: devuelve el **corte superior** del cubo donde cae el
    /// percentil. Es una cota, no un valor exacto —lo que se puede decir con un histograma— y basta
    /// para comparar contra un umbral, que es lo único que se hace con ella.
    ///
    /// Devuelve <c>null</c> si no hay muestras: distinto de «0 ms», que sería una latencia excelente
    /// inventada.
    /// </summary>
    public static int? LatencyP95Ms(IReadOnlyDictionary<string, long> window)
    {
        var total = TelemetryMetrics.LatencyBucketsMs
            .Sum(bucket => window.GetValueOrDefault(TelemetryMetrics.LatencyBucket(bucket)));

        if (total == 0) return null;

        var target = total * 0.95;
        long accumulated = 0;

        foreach (var bucket in TelemetryMetrics.LatencyBucketsMs)
        {
            accumulated += window.GetValueOrDefault(TelemetryMetrics.LatencyBucket(bucket));
            if (accumulated >= target) return bucket;
        }

        return TelemetryMetrics.LatencyBucketsMs[^1];
    }

    /// <summary>
    /// El P95 es el corte superior de un cubo, así que se lee como «por debajo de X». El último cubo no
    /// tiene techo: decir «por debajo de 2 147 483 647 ms» sería absurdo, y «por encima de 2000 ms» es
    /// exactamente lo que se sabe.
    /// </summary>
    public static string FormatBucket(int bucketMs) =>
        bucketMs == int.MaxValue
            ? $"por encima de {TelemetryMetrics.LatencyBucketsMs[^2]} ms"
            : $"por debajo de {bucketMs} ms";

    private static string Percent(double ratio) =>
        (ratio * 100).ToString("0.##", CultureInfo.InvariantCulture) + " %";
}
