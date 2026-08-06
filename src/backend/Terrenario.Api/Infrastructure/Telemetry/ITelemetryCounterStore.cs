namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-601 — Persistencia de los contadores agregados. Es lo que convierte una medida volátil en una
/// serie con la que se puede mirar atrás siete o treinta días, que es lo que exigen los SLO de
/// <c>docs/05-infraestructura/observabilidad.md</c>.
/// </summary>
public interface ITelemetryCounterStore
{
    /// <summary>
    /// Suma los deltas a lo que ya haya de ese día. **Suma, no sustituye**: cada volcado trae lo
    /// ocurrido desde el anterior, y con varias instancias los deltas de todas deben acumularse.
    /// </summary>
    Task AddAsync(IReadOnlyCollection<TelemetryCounter> counters, DateTimeOffset now, CancellationToken ct);

    /// <summary>Contadores de un rango de días, ambos extremos incluidos.</summary>
    Task<IReadOnlyList<TelemetryCounter>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken ct);

    /// <summary>Borra los contadores anteriores a <paramref name="before"/>. Devuelve cuántos borró.</summary>
    Task<int> PruneAsync(DateOnly before, CancellationToken ct);
}
