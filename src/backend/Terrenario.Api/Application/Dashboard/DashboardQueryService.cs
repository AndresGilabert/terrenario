using System.Globalization;
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

/// <summary>Kilos de un terreno (MVP-404, CA-1). El orden de la lista lo fija RN-011.</summary>
public sealed record PlotTotal(Guid PlotId, string PlotName, decimal Kg);

/// <summary>Granularidad temporal de la evolución de rendimiento (MVP-404).</summary>
public enum YieldGranularity { Month, Week }

/// <summary>
/// Un punto de la serie de evolución de rendimiento (MVP-404, CA-2). <see cref="Yield"/> es el
/// rendimiento medio del periodo en la unidad canónica L/100kg (RN-013), ponderado por kilos.
/// </summary>
public sealed record YieldPoint(string Period, decimal Yield, decimal Kg);

/// <summary>
/// Comparativa histórica básica (MVP-404, RN-015). Cada media es <c>null</c> mientras no haya
/// «histórico suficiente»: la general aparece con una temporada previa con dato; las de 5 y 10
/// temporadas, solo con al menos 5 y 10.
/// </summary>
public sealed record YieldHistory(
    decimal? Average,
    decimal? Average5Seasons,
    decimal? Average10Seasons,
    int PriorSeasonsWithData);

