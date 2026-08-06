using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

/// <summary>
/// MVP-601 — Los contadores agregados, en la misma base de datos que el resto del producto: región
/// declarada en la Política de Privacidad, copia de seguridad ya existente y ningún proveedor nuevo.
/// </summary>
public sealed class TelemetryCounterStore(TerrenarioDbContext db) : ITelemetryCounterStore
{
    public async Task AddAsync(
        IReadOnlyCollection<TelemetryCounter> counters, DateTimeOffset now, CancellationToken ct)
    {
        if (counters.Count == 0) return;

        // `ON CONFLICT ... DO UPDATE` y no leer-modificar-escribir: dos instancias volcando a la vez
        // sumarían sobre el mismo valor leído y una de las dos se perdería. La suma la hace el motor.
        foreach (var counter in counters)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO telemetry_daily_counters (date, metric, value, updated_at)
                VALUES ({counter.Date}, {counter.Metric}, {counter.Value}, {now})
                ON CONFLICT (date, metric) DO UPDATE
                SET value = telemetry_daily_counters.value + EXCLUDED.value,
                    updated_at = EXCLUDED.updated_at
                """, ct);
        }
    }

    public async Task<IReadOnlyList<TelemetryCounter>> GetRangeAsync(
        DateOnly from, DateOnly to, CancellationToken ct)
        => await db.TelemetryDailyCounters
            .AsNoTracking()
            .Where(c => c.Date >= from && c.Date <= to)
            .OrderBy(c => c.Date).ThenBy(c => c.Metric)
            .Select(c => new TelemetryCounter(c.Date, c.Metric, c.Value))
            .ToListAsync(ct);

    public async Task<int> PruneAsync(DateOnly before, CancellationToken ct)
        => await db.TelemetryDailyCounters.Where(c => c.Date < before).ExecuteDeleteAsync(ct);
}
