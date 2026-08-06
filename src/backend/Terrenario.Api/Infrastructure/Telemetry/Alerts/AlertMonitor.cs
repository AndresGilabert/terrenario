using Microsoft.Extensions.Options;

namespace Terrenario.Api.Infrastructure.Telemetry.Alerts;

/// <summary>
/// MVP-603 — La vigilancia: cada minuto comprueba la salud, mira la ventana de 30 minutos, evalúa las
/// cinco alertas de la KB y avisa **solo cuando algo cambia de estado**.
///
/// Mismo patrón que el resto de servicios de fondo del producto (expurgo de `RN-041`, volcado de
/// telemetría) y por la misma razón: viaja con la aplicación y no añade infraestructura que hoy no
/// existe. Con el tamaño de equipo actual, una plataforma de alertado sería desproporcionada.
///
/// <b>Punto ciego declarado</b>: un proceso muerto no se vigila a sí mismo. `ServiceDown` cubre aquí la
/// degradación observable desde dentro —la base de datos inalcanzable—; la caída total la detecta la
/// sonda de la plataforma contra <c>GET /api/v1/health</c>. Está escrito así en
/// <c>observabilidad.md</c> para que nadie lea estas señales como una garantía de disponibilidad.
/// </summary>
public sealed class AlertMonitor(
    IServiceProvider services,
    RollingWindowMetrics window,
    ITelemetryCounters counters,
    AlertStateStore states,
    IOptions<OpsOptions> options,
    TimeProvider clock,
    ILogger<AlertMonitor> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private int _consecutiveFailedProbes;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.AlertsEnabled)
        {
            logger.LogInformation("Vigilancia de alertas (MVP-603) desactivada por configuración.");
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(Interval, clock, stoppingToken);
                await RunOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Parada normal de la aplicación.
        }
    }

    /// <summary>Una pasada de vigilancia. Un fallo se registra y no se propaga.</summary>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            // Un solo ámbito para toda la pasada: la sonda y el emisor de avisos son servicios por
            // petición (base de datos y transporte SMTP), y aquí no hay petición que los provea.
            using var scope = services.CreateScope();

            await ProbeHealthAsync(scope.ServiceProvider, ct);

            var verdicts = AlertEvaluator.Evaluate(
                window.Snapshot(AlertThresholds.Window), _consecutiveFailedProbes);

            var now = clock.GetUtcNow();
            var notifier = scope.ServiceProvider.GetRequiredService<IAlertNotifier>();

            foreach (var verdict in verdicts)
            {
                if (states.Apply(verdict, now) is not { } transition) continue;

                if (transition.Started)
                {
                    counters.Add(TelemetryMetrics.AlertFiredFor(verdict.Name));
                    await notifier.NotifyFiringAsync(verdict, ct);
                }
                else
                {
                    await notifier.NotifyResolvedAsync(verdict, transition.Duration, ct);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Parada durante la pasada.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo en la vigilancia de alertas. Se reintentará en la siguiente pasada.");
        }
    }

    private async Task ProbeHealthAsync(IServiceProvider scoped, CancellationToken ct)
    {
        var report = await scoped.GetRequiredService<HealthProbe>().CheckAsync(ct);

        if (report.IsHealthy)
        {
            _consecutiveFailedProbes = 0;
            counters.Add(TelemetryMetrics.HealthProbeOk);
        }
        else
        {
            _consecutiveFailedProbes++;
            counters.Add(TelemetryMetrics.HealthProbeFailed);
        }
    }
}