/// <summary>Evolución de rendimiento del ámbito y su comparativa histórica (MVP-404).</summary>
public sealed record YieldEvolution(
    DashboardScope Scope,
    YieldGranularity Granularity,
    IReadOnlyList<YieldPoint> Series,
    YieldHistory History);

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
        var liters = withOil.Sum(row => row.EffectiveLiters ?? 0m);

        return new SeasonSummary(
            scope,
            decimal.Round(rows.Sum(row => row.Kgs), 2, MidpointRounding.AwayFromZero),
            // «Cuando exista dato»: sin partidas con aceite el valor es desconocido, no cero.
            withOil.Count == 0 ? null : decimal.Round(liters, 2, MidpointRounding.AwayFromZero),
            WeightedYield(rows),
            rows.Count,
            withOil.Count);
    }

    /// <summary>
    /// Rendimiento medio en L/100kg (RN-013) <b>ponderado por kilos</b>: litros de aceite (declarados o
    /// derivados) sobre kilos de aceituna, no la media de los rendimientos de cada partida. <c>null</c>
    /// si ninguna partida aporta dato de aceite. Es la única forma correcta de promediar un ratio, y por
    /// eso vive en un solo sitio: el resumen, cada terreno y cada periodo de la evolución lo calculan
    /// igual.
    /// </summary>
    private static decimal? WeightedYield(IEnumerable<HarvestAggregateRow> rows)
    {
        var withOil = rows.Where(row => row.HasOilData).ToList();
        var kg = withOil.Sum(row => row.Kgs);
        if (kg <= 0) return null;

        var liters = withOil.Sum(row => row.EffectiveLiters ?? 0m);
        return decimal.Round(liters / kg * 100m, 2, MidpointRounding.AwayFromZero);
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
    /// MVP-404 (CA-1) — Kilos por terreno del ámbito. El orden lo fija RN-011: <b>kg descendentes y
    /// desempate alfabético</b> por nombre de terreno, sin orden manual.
    ///
    /// Solo aparecen los terrenos que <b>han producido</b>, igual que kg por destino solo lista los
    /// destinos presentes (MVP-403): un terreno del ámbito sin cosechas sería una barra a cero, ruido en
    /// un gráfico que existe para comparar quién aporta más. El nombre se resuelve del propio ámbito, ya
    /// cargado, sin un <c>JOIN</c> extra.
    /// </summary>
    public async Task<(DashboardScope Scope, IReadOnlyList<PlotTotal> Totals, decimal TotalKg)>
        GetKgByPlotAsync(Guid workspaceId, DashboardRequest request, CancellationToken ct = default)
    {
        var scope = await scopeResolver.ResolveAsync(workspaceId, request, ct);
        var rows = await LoadAsync(workspaceId, scope, ct);

        var namesByPlot = scope.Plots.ToDictionary(plot => plot.Id, plot => plot.Name);

        var totals = rows
            .GroupBy(row => row.PlotId)
            .Select(group => new PlotTotal(
                group.Key,
                namesByPlot.TryGetValue(group.Key, out var name) ? name : string.Empty,
                decimal.Round(group.Sum(row => row.Kgs), 2, MidpointRounding.AwayFromZero)))
            // RN-011 — kg descendentes; a igualdad, alfabético por nombre (insensible a mayúsculas para
            // que el desempate sea el que una persona espera al leer la lista).
            .OrderByDescending(total => total.Kg)
            .ThenBy(total => total.PlotName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return (scope, totals, decimal.Round(rows.Sum(row => row.Kgs), 2, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// MVP-404 (CA-2) — Evolución de rendimiento del ámbito, con su comparativa histórica básica.
    ///
    /// La <b>serie</b> es el rendimiento medio ponderado de la temporada del ámbito, agrupado por mes o
    /// semana (RN-013). Solo aparecen los periodos con dato de aceite: un mes con cosechas pero sin
    /// rendimiento no tiene punto que dibujar, y forzar un cero fingiría una caída que no ocurrió.
    ///
    /// El <b>histórico</b> (RN-015) compara la campaña actual con las anteriores <b>de los mismos
    /// terrenos</b>, para que sea una comparación de las mismas parcelas en años distintos y no de
    /// conjuntos diferentes. La media general aparece con una temporada previa con dato; las de 5 y 10,
    /// solo con al menos 5 y 10 temporadas previas con dato («histórico suficiente», CA-2). En este MVP
    /// una temporada es la campaña anual, así que «5 temporadas» ≈ «5 años»; una distinción más fina
    /// entre año natural y campaña es post-MVP.
    /// </summary>
    public async Task<YieldEvolution> GetYieldEvolutionAsync(
        Guid workspaceId,
        DashboardRequest request,
        YieldGranularity granularity,
        CancellationToken ct = default)
    {
        var scope = await scopeResolver.ResolveAsync(workspaceId, request, ct);

        if (!scope.IsResolvable || scope.Plots.Count == 0)
            return new YieldEvolution(scope, granularity, [], new YieldHistory(null, null, null, 0));

        // Una sola lectura para serie e histórico: los mismos terrenos en **todas** las temporadas. Se
        // reparte en memoria por temporada, que es más barato que dos consultas y no puede descuadrar.
        var plotIds = scope.Plots.Select(plot => plot.Id).ToArray();
        var allRows = await harvestRepository.ListAggregateRowsAsync(
            workspaceId, new HarvestAggregateFilter(null, plotIds), ct);
        var seasons = await seasonRepository.ListByWorkspaceAsync(workspaceId, ct);

        var currentSeasonId = scope.Season!.Id;

        var series = allRows
            .Where(row => row.SeasonId == currentSeasonId)
            .GroupBy(row => PeriodKey(row.Date, granularity))
            .Select(group => new { Period = group.Key, Yield = WeightedYield(group), Kg = group.Sum(r => r.Kgs) })
            // Un periodo sin dato de aceite no es un punto de la serie de rendimiento.
            .Where(point => point.Yield is not null)
            .OrderBy(point => point.Period, StringComparer.Ordinal)
            .Select(point => new YieldPoint(
                point.Period, point.Yield!.Value, decimal.Round(point.Kg, 2, MidpointRounding.AwayFromZero)))
            .ToList();

        return new YieldEvolution(scope, granularity, series, BuildHistory(allRows, seasons, scope.Season!));
    }

    /// <summary>
    /// RN-015 — Medias históricas de rendimiento sobre las temporadas <b>anteriores</b> a la que se mira
    /// (por fecha de inicio), tomando solo las que tienen dato de aceite. La ventana de 5 y 10 se cuenta
    /// sobre temporadas <b>con dato</b>, no sobre el calendario: una media «de 5 años» calculada sobre 2
    /// campañas que por casualidad tienen dato engañaría más que ayudar.
    /// </summary>
    private static YieldHistory BuildHistory(
        IReadOnlyList<HarvestAggregateRow> allRows,
        IReadOnlyList<Season> seasons,
        Season current)
    {
        var rowsBySeason = allRows.GroupBy(row => row.SeasonId).ToDictionary(g => g.Key, g => g.ToList());

        // Temporadas previas con dato de aceite, de la más reciente a la más antigua.
        var priorWithData = seasons
            .Where(season => season.StartDate < current.StartDate)
            .OrderByDescending(season => season.StartDate)
            .Select(season => rowsBySeason.TryGetValue(season.Id, out var rows) ? rows : [])
            .Where(rows => rows.Any(row => row.HasOilData))
            .ToList();

        var allPriorRows = priorWithData.SelectMany(rows => rows);

        return new YieldHistory(
            priorWithData.Count >= 1 ? WeightedYield(allPriorRows) : null,
            priorWithData.Count >= 5 ? WeightedYield(priorWithData.Take(5).SelectMany(r => r)) : null,
            priorWithData.Count >= 10 ? WeightedYield(priorWithData.Take(10).SelectMany(r => r)) : null,
            priorWithData.Count);
    }

    /// <summary>
    /// Clave ordenable del periodo: <c>YYYY-MM</c> por mes, <c>YYYY-Www</c> por semana ISO. El formato
    /// ordena cronológicamente como texto, así que la serie no necesita ordenar por fecha aparte.
    /// </summary>
    private static string PeriodKey(DateOnly date, YieldGranularity granularity)
        => granularity == YieldGranularity.Week
            ? $"{ISOWeek.GetYear(date.ToDateTime(TimeOnly.MinValue)):D4}-W{ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue)):D2}"
            : $"{date.Year:D4}-{date.Month:D2}";

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
