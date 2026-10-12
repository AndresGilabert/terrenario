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
    int HarvestsWithOilData,
    /// <summary>
    /// MVP-405 (CA-3, RN-010) — Kilos por árbol del ámbito: Σkg / Σárboles de los terrenos que han
    /// producido <b>y tienen</b> número de árboles. <c>null</c> si ninguno de los terrenos con cosechas
    /// tiene <c>tree_count</c> (dato desconocido, no cero).
    /// </summary>
    decimal? KgPerTree,
    /// <summary>Árboles sobre los que se ha calculado <see cref="KgPerTree"/> (denominador del KPI).</summary>
    int TreesCounted,
    /// <summary>Terrenos con cosechas incluidos en <see cref="KgPerTree"/> (tienen número de árboles).</summary>
    int PlotsCounted,
    /// <summary>
    /// Terrenos con cosechas <b>excluidos</b> del KPI por no tener número de árboles (RN-010). Si es
    /// <c>&gt; 0</c>, el widget avisa de que <see cref="KgPerTree"/> es un dato incompleto.
    /// </summary>
    int PlotsWithoutTreeCount);

/// <summary>Kilos por destino (MVP-403, CA-2). Taxonomía cerrada de RN-012, incluido `desconocido`.</summary>
public sealed record DestinationTotal(string Destination, decimal Kg);

/// <summary>Producción agregada de una temporada (P-021): lo que enriquece la tarjeta del maestro.</summary>
public sealed record SeasonProduction(Guid SeasonId, string SeasonName, decimal TotalKg, int Harvests);

/// <summary>Kilos de un terreno (MVP-404, CA-1). El orden de la lista lo fija RN-011.</summary>
public sealed record PlotTotal(Guid PlotId, string PlotName, decimal Kg);

/// <summary>
/// Un punto de la serie de evolución de rendimiento (PRD-101): un día cosechado. <see cref="Yield"/> es
/// el rendimiento del día en L/100kg (RN-013), ponderado por kilos si hubo varias cosechas.
/// </summary>
public sealed record YieldPoint(DateOnly Date, decimal Yield, decimal Kg);

/// <summary>
/// Referencia histórica (PRD-101, RN-015): rectas de regresión de los mismos días de años anteriores,
/// sobre las mismas parcelas. Cada recta es <c>null</c> mientras no haya «histórico suficiente»:
/// <see cref="General"/> con un año previo con dato; <see cref="Last5Years"/> y <see cref="Last10Years"/>,
/// solo cuando el histórico llega al menos 5 y 10 años atrás.
/// </summary>
public sealed record YieldHistory(
    TrendLine? General,
    TrendLine? Last5Years,
    TrendLine? Last10Years,
    int PriorYearsWithData)
{
    public static readonly YieldHistory Empty = new(null, null, null, 0);
}

