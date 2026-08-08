using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Application.Diary;
using Terrenario.Api.Domain.Diary;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

/// <summary>
/// MVP-506 — Diario unificado resuelto <b>en SQL</b>.
///
/// Hasta ahora la mezcla se hacía en memoria sobre lo que devolvían los cuatro puertos operativos
/// (MVP-305). Era equivalente mientras no hubiera paginación —en los dos casos se traían todas las
/// filas del rango— pero deja de serlo en cuanto la hay: <b>paginar sobre cuatro listas ya
/// materializadas no es paginar</b>, y buscar sobre una página no es buscar (`P-051`, `P-052`).
///
/// Cada entidad se proyecta a la forma común <see cref="DiaryRow"/> y las cuatro se unen con
/// <c>UNION ALL</c>; el orden, el recorte de página y los totales los resuelve la base de datos. Son
/// <b>dos</b> consultas por petición: la página y los agregados. Los totales se calculan sobre el
/// conjunto filtrado completo, no sobre la página, porque son la cabecera del muro y cambiarían en
/// cada avance si contaran solo lo visible.
///
/// Los filtros se aplican <b>antes</b> de la unión, sobre columnas reales de cada tabla: es lo que EF
/// sabe traducir (lección de <c>P-014</c>) y además evita construir filas que se van a descartar.
/// </summary>
public sealed class DiaryRepository(TerrenarioDbContext db) : IDiaryRepository
{
    public async Task<IReadOnlyList<DiaryRow>> ListPageAsync(
        Guid workspaceId,
        DiaryFilter filter,
        DiaryPageRequest page,
        CancellationToken ct = default)
        => await Union(workspaceId, filter)
            // RN-033 — fecha de negocio descendente y, a igualdad, lo capturado más tarde primero:
            // es el orden en que la persona recuerda haberlo apuntado.
            .OrderByDescending(row => row.Date)
            .ThenByDescending(row => row.CreatedAt)
            // El desempate final por id hace la paginación estable: sin él, dos filas con la misma
            // fecha y el mismo instante de captura podrían repetirse o perderse entre páginas.
            .ThenBy(row => row.Id)
            .Skip(page.Skip)
            .Take(page.Limit)
            .ToListAsync(ct);

    public async Task<DiaryTotals> GetTotalsAsync(
        Guid workspaceId,
        DiaryFilter filter,
        CancellationToken ct = default)
    {
        // Una sola agregación por (tipo, tiene-compra) resuelve los ocho números de la cabecera. El
        // corte por `HasPurchase` es el que separa gasto real de reparto (`R-01` de MVP-399) y el que
        // cuenta los consumos sin compra previa (RN-032).
        var groups = await Union(workspaceId, filter)
            .GroupBy(row => new { row.Type, row.HasPurchase })
            .Select(group => new
            {
                group.Key.Type,
                group.Key.HasPurchase,
                Count = group.Count(),
                Cost = group.Sum(row => row.Cost),
                Kgs = group.Sum(row => row.Kgs ?? 0m),
                // MVP-707 — Ingreso y cuántas partidas lo aportan. El contador es lo que permite
                // distinguir «cero euros» de «ninguna partida tiene precio» (CA-5).
                Income = group.Sum(row => row.Amount ?? 0m),
                WithPrice = group.Count(row => row.Amount != null)
            })
            .ToListAsync(ct);

        int CountOf(string type) => groups.Where(g => g.Type == type).Sum(g => g.Count);

        return new DiaryTotals(
            Total: groups.Sum(g => g.Count),
            Activities: CountOf(DiaryEntryTypes.Activity),
            Purchases: CountOf(DiaryEntryTypes.Purchase),
            Consumptions: CountOf(DiaryEntryTypes.Consumption),
            Harvests: CountOf(DiaryEntryTypes.Harvest),
            TotalKg: groups.Sum(g => g.Kgs),
            // `HasPurchase` solo es `true` en las imputaciones: en labores, compras y cosechas es
            // nulo, y en un consumo sin compra —que sí es gasto real, aunque hoy valga 0 por RN-032—
            // es `false`.
            TotalCost: groups.Where(g => g.HasPurchase != true).Sum(g => g.Cost),
            ImputedCost: groups.Where(g => g.HasPurchase == true).Sum(g => g.Cost),
            ConsumptionsWithoutPurchase: groups.Where(g => g.HasPurchase == false).Sum(g => g.Count),
            // `null` y no `0` cuando ninguna partida tiene precio: la campaña no ha ingresado cero,
            // es que no se sabe. Afirmar el cero sería afirmar algo falso (CA-5).
            TotalIncome: groups.Sum(g => g.WithPrice) == 0 ? null : groups.Sum(g => g.Income),
            HarvestsWithPrice: groups.Sum(g => g.WithPrice));
    }

