using System.Collections.Concurrent;

namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-603 — Los mismos contadores, pero en cubos de un minuto y solo de la última hora.
///
/// Hace falta además del contador diario porque las alertas de la KB están definidas sobre ventanas
/// cortas —«abandono &gt; 25 % durante 30 min»— y un contador diario no puede responder a eso: a las
/// 23:00 lleva acumuladas las últimas veintitrés horas, así que una caída de media hora queda diluida
/// y la alerta no salta nunca.
///
/// Vive solo en memoria y no se persiste: es un estado de decisión, no un dato. Si el proceso se
/// reinicia, la ventana empieza vacía y las alertas esperan a tener volumen suficiente.
/// </summary>
public sealed class RollingWindowMetrics(TimeProvider clock)
{
    /// <summary>Cuánto histórico se guarda. Por encima de la ventana más larga que se consulta (30 min).</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromMinutes(60);

    private readonly ConcurrentDictionary<(long Minute, string Metric), long> _buckets = new();
    private long _lastPrunedMinute;

    public void Add(string metric, long delta)
    {
        if (string.IsNullOrWhiteSpace(metric) || delta == 0) return;

        var minute = CurrentMinute();
        _buckets.AddOrUpdate((minute, metric), delta, (_, current) => current + delta);

        // Barrer el diccionario en cada medida sería trabajo por petición servida: basta con hacerlo
        // cuando cambia el minuto, que es cuando puede haber algo que caducar.
        if (Interlocked.Exchange(ref _lastPrunedMinute, minute) != minute) Prune(minute);
    }

    /// <summary>Suma de cada métrica en los últimos <paramref name="window"/>.</summary>
    public IReadOnlyDictionary<string, long> Snapshot(TimeSpan window)
    {
        var oldest = CurrentMinute() - (long)Math.Ceiling(window.TotalMinutes) + 1;
        var totals = new Dictionary<string, long>(StringComparer.Ordinal);

        foreach (var ((minute, metric), value) in _buckets)
        {
            if (minute < oldest) continue;
            totals[metric] = totals.GetValueOrDefault(metric) + value;
        }

        return totals;
    }

    private long CurrentMinute() => clock.GetUtcNow().ToUnixTimeSeconds() / 60;

    private void Prune(long currentMinute)
    {
        var oldest = currentMinute - (long)Retention.TotalMinutes;

        foreach (var key in _buckets.Keys)
        {
            if (key.Minute < oldest) _buckets.TryRemove(key, out _);
        }
    }
}

/// <summary>
/// Reparte cada medida a las dos salidas: el acumulador diario (la serie que se conserva) y la
/// ventana corta (la que deciden las alertas). Quien mide sigue llamando a
/// <see cref="ITelemetryCounters"/> sin saber que hay dos destinos.
/// </summary>
public sealed class CompositeTelemetryCounters(
    TelemetryCounterAccumulator daily, RollingWindowMetrics window) : ITelemetryCounters
{
    public void Add(string metric, long delta = 1)
    {
        daily.Add(metric, delta);
        window.Add(metric, delta);
    }
}
