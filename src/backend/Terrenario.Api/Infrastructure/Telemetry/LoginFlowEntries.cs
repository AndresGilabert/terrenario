using System.Collections.Concurrent;

namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MKT-106 — Recuerda la clasificación de entrada (<see cref="ReferrerClassifier"/>) de cada intento
/// de login para poder sumarla también a <c>login.success.entry.*</c> cuando el intento termina en
/// éxito. Mismo patrón y los mismos límites que <see cref="LoginFlowTimings"/>: vive en memoria, solo
/// mientras dura el intento, y se poda por tamaño y por edad.
/// </summary>
public sealed class LoginFlowEntries(TimeProvider clock)
{
    private sealed record Entry(string Classification, DateTimeOffset RecordedAt);

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    /// <summary>Marca la clasificación de entrada del intento. Repetir «pantalla vista» no la reemplaza.</summary>
    public void Start(string flowId, string classification)
    {
        if (_entries.Count >= LoginFlowTimings.MaxTrackedFlows / 2) Prune();
        if (_entries.Count >= LoginFlowTimings.MaxTrackedFlows) return;

        _entries.TryAdd(flowId, new Entry(classification, clock.GetUtcNow()));
    }

    /// <summary>Cierra el intento y devuelve la clasificación, o <c>null</c> si no se conocía.</summary>
    public string? Complete(string flowId)
    {
        if (!_entries.TryRemove(flowId, out var entry)) return null;

        var age = clock.GetUtcNow() - entry.RecordedAt;
        return age < TimeSpan.Zero || age > LoginFlowTimings.MaxAge ? null : entry.Classification;
    }

    /// <summary>Descarta el intento sin sumarlo (error o abandono).</summary>
    public void Discard(string flowId) => _entries.TryRemove(flowId, out _);

    private void Prune()
    {
        var limit = clock.GetUtcNow() - LoginFlowTimings.MaxAge;

        foreach (var (flowId, entry) in _entries)
        {
            if (entry.RecordedAt < limit) _entries.TryRemove(flowId, out _);
        }
    }
}