    /// <summary>
    /// La unión de los tipos pedidos. Solo se consulta lo que se va a mostrar: filtrar por tipo debe
    /// ahorrar trabajo, no solo ocultarlo después.
    /// </summary>
    private IQueryable<DiaryRow> Union(Guid workspaceId, DiaryFilter filter)
    {
        var sources = new List<IQueryable<DiaryRow>>();

        if (Includes(filter, DiaryEntryTypes.Activity)) sources.Add(Activities(workspaceId, filter));
        if (Includes(filter, DiaryEntryTypes.Harvest)) sources.Add(Harvests(workspaceId, filter));
        if (Includes(filter, DiaryEntryTypes.Purchase)) sources.Add(Purchases(workspaceId, filter));
        if (Includes(filter, DiaryEntryTypes.Consumption)) sources.Add(Consumptions(workspaceId, filter));

        // Puede no quedar ninguna fuente: p. ej. filtrar por responsable y por tipo «compra» a la vez.
        // Devolver una consulta vacía es más honesto que inventarse un resultado.
        if (sources.Count == 0) return Empty();

        return sources.Aggregate((left, right) => left.Concat(right));
    }

    private static bool Includes(DiaryFilter filter, string type)
        => filter.Types is null || filter.Types.Count == 0 || filter.Types.Contains(type);

    private IQueryable<DiaryRow> Activities(Guid workspaceId, DiaryFilter filter)
    {
        var live = db.Activities.Where(a => a.WorkspaceId == workspaceId && a.DeletedAt == null);

        if (filter.From is { } from) live = live.Where(a => a.Date >= from);
        if (filter.To is { } to) live = live.Where(a => a.Date <= to);
        if (filter.PlotId is { } plotId) live = live.Where(a => a.PlotId == plotId);
        if (filter.PlotIds is { Count: > 0 } plotIds) live = live.Where(a => plotIds.Contains(a.PlotId));
        if (filter.SeasonId is { } seasonId) live = live.Where(a => a.SeasonId == seasonId);
        if (filter.WorkerId is { } workerId) live = live.Where(a => a.WorkerId == workerId);

        var rows = from a in live
                   join p in db.Plots on a.PlotId equals p.Id
                   join s in db.Seasons on a.SeasonId equals s.Id
                   join w in db.Workers on a.WorkerId equals w.Id
                   // La tarea entra con LEFT JOIN porque puede ser texto libre y no existir en el
                   // catálogo (RN-025).
                   join t in db.Tasks on a.TaskId equals t.Id into taskMatches
                   from t in taskMatches.DefaultIfEmpty()
                   select new DiaryRow
                   {
                       Type = DiaryEntryTypes.Activity,
                       Id = a.Id,
                       Date = a.Date,
                       Title = (t != null ? t.Name : a.TaskText) ?? string.Empty,
                       Description = a.Description,
                       PlotId = a.PlotId,
                       PlotName = p.Name,
                       SeasonId = a.SeasonId,
                       SeasonName = s.Name,
                       SeasonStartDate = s.StartDate,
                       SeasonEndDate = s.EndDate,
                       Cost = a.ManualCost,
                       Version = a.Version,
                       CreatedAt = a.CreatedAt,
                       WorkerName = w.Name,
                       Hours = a.Hours,
                       TaskId = a.TaskId,
                       Quantity = null,
                       HasPurchase = null,
                       PurchaseDate = null,
                       Kgs = null,
                       Destination = null,
                       Yield = null,
                       Amount = null
                   };

        if (Needle(filter) is { } needle)
            rows = rows.Where(r =>
                r.Title.ToLower().Contains(needle)
                || (r.PlotName != null && r.PlotName.ToLower().Contains(needle))
                || (r.WorkerName != null && r.WorkerName.ToLower().Contains(needle))
                || (r.Description != null && r.Description.ToLower().Contains(needle)));

        return rows;
    }

