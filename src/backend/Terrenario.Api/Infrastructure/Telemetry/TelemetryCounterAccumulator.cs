using System.Collections.Concurrent;

namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-601 — Suma en memoria y entrega lo acumulado cuando se lo piden. Es un singleton: los eventos
/// llegan desde peticiones concurrentes y el volcado corre en un servicio de fondo.
///
/// El día se decide con la marca **UTC** del momento en que ocurre el evento, no con la del volcado:
/// si no, todo lo acumulado antes de medianoche acabaría contado en el día siguiente.
/// </summary>
public sealed class TelemetryCounterAccumulator(TimeProvider clock) : ITelemetryCounters
{
    private readonly ConcurrentDictionary<(DateOnly Date, string Metric), long> _counters = new();

    public void Add(string metric, long delta = 1)
    {
        if (string.IsNullOrWhiteSpace(metric) || delta == 0) return;

        var date = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        _counters.AddOrUpdate((date, metric), delta, (_, current) => current + delta);
    }

    /// <summary>
    /// Devuelve lo acumulado y lo deja a cero en el mismo paso. Se vacía **antes** de escribir para no
    /// bloquear a quien está midiendo mientras dura la escritura; si esa escritura falla, lo drenado
    /// se devuelve con <see cref="Restore"/> en vez de perderse.
    /// </summary>
    public IReadOnlyCollection<TelemetryCounter> Drain()
    {
        if (_counters.IsEmpty) return [];

        var drained = new List<TelemetryCounter>(_counters.Count);

        foreach (var key in _counters.Keys.ToArray())
        {
            if (_counters.TryRemove(key, out var value) && value != 0)
                drained.Add(new TelemetryCounter(key.Date, key.Metric, value));
        }

        return drained;
    }

    /// <summary>Devuelve al acumulador lo que no se pudo escribir, sumándolo a lo que haya llegado entretanto.</summary>
    public void Restore(IReadOnlyCollection<TelemetryCounter> counters)
    {
        foreach (var counter in counters)
            _counters.AddOrUpdate((counter.Date, counter.Metric), counter.Value, (_, current) => current + counter.Value);
    }
}
