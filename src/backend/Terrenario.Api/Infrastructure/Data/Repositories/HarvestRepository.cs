using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Harvests;
using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Adaptador EF Core de las cosechas (MVP-401). Reproduce las dos decisiones que ya comparten
/// actividades, compras y consumos:
/// <list type="bullet">
/// <item><b>La baja lógica se filtra aquí</b> (<c>deleted_at IS NULL</c>), no con un filtro global de
/// EF.</item>
/// <item><b>La colisión de versión de la base de datos se traduce a 409</b>, no a 500: el
/// <c>EnsureVersion</c> del dominio cubre el caso normal, pero dos escrituras simultáneas con la misma
/// versión de partida solo las separa el token de concurrencia de EF.</item>
/// </list>
/// </summary>
public sealed class HarvestRepository(TerrenarioDbContext db) : IHarvestRepository
{
    public async Task AddAsync(Harvest harvest, CancellationToken ct = default)
        => await db.Harvests.AddAsync(harvest, ct);

    public Task<Harvest?> FindByIdAsync(Guid workspaceId, Guid harvestId, CancellationToken ct = default)
        => db.Harvests.FirstOrDefaultAsync(
            h => h.Id == harvestId && h.WorkspaceId == workspaceId && h.DeletedAt == null, ct);

    public async Task<IReadOnlyList<HarvestView>> ListAsync(
        Guid workspaceId,
        HarvestFilter filter,
        CancellationToken ct = default)
    {
        var live = LiveHarvests(workspaceId);

        if (filter.From is { } from) live = live.Where(h => h.Date >= from);
        if (filter.To is { } to) live = live.Where(h => h.Date <= to);
        if (filter.PlotId is { } plotId) live = live.Where(h => h.PlotId == plotId);
        if (filter.SeasonId is { } seasonId) live = live.Where(h => h.SeasonId == seasonId);
        // El destino es un catálogo cerrado: comparación exacta, no parcial como el material libre de
        // las compras (RN-031 frente a RN-012).
        if (!string.IsNullOrWhiteSpace(filter.Destination))
        {
            var destination = filter.Destination.Trim();
            live = live.Where(h => h.Destination == destination);
        }

        // Filtros y orden se aplican sobre columnas reales **antes** de proyectar, que es lo que EF
        // sabe traducir (lección de P-014).
        var rows = await ProjectViews(live.OrderByDescending(h => h.Date)).ToListAsync(ct);

        // El desempate por fecha de captura se reaplica en memoria: EF+SQLite no traduce `ORDER BY`
        // sobre `DateTimeOffset` (P-031) y degradar la consulta de producción para que el arnés de
        // tests la ejercite sería justo el error que ese punto describe.
        return rows
            .OrderByDescending(v => v.Date)
            .ThenByDescending(v => v.CreatedAt)
            .ToList();
    }

    public Task<HarvestView?> GetViewAsync(Guid workspaceId, Guid harvestId, CancellationToken ct = default)
        => ProjectViews(LiveHarvests(workspaceId).Where(h => h.Id == harvestId)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<HarvestAggregateRow>> ListAggregateRowsAsync(
        Guid workspaceId,
        HarvestAggregateFilter filter,
        CancellationToken ct = default)
    {
        var live = LiveHarvests(workspaceId);

        if (filter.SeasonId is { } seasonId) live = live.Where(h => h.SeasonId == seasonId);

        // Una lista vacía **no** es «todos»: eso lo decide el caso de uso resolviendo el ámbito por
        // defecto (RN-008). Aquí, `null` es «sin restringir» y una lista con valores restringe.
        if (filter.PlotIds is { Count: > 0 } plotIds)
        {
            var ids = plotIds.ToArray();
            live = live.Where(h => ids.Contains(h.PlotId));
        }

        // Solo las columnas que suman: sin `JOIN` a los maestros y sin orden, porque agregar no lo
        // necesita. Es la consulta más barata que responde a los cuatro widgets.
        return await live
            .Select(h => new HarvestAggregateRow(h.PlotId, h.SeasonId, h.Kgs, h.Yield, h.Liters, h.Destination))
            .ToListAsync(ct);
    }

    /// <summary>Cosechas vivas del Workspace: el filtro de baja lógica en un único sitio (RN-037).</summary>
    private IQueryable<Harvest> LiveHarvests(Guid workspaceId)
        => db.Harvests.Where(h => h.WorkspaceId == workspaceId && h.DeletedAt == null);

    /// <summary>
    /// Proyección de lectura: resuelve en una sola consulta el nombre del terreno y de la temporada,
    /// más el rango de esta última que necesita el aviso de RN-023.
    /// </summary>
    private IQueryable<HarvestView> ProjectViews(IQueryable<Harvest> harvests)
        => from h in harvests
           join p in db.Plots on h.PlotId equals p.Id
           join s in db.Seasons on h.SeasonId equals s.Id
           select new HarvestView(
               h.Id,
               h.WorkspaceId,
               h.PlotId,
               p.Name,
               h.SeasonId,
               s.Name,
               s.StartDate,
               s.EndDate,
               h.Date,
               h.Product,
               h.Kgs,
               h.Yield,
               h.Liters,
               h.Destination,
               h.Version,
               h.CreatedAt,
               h.UpdatedAt);

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Dos escrituras simultáneas partiendo de la misma versión: la primera gana y la segunda
            // llega aquí. Se responde el mismo 409 que la guarda de aplicación (ADR-0005), no un 500.
            throw new ConcurrencyConflictException(
                "Otra persona ha modificado esta cosecha mientras la editabas. Refresca para ver la versión actual.");
        }
    }
}