    private IQueryable<DiaryRow> Harvests(Guid workspaceId, DiaryFilter filter)
    {
        // Una cosecha no tiene responsable: filtrar por él la deja fuera por definición.
        if (filter.WorkerId is not null) return Empty();

        var live = db.Harvests.Where(h => h.WorkspaceId == workspaceId && h.DeletedAt == null);

        if (filter.From is { } from) live = live.Where(h => h.Date >= from);
        if (filter.To is { } to) live = live.Where(h => h.Date <= to);
        if (filter.PlotId is { } plotId) live = live.Where(h => h.PlotId == plotId);
        if (filter.PlotIds is { Count: > 0 } plotIds) live = live.Where(h => plotIds.Contains(h.PlotId));
        if (filter.SeasonId is { } seasonId) live = live.Where(h => h.SeasonId == seasonId);

        var rows = from h in live
                   join p in db.Plots on h.PlotId equals p.Id
                   join s in db.Seasons on h.SeasonId equals s.Id
                   select new DiaryRow
                   {
                       Type = DiaryEntryTypes.Harvest,
                       Id = h.Id,
                       Date = h.Date,
                       Title = h.Product,
                       Description = null,
                       PlotId = h.PlotId,
                       PlotName = p.Name,
                       SeasonId = h.SeasonId,
                       SeasonName = s.Name,
                       SeasonStartDate = s.StartDate,
                       SeasonEndDate = s.EndDate,
                       // Una cosecha **no tiene coste**: RN-029 deja fuera precio y molturación. No es
                       // «gratis» ni «desconocido», es que la magnitud no aplica.
                       Cost = 0m,
                       Version = h.Version,
                       CreatedAt = h.CreatedAt,
                       WorkerName = null,
                       Hours = null,
                       TaskId = null,
                       Quantity = null,
                       HasPurchase = null,
                       PurchaseDate = null,
                       Kgs = h.Kgs,
                       Destination = h.Destination,
                       // MVP-402 — rendimiento efectivo: el declarado o el que se deduce de los litros
                       // obtenidos (RN-014). Para quien lee es el mismo dato.
                       Yield = h.Yield ?? (h.Liters != null && h.Kgs > 0 ? h.Liters * 100m / h.Kgs : null),
                       // MVP-707 — Importe ingresado, derivado en la propia consulta: kilos × precio.
                       Amount = h.UnitPrice != null ? h.Kgs * h.UnitPrice : null
                   };

        if (Needle(filter) is { } needle)
            rows = rows.Where(r =>
                r.Title.ToLower().Contains(needle)
                || (r.PlotName != null && r.PlotName.ToLower().Contains(needle)));

        return rows;
    }

    private IQueryable<DiaryRow> Purchases(Guid workspaceId, DiaryFilter filter)
    {
        // Una compra es del Workspace, no de un terreno ni de una persona: los dos filtros la dejan
        // fuera por definición, no por olvido. El reparto por terrenos es la imputación (MVP-304).
        // MVP-707 — `PlotIds` cuenta igual que `PlotId`: acotar por terrenos deja la compra fuera.
        if (filter.PlotId is not null || filter.PlotIds is { Count: > 0 } || filter.WorkerId is not null)
            return Empty();

        var live = db.Purchases.Where(p => p.WorkspaceId == workspaceId && p.DeletedAt == null);

        if (filter.From is { } from) live = live.Where(p => p.PurchaseDate >= from);
        if (filter.To is { } to) live = live.Where(p => p.PurchaseDate <= to);
        if (filter.SeasonId is { } seasonId) live = live.Where(p => p.SeasonId == seasonId);

        var rows = from p in live
                   join s in db.Seasons on p.SeasonId equals s.Id
                   select new DiaryRow
                   {
                       Type = DiaryEntryTypes.Purchase,
                       Id = p.Id,
                       Date = p.PurchaseDate,
                       Title = p.Product,
                       Description = null,
                       // Una compra es del Workspace, no de un terreno: el reparto es la imputacion.
                       PlotId = null,
                       PlotName = null,
                       SeasonId = p.SeasonId,
                       SeasonName = s.Name,
                       SeasonStartDate = s.StartDate,
                       SeasonEndDate = s.EndDate,
                       Cost = p.TotalCost,
                       Version = p.Version,
                       CreatedAt = p.CreatedAt,
                       WorkerName = null,
                       Hours = null,
                       TaskId = null,
                       Quantity = p.TotalQuantity,
                       HasPurchase = null,
                       PurchaseDate = null,
                       Kgs = null,
                       Destination = null,
                       Yield = null,
                       Amount = null
                   };

        if (Needle(filter) is { } needle)
            rows = rows.Where(r => r.Title.ToLower().Contains(needle));

        return rows;
    }

