using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Adaptador EF Core de consumos e imputaciones (MVP-304). Repite las decisiones de
/// <c>ActivityRepository</c> y <c>PurchaseRepository</c>: filtro de baja lógica en el puerto y
/// traducción de la colisión de versión a 409.
/// </summary>
public sealed class ConsumptionRepository(TerrenarioDbContext db) : IConsumptionRepository
{
    public async Task AddAsync(PurchaseConsumption consumption, CancellationToken ct = default)
        => await db.PurchaseConsumptions.AddAsync(consumption, ct);

    public Task<PurchaseConsumption?> FindByIdAsync(
        Guid workspaceId,
        Guid consumptionId,
        CancellationToken ct = default)
        => db.PurchaseConsumptions.FirstOrDefaultAsync(
            c => c.Id == consumptionId && c.WorkspaceId == workspaceId && c.DeletedAt == null, ct);

    public async Task<IReadOnlyList<ConsumptionView>> ListAsync(
        Guid workspaceId,
        ConsumptionFilter filter,
        CancellationToken ct = default)
    {
        var live = LiveConsumptions(workspaceId);

        if (filter.From is { } from) live = live.Where(c => c.Date >= from);
        if (filter.To is { } to) live = live.Where(c => c.Date <= to);
        if (filter.PlotId is { } plotId) live = live.Where(c => c.PlotId == plotId);
        if (filter.SeasonId is { } seasonId) live = live.Where(c => c.SeasonId == seasonId);
        if (filter.PurchaseId is { } purchaseId) live = live.Where(c => c.PurchaseId == purchaseId);
        if (!string.IsNullOrWhiteSpace(filter.Product))
        {
            // Mismo criterio que en compras (R-06): el material es texto libre, así que la igualdad
            // exacta obligaría a recordar cómo se escribió.
            var needle = filter.Product.Trim().ToLower();
            live = live.Where(c => c.Product.ToLower().Contains(needle));
        }

        // Orden por fecha de negocio (CA-4) y desempate por fecha de captura, los dos en SQL desde
        // MVP-501: el desempate se hacía en memoria porque EF+SQLite no traduce `ORDER BY` sobre
        // `DateTimeOffset` (P-031).
        return await ProjectViews(
                live.OrderByDescending(c => c.Date).ThenByDescending(c => c.CreatedAt))
            .ToListAsync(ct);
    }

    public Task<ConsumptionView?> GetViewAsync(
        Guid workspaceId,
        Guid consumptionId,
        CancellationToken ct = default)
        => ProjectViews(LiveConsumptions(workspaceId).Where(c => c.Id == consumptionId))
            .FirstOrDefaultAsync(ct);

    public async Task<decimal> SumImputedQuantityAsync(
        Guid workspaceId,
        Guid purchaseId,
        Guid? excludeConsumptionId = null,
        CancellationToken ct = default)
    {
        var query = LiveConsumptions(workspaceId).Where(c => c.PurchaseId == purchaseId);

        if (excludeConsumptionId is { } excluded)
            query = query.Where(c => c.Id != excluded);

        // `SumAsync` sobre una secuencia vacía devuelve 0 en SQL, pero EF lo traduce a `SUM(...)`,
        // que en SQL es NULL: se proyecta a `decimal?` y se colapsa aquí.
        return await query.SumAsync(c => (decimal?)c.ConsumedQuantity, ct) ?? 0m;
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> SumImputedQuantityByPurchaseAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> purchaseIds,
        CancellationToken ct = default)
    {
        if (purchaseIds.Count == 0) return new Dictionary<Guid, decimal>();

        // Una sola agrupación para todo el listado: mostrar «imputado / total» por fila no puede
        // costar una consulta por compra.
        var rows = await LiveConsumptions(workspaceId)
            .Where(c => c.PurchaseId != null && purchaseIds.Contains(c.PurchaseId.Value))
            .GroupBy(c => c.PurchaseId!.Value)
            .Select(g => new { PurchaseId = g.Key, Imputed = g.Sum(c => c.ConsumedQuantity) })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.PurchaseId, r => r.Imputed);
    }

    public Task<int> CountLiveByPurchaseAsync(
        Guid workspaceId,
        Guid purchaseId,
        CancellationToken ct = default)
        => LiveConsumptions(workspaceId).CountAsync(c => c.PurchaseId == purchaseId, ct);

    /// <summary>Consumos vivos del Workspace: el filtro de baja lógica en un único sitio (RN-037).</summary>
    private IQueryable<PurchaseConsumption> LiveConsumptions(Guid workspaceId)
        => db.PurchaseConsumptions.Where(c => c.WorkspaceId == workspaceId && c.DeletedAt == null);

    /// <summary>
    /// Proyección de lectura: resuelve terreno y temporada en la misma consulta.
    ///
    /// De la compra **solo** se trae la fecha, y con <c>LEFT JOIN</c> porque puede no haberla
    /// (RN-032). El resto sigue sin unirse a propósito: el consumo guarda su propio producto y su
    /// propio precio unitario, así que la fila se explica sola aunque la compra se edite después.
    /// La fecha no es una excepción a eso —no entra en ningún cálculo— sino la referencia con la que
    /// se deriva el aviso de RN-043 (MVP-708, <c>P-058</c>), que tiene que reflejar la compra tal y
    /// como está **ahora**: si se corrige la fecha de la compra, el aviso debe aparecer o irse solo.
    /// </summary>
    private IQueryable<ConsumptionView> ProjectViews(IQueryable<PurchaseConsumption> consumptions)
        => from c in consumptions
           join p in db.Plots on c.PlotId equals p.Id
           join s in db.Seasons on c.SeasonId equals s.Id
           join pu in db.Purchases on c.PurchaseId equals pu.Id into purchaseMatches
           from pu in purchaseMatches.DefaultIfEmpty()
           select new ConsumptionView(
               c.Id,
               c.WorkspaceId,
               c.PurchaseId,
               c.PlotId,
               p.Name,
               c.SeasonId,
               s.Name,
               s.StartDate,
               s.EndDate,
               c.Date,
               c.Product,
               c.ConsumedQuantity,
               c.UnitPrice,
               c.ProportionalCost,
               c.Version,
               c.CreatedAt,
               c.UpdatedAt,
               pu != null ? pu.PurchaseDate : null);

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "Otra persona ha modificado este consumo mientras lo editabas. Refresca para ver la versión actual.");
        }
    }
}
