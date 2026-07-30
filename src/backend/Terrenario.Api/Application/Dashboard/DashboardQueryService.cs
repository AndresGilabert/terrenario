using Terrenario.Api.Domain.Harvests;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Dashboard;

/// <summary>Resumen de temporada (MVP-403, CA-1). Todas las cifras son de lo que hay en el ámbito.</summary>
public sealed record SeasonSummary(
    DashboardScope Scope,
    /// <summary>Kilos recolectados. Es la única cifra que siempre existe si hay cosechas.</summary>
    decimal TotalKg,
    /// <summary>
    /// Litros de aceite «cuando exista dato»: declarados o derivados del rendimiento (RN-014).
    /// <c>null</c> si ninguna partida aporta dato de aceite, que no es lo mismo que cero litros.
    /// </summary>
    decimal? TotalLiters,
    /// <summary>
    /// Rendimiento medio en L/100kg (RN-013), <b>ponderado por kilos</b>. <c>null</c> si ninguna
    /// partida tiene dato de aceite.
    /// </summary>
    decimal? AverageYield,
    int Harvests,
    /// <summary>
    /// Partidas que aportan dato de aceite. Junto a <see cref="Harvests"/> permite decir sobre cuántas
    /// se ha promediado, en vez de presentar una media que parece de todo.
    /// </summary>
    int HarvestsWithOilData);

/// <summary>Kilos por destino (MVP-403, CA-2). Taxonomía cerrada de RN-012, incluido `desconocido`.</summary>
public sealed record DestinationTotal(string Destination, decimal Kg);

/// <summary>Producción agregada de una temporada (P-021): lo que enriquece la tarjeta del maestro.</summary>
public sealed record SeasonProduction(Guid SeasonId, string SeasonName, decimal TotalKg, int Harvests);