    private IQueryable<DiaryRow> Consumptions(Guid workspaceId, DiaryFilter filter)
    {
        if (filter.WorkerId is not null) return Empty();

        var live = db.PurchaseConsumptions.Where(c => c.WorkspaceId == workspaceId && c.DeletedAt == null);

        if (filter.From is { } from) live = live.Where(c => c.Date >= from);
        if (filter.To is { } to) live = live.Where(c => c.Date <= to);
        if (filter.PlotId is { } plotId) live = live.Where(c => c.PlotId == plotId);
        if (filter.PlotIds is { Count: > 0 } plotIds) live = live.Where(c => plotIds.Contains(c.PlotId));
        if (filter.SeasonId is { } seasonId) live = live.Where(c => c.SeasonId == seasonId);

        var rows = from c in live
                   join p in db.Plots on c.PlotId equals p.Id
                   join s in db.Seasons on c.SeasonId equals s.Id
                   // MVP-708 (RN-043) — LEFT JOIN porque el consumo puede no tener compra (RN-032).
                   // Solo se trae la fecha, y solo para poder avisar de un consumo anterior a ella:
                   // el coste y el material siguen siendo los que el propio consumo congeló.
                   join pu in db.Purchases on c.PurchaseId equals pu.Id into purchaseMatches
                   from pu in purchaseMatches.DefaultIfEmpty()
                   select new DiaryRow
                   {
                       Type = DiaryEntryTypes.Consumption,
                       Id = c.Id,
                       Date = c.Date,
                       Title = c.Product,
                       Description = null,
                       PlotId = c.PlotId,
                       PlotName = p.Name,
                       SeasonId = c.SeasonId,
                       SeasonName = s.Name,
                       SeasonStartDate = s.StartDate,
                       SeasonEndDate = s.EndDate,
                       Cost = c.ProportionalCost,
                       Version = c.Version,
                       CreatedAt = c.CreatedAt,
                       WorkerName = null,
                       Hours = null,
                       TaskId = null,
                       Quantity = c.ConsumedQuantity,
                       // `false` implica consumo sin compra previa: su coste es desconocido, no cero (RN-032).
                       HasPurchase = c.PurchaseId != null,
                       PurchaseDate = pu != null ? pu.PurchaseDate : null,
                       Kgs = null,
                       Destination = null,
                       Yield = null,
                       Amount = null
                   };

        if (Needle(filter) is { } needle)
            rows = rows.Where(r =>
                r.Title.ToLower().Contains(needle)
                || (r.PlotName != null && r.PlotName.ToLower().Contains(needle)));

        return rows;
    }

    /// <summary>
    /// Consulta vacía con la forma de <see cref="DiaryRow"/>, para excluir un tipo entero de la unión.
    /// Reutiliza la proyección de actividades sobre un Workspace imposible: la forma tiene que ser una
    /// proyección real, no una constante, para que EF pueda unirla con las demás.
    /// </summary>
    private IQueryable<DiaryRow> Empty() => Activities(Guid.Empty, new DiaryFilter());

    /// <summary>Término de búsqueda normalizado, o <c>null</c> si no hay búsqueda.</summary>
    private static string? Needle(DiaryFilter filter)
        => string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim().ToLowerInvariant();
}