/// <summary>Evolución de rendimiento del ámbito, su tendencia y su referencia histórica (PRD-101).</summary>
public sealed record YieldEvolution(
    DashboardScope Scope,
    IReadOnlyList<YieldPoint> Series,
    TrendLine? Trend,
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
        Guid userId,
        Guid workspaceId,
        DashboardRequest request,
        CancellationToken ct = default)
    {
        var scope = await scopeResolver.ResolveAsync(userId, workspaceId, request, ct);
        var rows = await LoadAsync(workspaceId, scope, ct);

        var withOil = rows.Where(row => row.HasOilData).ToList();
        var liters = withOil.Sum(row => row.EffectiveLiters ?? 0m);
        var perTree = KgPerTree(rows, scope);

        return new SeasonSummary(
            scope,
            decimal.Round(rows.Sum(row => row.Kgs), 2, MidpointRounding.AwayFromZero),
            // «Cuando exista dato»: sin partidas con aceite el valor es desconocido, no cero.
            withOil.Count == 0 ? null : decimal.Round(liters, 2, MidpointRounding.AwayFromZero),
            WeightedYield(rows),
            rows.Count,
            withOil.Count,
            perTree.KgPerTree,
            perTree.Trees,
            perTree.Plots,
            perTree.Excluded);
    }

    /// <summary>
    /// MVP-405 (CA-3, RN-010) — Kilos por árbol del ámbito. Se calcula sobre los terrenos que <b>han
    /// producido</b> (aparecen en las filas) y <b>tienen</b> número de árboles: Σkg de esos terrenos
    /// entre la suma de sus árboles. Los terrenos con cosechas <b>sin</b> <c>tree_count</c> se excluyen
    /// del numerador y del denominador y se cuentan aparte (<paramref name="rows"/> ⇒
    /// <c>Excluded</c>), para que el widget avise de que el dato es incompleto.
    ///
    /// Un terreno del ámbito sin cosechas no cuenta —no aporta kilos ni interesa su densidad—, así que
    /// el KPI no se diluye con parcelas que no rindieron. <c>KgPerTree</c> es <c>null</c> cuando ningún
    /// terreno con cosechas tiene árboles: dato desconocido, no cero.
    /// </summary>
    private static (decimal? KgPerTree, int Trees, int Plots, int Excluded) KgPerTree(
        IReadOnlyList<HarvestAggregateRow> rows,
        DashboardScope scope)
    {
        var treesByPlot = scope.Plots.ToDictionary(plot => plot.Id, plot => plot.TreeCount);

        int totalTrees = 0, plotsCounted = 0, excluded = 0;
        decimal kgCounted = 0m;
        foreach (var group in rows.GroupBy(row => row.PlotId))
        {
            // El terreno debería estar en el ámbito: las filas ya vienen filtradas por él (LoadAsync).
            // Si no está, no es «dato incompleto», es ajeno al ámbito: se ignora sin sumar al aviso.
            if (!treesByPlot.TryGetValue(group.Key, out var trees))
                continue;

            // RN-010 — con cosechas pero sin número de árboles: fuera del KPI y suma al aviso.
            if (trees is null)
            {
                excluded++;
                continue;
            }

            totalTrees += trees.Value;
            kgCounted += group.Sum(row => row.Kgs);
            plotsCounted++;
        }

        var kgPerTree = totalTrees > 0
            ? decimal.Round(kgCounted / totalTrees, 2, MidpointRounding.AwayFromZero)
            : (decimal?)null;

        return (kgPerTree, totalTrees, plotsCounted, excluded);
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
        GetKgByDestinationAsync(Guid userId, Guid workspaceId, DashboardRequest request, CancellationToken ct = default)
    {
        var scope = await scopeResolver.ResolveAsync(userId, workspaceId, request, ct);
        var rows = await LoadAsync(workspaceId, scope, ct);

        // Solo se devuelven los destinos **presentes**: enseñar categorías a cero llenaría el widget de
        // ruido. Lo que la taxonomía cerrada garantiza (CA-2) es que las claves salen del catálogo de
        // RN-012 y no de texto libre, no que haya que pintarlas todas.
        var totals = rows
            .GroupBy(row => row.Destination)
            .Select(group => new DestinationTotal(
                group.Key, decimal.Round(group.Sum(row => row.Kgs), 2, MidpointRounding.AwayFromZero)))
            // Kilos descendentes y desempate alfabético por la clave canónica: **mismo comparador** que
            // RN-011 impone al widget de terrenos (OrdinalIgnoreCase), para que las dos listas se lean
            // igual (MVP-499, R-06). Las claves de destino son minúsculas canónicas, así que hoy no
            // cambia el resultado; se alinea para que «mismo criterio» sea literal.
            .OrderByDescending(total => total.Kg)
            .ThenBy(total => total.Destination, StringComparer.OrdinalIgnoreCase)
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
        GetKgByPlotAsync(Guid userId, Guid workspaceId, DashboardRequest request, CancellationToken ct = default)
    {
        var scope = await scopeResolver.ResolveAsync(userId, workspaceId, request, ct);
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
    /// PRD-101 — Evolución de rendimiento del ámbito: serie diaria de la temporada, su recta de tendencia
    /// y la referencia histórica de RN-015.
    ///
    /// La <b>serie</b> tiene un punto por día cosechado: el rendimiento ponderado por kilos de las
    /// cosechas de ese día (RN-013). Un día sin dato de aceite no es un punto, porque forzar un cero
    /// fingiría una caída que no ocurrió. La <b>tendencia</b> exige dos fechas distintas.
    ///
    /// El <b>histórico</b> son rectas de regresión de los mismos días de años anteriores sobre las mismas
    /// parcelas (ver <see cref="BuildHistory"/>) y aparece <b>aunque la campaña actual aún no tenga
    /// cosechas</b>: es la referencia con la que decidir cuándo empezar.
    /// </summary>
    public async Task<YieldEvolution> GetYieldEvolutionAsync(
        Guid userId,
        Guid workspaceId,
        DashboardRequest request,
        CancellationToken ct = default)
    {
        var scope = await scopeResolver.ResolveAsync(userId, workspaceId, request, ct);

        if (!scope.IsResolvable || scope.Plots.Count == 0)
            return new YieldEvolution(scope, [], null, YieldHistory.Empty);

        // Una sola lectura: los mismos terrenos en **todas** las temporadas. La serie mira la temporada
        // activa; el histórico, las fechas de años anteriores. Ambos sobre el mismo conjunto de filas.
        var plotIds = scope.Plots.Select(plot => plot.Id).ToArray();
        var allRows = await harvestRepository.ListAggregateRowsAsync(
            workspaceId, new HarvestAggregateFilter(null, plotIds), ct);

        var series = allRows
            .Where(row => row.SeasonId == scope.Season!.Id)
            .GroupBy(row => row.Date)
            .Select(group => new { Date = group.Key, Yield = WeightedYield(group), Kg = group.Sum(r => r.Kgs) })
            .Where(point => point.Yield is not null)
            .OrderBy(point => point.Date)
            .Select(point => new YieldPoint(
                point.Date, point.Yield!.Value, decimal.Round(point.Kg, 2, MidpointRounding.AwayFromZero)))
            .ToList();

        var trend = YieldRegression.Fit(series.Select(point => (point.Date, point.Yield)).ToList());

        return new YieldEvolution(scope, series, trend, BuildHistory(allRows, scope.Season!));
    }

    /// <summary>
    /// RN-015 — Referencia histórica: rectas de regresión de los <b>mismos días de años anteriores</b>.
    ///
    /// La ventana es el calendario de la temporada actual (o un año desde su inicio si no tiene fin), no
    /// el rango de sus cosechas: así el histórico cubre también las semanas aún sin cosechar, que es la
    /// evolución esperada con la que decidir si adelantar o retrasar la recolección. Cada día cosechado
    /// de una campaña anterior se traslada al año actual sumando los años que lo llevan a caer dentro de
    /// la ventana; un día que no cae en ninguna —de otra época del año— no es comparable y se descarta.
    /// Cada día de cada año es un punto (rendimiento ponderado por kilos si hubo varias cosechas) y
    /// todos pesan igual en la recta.
    ///
    /// «Histórico suficiente» (RN-015) se mide por hasta cuántos años atrás llega el dato: la recta
    /// general con un año previo con dato; la de 5 años solo si el histórico alcanza 5 años atrás, y la
    /// de 10 si alcanza 10. Cada una usa los años dentro de su tramo.
    /// </summary>
    private static YieldHistory BuildHistory(IReadOnlyList<HarvestAggregateRow> allRows, Season current)
    {
        var windowStart = current.StartDate;
        var windowEnd = current.EndDate ?? current.StartDate.AddYears(1).AddDays(-1);

        var pointsByYearsBack = new Dictionary<int, List<(DateOnly Date, decimal Value)>>();
        var comparable = allRows.Where(row =>
            row.HasOilData && row.SeasonId != current.Id && row.Date < windowStart);

        foreach (var day in comparable.GroupBy(row => row.Date))
        {
            if (YearsBack(day.Key, windowStart, windowEnd) is not { } years) continue;
            if (WeightedYield(day) is not { } value) continue;

            if (!pointsByYearsBack.TryGetValue(years, out var list)) pointsByYearsBack[years] = list = [];
            list.Add((day.Key.AddYears(years), value));
        }

        if (pointsByYearsBack.Count == 0) return YieldHistory.Empty;

        var oldest = pointsByYearsBack.Keys.Max();
        TrendLine? FitWithin(int maxYears) => YieldRegression.Fit(
            pointsByYearsBack.Where(entry => entry.Key <= maxYears).SelectMany(entry => entry.Value).ToList());

        return new YieldHistory(
            FitWithin(int.MaxValue),
            // «5 años» solo si el histórico alcanza al menos 5 años atrás: si no, sería un «5 años» que
            // en realidad son 2, y engaña.
            oldest >= 5 ? FitWithin(5) : null,
            oldest >= 10 ? FitWithin(10) : null,
            pointsByYearsBack.Count);
    }

    /// <summary>
    /// Años que hay que sumar a <paramref name="date"/> para que caiga dentro de la ventana
    /// [<paramref name="windowStart"/>, <paramref name="windowEnd"/>], o <c>null</c> si no cae en
    /// ninguno (una cosecha de otra época del año que no es comparable con la ventana actual). Mientras
    /// la ventana dure menos de un año, las de años consecutivos no se solapan y hay como mucho un año
    /// que la encaja.
    /// </summary>
    private static int? YearsBack(DateOnly date, DateOnly windowStart, DateOnly windowEnd)
    {
        for (var years = 1; years <= 60; years++)
        {
            var shifted = date.AddYears(years);
            if (shifted > windowEnd) return null;
            if (shifted >= windowStart) return years;
        }

        return null;
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
