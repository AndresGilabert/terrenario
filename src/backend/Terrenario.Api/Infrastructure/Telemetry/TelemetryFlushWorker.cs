using Microsoft.Extensions.Options;

namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-601 — Vuelca a base de datos lo que el acumulador lleva sumado, y una vez al día poda el
/// histórico que ya no se mira.
///
/// Mismo patrón que <c>RetentionPurgeWorker</c> (MVP-504) y por la misma razón: un
/// <see cref="BackgroundService"/> viaja con la aplicación y no añade nada que desplegar. Aquí no hace
/// falta el <i>advisory lock</i> de aquel: el volcado suma deltas con <c>ON CONFLICT DO UPDATE</c>, así
/// que dos instancias volcando a la vez dan el resultado correcto en vez de pisarse.
///
/// Un fallo de escritura **devuelve lo drenado al acumulador** y se reintenta en la pasada siguiente.
/// Lo que no puede pasar es que medir se lleve por delante al proceso que sirve la aplicación.
/// </summary>
public sealed class TelemetryFlushWorker(
    IServiceProvider services,
    TelemetryCounterAccumulator accumulator,
    IOptions<TelemetryOptions> options,
    TimeProvider clock,
    ILogger<TelemetryFlushWorker> logger) : BackgroundService
{
    private DateOnly _lastPrunedOn = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        if (!settings.Enabled)
        {
            logger.LogInformation("Volcado de telemetría (MVP-601) desactivado por configuración.");
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(settings.FlushInterval, clock, stoppingToken);
                await FlushOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Parada normal de la aplicación.
        }
        finally
        {
            // Un despliegue no debería costar la ventana en curso: se intenta un último volcado con un
            // margen propio, porque el token de parada ya está cancelado.
            if (settings.Enabled)
                await FlushOnceAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        }
    }

    /// <summary>
    /// Una pasada de volcado. Es público para poder ejercitarlo sin esperar al temporizador: probar
    /// el volcado a través de <see cref="ExecuteAsync"/> obligaría a que el test durase lo que dura la
    /// cadencia real.
    /// </summary>
    public async Task FlushOnceAsync(CancellationToken ct)
    {
        var drained = accumulator.Drain();

        try
        {
            if (drained.Count > 0)
            {
                using var scope = services.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<ITelemetryCounterStore>();
                await store.AddAsync(drained, clock.GetUtcNow(), ct);
                drained = [];
            }

            await PruneIfDueAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Parada durante el volcado: lo drenado vuelve al acumulador en el `finally`.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo volcar la telemetría. Se reintentará en la siguiente pasada.");
        }
        finally
        {
            if (drained.Count > 0) accumulator.Restore(drained);
        }
    }

    private async Task PruneIfDueAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (_lastPrunedOn == today) return;

        using var scope = services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ITelemetryCounterStore>();
        var borrados = await store.PruneAsync(today.AddDays(-Math.Max(1, options.Value.RetentionDays)), ct);
        _lastPrunedOn = today;

        if (borrados > 0)
            logger.LogInformation("Telemetría: {Borrados} contadores fuera de ventana eliminados.", borrados);
    }
}
