using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Operations;
using Terrenario.Api.Domain.Purchases;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Adaptador EF Core del libro de compras (MVP-303). Repite las dos decisiones que estrenó
/// <c>ActivityRepository</c> en MVP-301: el filtro de baja lógica vive aquí (no en un filtro global de
/// EF) y la colisión de versión de la base de datos se traduce a 409, no a 500.
/// </summary>
public sealed class PurchaseRepository(TerrenarioDbContext db) : IPurchaseRepository
{
    public async Task AddAsync(Purchase purchase, CancellationToken ct = default)
        => await db.Purchases.AddAsync(purchase, ct);

    public Task<Purchase?> FindByIdAsync(Guid workspaceId, Guid purchaseId, CancellationToken ct = default)
        => db.Purchases.FirstOrDefaultAsync(
            p => p.Id == purchaseId && p.WorkspaceId == workspaceId && p.DeletedAt == null, ct);

    public async Task<IReadOnlyList<PurchaseView>> ListAsync(
        Guid workspaceId,
        PurchaseFilter filter,
        CancellationToken ct = default)
    {
        var live = LivePurchases(workspaceId);

        if (filter.SeasonId is { } seasonId) live = live.Where(p => p.SeasonId == seasonId);
        if (filter.From is { } from) live = live.Where(p => p.PurchaseDate >= from);
        if (filter.To is { } to) live = live.Where(p => p.PurchaseDate <= to);
        if (!string.IsNullOrWhiteSpace(filter.Product))
        {
            // Búsqueda parcial e insensible a mayúsculas: el producto es texto libre (RN-031), así que
            // filtrar por igualdad exacta obligaría a recordar cómo se escribió.
            var needle = filter.Product.Trim().ToLower();
            live = live.Where(p => p.Product.ToLower().Contains(needle));
        }

        // Filtros y orden sobre columnas reales antes de proyectar (lección de P-014).
        // Orden completo en SQL desde MVP-501, desempate incluido: antes el `ThenBy` se reaplicaba en
        // memoria solo porque EF+SQLite no lo traducía sobre `DateTimeOffset` (P-031).
        return await ProjectViews(
                live.OrderByDescending(p => p.PurchaseDate).ThenByDescending(p => p.CreatedAt))
            .ToListAsync(ct);
    }

    public Task<PurchaseView?> GetViewAsync(Guid workspaceId, Guid purchaseId, CancellationToken ct = default)
        => ProjectViews(LivePurchases(workspaceId).Where(p => p.Id == purchaseId)).FirstOrDefaultAsync(ct);

    /// <summary>Compras vivas del Workspace: el filtro de baja lógica en un único sitio (RN-037).</summary>
    private IQueryable<Purchase> LivePurchases(Guid workspaceId)
        => db.Purchases.Where(p => p.WorkspaceId == workspaceId && p.DeletedAt == null);

    /// <summary>
    /// Proyección de lectura: resuelve el nombre y el rango de la temporada en la misma consulta, que
    /// es lo que necesita el aviso de RN-023 y la etiqueta de campaña del libro de compras.
    ///
    /// MVP-804 — Y la autoría, con <c>LEFT JOIN</c> por el mismo motivo que en actividades: sin FK
    /// hacia <c>users</c>, una cuenta purgada por RN-041 dejaría la compra fuera del listado si el
    /// <c>JOIN</c> fuera interno.
    /// </summary>
    private IQueryable<PurchaseView> ProjectViews(IQueryable<Purchase> purchases)
        => from p in purchases
           join s in db.Seasons on p.SeasonId equals s.Id
           join cb in db.Users on p.CreatedBy equals cb.Id into createdByMatches
           from cb in createdByMatches.DefaultIfEmpty()
           join ub in db.Users on p.UpdatedBy equals ub.Id into updatedByMatches
           from ub in updatedByMatches.DefaultIfEmpty()
           select new PurchaseView(
               p.Id,
               p.WorkspaceId,
               p.SeasonId,
               s.Name,
               s.StartDate,
               s.EndDate,
               p.PurchaseDate,
               p.Product,
               p.TotalQuantity,
               p.TotalCost,
               p.UnitPrice,
               p.Version,
               p.CreatedAt,
               p.UpdatedAt,
               // MVP-804 (CA-3) — La cuenta dada de baja no devuelve nombre **antes** de mirar qué
               // guarda su `display_name`: quien lo rotula es `RecordAuthor.NameOf`.
               cb != null && cb.DeletedAt == null ? cb.DisplayName : null,
               ub != null && ub.DeletedAt == null ? ub.DisplayName : null);

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "Otra persona ha modificado esta compra mientras la editabas. Refresca para ver la versión actual.");
        }
    }
}
