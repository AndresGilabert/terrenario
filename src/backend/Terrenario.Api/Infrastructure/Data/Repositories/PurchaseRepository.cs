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
        var rows = await ProjectViews(live.OrderByDescending(p => p.PurchaseDate)).ToListAsync(ct);

        // Desempate por fecha de captura en memoria: EF+SQLite no traduce `ORDER BY` sobre
        // `DateTimeOffset` (P-031), y degradar la consulta de producción por el arnés sería el error
        // que ese punto describe.
        return rows
            .OrderByDescending(v => v.PurchaseDate)
            .ThenByDescending(v => v.CreatedAt)
            .ToList();
    }

    public Task<PurchaseView?> GetViewAsync(Guid workspaceId, Guid purchaseId, CancellationToken ct = default)
        => ProjectViews(LivePurchases(workspaceId).Where(p => p.Id == purchaseId)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ProductSuggestion>> ListProductSuggestionsAsync(
        Guid workspaceId,
        string? search,
        int limit,
        CancellationToken ct = default)
    {
        var live = LivePurchases(workspaceId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim().ToLower();
            live = live.Where(p => p.Product.ToLower().Contains(needle));
        }

        // Se agrupa por el texto tal cual se escribió: normalizar aquí escondería que el Workspace
        // tiene «Abono NPK» y «abono npk», que es justo lo que las sugerencias ayudan a evitar a
        // partir de ahora sin reescribir el histórico.
        //
        // La agrupación se proyecta a un tipo anónimo y no directamente a `ProductSuggestion`: EF no
        // sabe traducir un `ORDER BY` sobre los miembros de un record posicional, igual que pasaba
        // con `ActivityView` en MVP-301 (lección de P-014). El mapeo al tipo del dominio se hace ya
        // en memoria, sobre las pocas filas que devuelve el `Take`.
        var rows = await live
            .GroupBy(p => p.Product)
            .Select(g => new { Product = g.Key, TimesUsed = g.Count() })
            .OrderByDescending(x => x.TimesUsed)
            .ThenBy(x => x.Product)
            .Take(limit)
            .ToListAsync(ct);

        return rows.Select(x => new ProductSuggestion(x.Product, x.TimesUsed)).ToList();
    }

    /// <summary>Compras vivas del Workspace: el filtro de baja lógica en un único sitio (RN-037).</summary>
    private IQueryable<Purchase> LivePurchases(Guid workspaceId)
        => db.Purchases.Where(p => p.WorkspaceId == workspaceId && p.DeletedAt == null);

    /// <summary>
    /// Proyección de lectura: resuelve el nombre y el rango de la temporada en la misma consulta, que
    /// es lo que necesita el aviso de RN-023 y la etiqueta de campaña del libro de compras.
    /// </summary>
    private IQueryable<PurchaseView> ProjectViews(IQueryable<Purchase> purchases)
        => from p in purchases
           join s in db.Seasons on p.SeasonId equals s.Id
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
               p.UpdatedAt);

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