/// <summary>
/// MVP-403 — Cálculo de los widgets del dashboard sobre las cosechas del Workspace.
///
/// <b>Una sola lectura por petición, y la agregación en memoria.</b> El puerto devuelve las filas
/// mínimas del ámbito y aquí se suman. Es una decisión consciente y acotada:
/// <list type="bullet">
/// <item>La KB exige que resumen y gráficos <b>no se contradigan</b> entre sí. Agregando sobre un único
/// conjunto de filas eso se cumple por construcción, sin depender de que cuatro consultas vean el mismo
/// estado.</item>
/// <item>El volumen del MVP lo permite: una campaña son decenas o centenares de partidas, no millones.
/// Es el mismo criterio que tomó <c>MVP-305</c> para mezclar el diario en memoria.</item>
/// <item>Cuando deje de bastar, la agregación está detrás de un único método del puerto
/// (<c>ListAggregateRowsAsync</c>): mover los <c>SUM</c>/<c>GROUP BY</c> a SQL —la evolución que ya
/// prevé <c>ADR-0004</c>— no toca a estos llamantes.</item>
/// </list>
///
/// <b>El rendimiento medio se pondera por kilos.</b> Una media aritmética daría el mismo peso a una
/// partida de 50 kg que a una de 5.000, que es exactamente la lectura equivocada: el rendimiento de una
/// campaña es el de todo el aceite sobre toda la aceituna, no el promedio de sus recibos.
/// </summary>
public sealed class DashboardQueryService(
    IHarvestRepository harvestRepository,
    ISeasonRepository seasonRepository,
    DashboardScopeResolver scopeResolver)
{
    public async Task<SeasonSummary> GetSummaryAsync(
        Guid workspaceId,
        DashboardRequest request,
        CancellationToken ct = default)
    {
        var scope = await scopeResolver.ResolveAsync(workspaceId, request, ct);
        var rows = await LoadAsync(workspaceId, scope, ct);

        var withOil = rows.Where(row => row.HasOilData).ToList();
        var kgWithOil = withOil.Sum(row => row.Kgs);
        var liters = withOil.Sum(row => row.EffectiveLiters ?? 0m);

        return new SeasonSummary(
            scope,
            decimal.Round(rows.Sum(row => row.Kgs), 2, MidpointRounding.AwayFromZero),
            // «Cuando exista dato»: sin partidas con aceite el valor es desconocido, no cero.
            withOil.Count == 0 ? null : decimal.Round(liters, 2, MidpointRounding.AwayFromZero),
            kgWithOil <= 0 ? null : decimal.Round(liters / kgWithOil * 100m, 2, MidpointRounding.AwayFromZero),
            rows.Count,
            withOil.Count);
    }

    public async Task<(DashboardScope Scope, IReadOnlyList<DestinationTotal> Totals, decimal TotalKg)>
        GetKgByDestinationAsync(Guid workspaceId, DashboardRequest request, CancellationToken ct = default)
    {
        var scope = await scopeResolver.ResolveAsync(workspaceId, request, ct);
        var rows = await LoadAsync(workspaceId, scope, ct);

        // Solo se devuelven los destinos **presentes**: enseñar categorías a cero llenaría el widget de
        // ruido. Lo que la taxonomía cerrada garantiza (CA-2) es que las claves salen del catálogo de
        // RN-012 y no de texto libre, no que haya que pintarlas todas.
        var totals = rows
            .GroupBy(row => row.Destination)
            .Select(group => new DestinationTotal(
                group.Key, decimal.Round(group.Sum(row => row.Kgs), 2, MidpointRounding.AwayFromZero)))
            // Kilos descendentes y desempate alfabético por la clave canónica: mismo criterio que
            // RN-011 impone al widget de terrenos, para que las dos listas se lean igual.
            .OrderByDescending(total => total.Kg)
            .ThenBy(total => total.Destination, StringComparer.Ordinal)
            .ToList();

        return (scope, totals, decimal.Round(rows.Sum(row => row.Kgs), 2, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// P-021 — Producción agregada por temporada, para enriquecer las tarjetas del maestro de
    /// temporadas (MVP-203 las omitió deliberadamente porque no existía <c>HARVEST</c>).
    ///
    /// Va sin filtro de terreno a propósito: la tarjeta habla de la campaña completa, no de un
    /// subconjunto. Y en una sola petición, para que el maestro no haga una por temporada.
    /// </summary>
    public async Task<IReadOnlyList<SeasonProduction>> GetKgBySeasonAsync(
        Guid workspaceId,
        CancellationToken ct = default)
    {
        var seasons = await seasonRepository.ListByWorkspaceAsync(workspaceId, ct);
        var rows = await harvestRepository.ListAggregateRowsAsync(
            workspaceId, new HarvestAggregateFilter(), ct);

        var bySeason = rows
            .GroupBy(row => row.SeasonId)
            .ToDictionary(group => group.Key, group => (Kg: group.Sum(r => r.Kgs), Count: group.Count()));

        // Se recorren las **temporadas**, no los grupos: una campaña sin cosechas debe aparecer con 0,
        // que es información («no se recolectó nada»), no ausencia de dato.
        return seasons
            .Select(season =>
            {
                var found = bySeason.TryGetValue(season.Id, out var totals) ? totals : (Kg: 0m, Count: 0);
                return new SeasonProduction(
                    season.Id,
                    season.Name,
                    decimal.Round(found.Kg, 2, MidpointRounding.AwayFromZero),
                    found.Count);
            })
            .ToList();
    }

    /// <summary>
    /// Sin temporada resoluble no se consulta nada: un Workspace sin campaña no tiene resumen vacío,
    /// tiene un ámbito imposible, y la respuesta lo dice en vez de devolver ceros que parecen datos.
    /// Lo mismo si el filtro deja el conjunto de terrenos vacío.
    /// </summary>
    private async Task<IReadOnlyList<HarvestAggregateRow>> LoadAsync(
        Guid workspaceId,
        DashboardScope scope,
        CancellationToken ct)
        => scope.IsResolvable && scope.Plots.Count > 0
            ? await harvestRepository.ListAggregateRowsAsync(workspaceId, scope.ToFilter(), ct)
            : [];
}
