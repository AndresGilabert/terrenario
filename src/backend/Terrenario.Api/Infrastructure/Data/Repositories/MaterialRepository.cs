using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Materials;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

/// <summary>
/// MVP-708 (<c>P-057</c>) — Adaptador EF Core del vocabulario de materiales (RN-031).
///
/// La unión se resuelve <b>en SQL</b> (<c>UNION ALL</c> + <c>GROUP BY</c>), no juntando en memoria dos
/// listas ya recortadas: con un tope de 20 por lista, un material que fuera el 21.º en compras y el
/// 21.º en consumos se quedaría fuera aunque sumando fuese de los más usados. Es el mismo motivo por
/// el que <c>MVP-506</c> bajó la mezcla del diario a la base de datos.
/// </summary>
public sealed class MaterialRepository(TerrenarioDbContext db) : IMaterialRepository
{
    public async Task<IReadOnlyList<MaterialSuggestion>> ListSuggestionsAsync(
        Guid workspaceId,
        string? search,
        int limit,
        CancellationToken ct = default)
    {
        var written = db.Purchases
            .Where(p => p.WorkspaceId == workspaceId && p.DeletedAt == null)
            .Select(p => p.Product)
            .Concat(db.PurchaseConsumptions
                // Solo los consumos **sin compra previa** (RN-032). Una imputación copia el material
                // de su compra, así que no puede aportar un nombre nuevo: contarla solo inflaría el
                // recuento y ordenaría el vocabulario por «cuánto se repartió» en vez de por «cuánto
                // se escribió», que es lo que hace útil la sugerencia.
                .Where(c => c.WorkspaceId == workspaceId && c.DeletedAt == null && c.PurchaseId == null)
                .Select(c => c.Product));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim().ToLower();
            written = written.Where(product => product.ToLower().Contains(needle));
        }

        // Se agrupa por el texto tal cual se escribió: normalizar aquí escondería que el Workspace
        // tiene «Abono NPK» y «abono npk», que es justo lo que las sugerencias ayudan a evitar de aquí
        // en adelante sin reescribir el histórico.
        //
        // La agrupación se proyecta a un tipo anónimo y no directamente a `MaterialSuggestion`: EF no
        // sabe traducir un `ORDER BY` sobre los miembros de un record posicional (lección de `P-014`).
        // El mapeo al tipo del dominio se hace ya en memoria, sobre las pocas filas del `Take`.
        var rows = await written
            .GroupBy(product => product)
            .Select(g => new { Product = g.Key, TimesUsed = g.Count() })
            .OrderByDescending(x => x.TimesUsed)
            .ThenBy(x => x.Product)
            .Take(limit)
            .ToListAsync(ct);

        return rows.Select(x => new MaterialSuggestion(x.Product, x.TimesUsed)).ToList();
    }
}
