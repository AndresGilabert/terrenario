using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Terrenario.Api.Application.Dashboard;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Workspaces;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-403 — Dashboard del Workspace activo (<c>contratos-api.md §8</c>). Solo lectura: agrega la
/// producción ya capturada, no la crea.
///
/// Los filtros son opcionales y el servidor pone los defectos de RN-008 —temporada activa y todos los
/// terrenos activos—; el ámbito resuelto **viaja en la respuesta** para que la pantalla pueda explicar
/// de qué son las cifras que muestra y para que <c>MVP-405</c> pinte los filtros ya posicionados.
///
/// En MVP no hay refresco continuo (RN-006): los datos se recalculan al entrar o al recargar.
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/dashboard")]
public sealed class DashboardController(
    DashboardQueryService dashboardQueryService,
    DashboardEconomicsService dashboardEconomicsService,
    IWorkspaceContext workspaceContext) : ControllerBase
{
    /// <summary>
    /// MVP-707 — Lectura económica de la campaña (RN-009 ampliada): cuánto ha salido y cuánto ha
    /// entrado, sobre el mismo ámbito que el resto de widgets.
    ///
    /// <c>income</c> a <c>null</c> significa que <b>ninguna</b> partida del ámbito tiene precio, y la
    /// pantalla debe decir «sin dato», no «0 €»: una campaña sin precios registrados no ha ingresado
    /// cero (CA-5). <c>harvests_with_price</c> explica sobre cuántas partidas se suma.
    /// </summary>
    [HttpGet("economics")]
    public async Task<IActionResult> Economics(
        [FromQuery(Name = "season_id")] Guid? seasonId,
        [FromQuery(Name = "plot_ids")] Guid[]? plotIds,
        CancellationToken ct)
    {
        var economics = await dashboardEconomicsService.HandleAsync(
            User.GetUserId()!.Value, workspaceContext.WorkspaceId, new DashboardRequest(seasonId, plotIds), ct);

        return Ok(new
        {
            scope = ToScope(economics.Scope),
            expense = economics.Expense,
            income = economics.Income,
            harvests = economics.Harvests,
            harvests_with_price = economics.HarvestsWithPrice
        });
    }

    /// <summary>Resumen de temporada: kilos, litros y rendimiento medio (CA-1).</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(
        [FromQuery(Name = "season_id")] Guid? seasonId,
        [FromQuery(Name = "plot_ids")] Guid[]? plotIds,
        CancellationToken ct)
    {
        var summary = await dashboardQueryService.GetSummaryAsync(
            User.GetUserId()!.Value, workspaceContext.WorkspaceId, new DashboardRequest(seasonId, plotIds), ct);

        return Ok(new
        {
            scope = ToScope(summary.Scope),
            total_kg = summary.TotalKg,
            // RN-014 — litros declarados o derivados del rendimiento. `null` = se desconoce, no cero.
            total_liters = summary.TotalLiters,
            // RN-013 — unidad canónica L/100kg, ponderado por kilos.
            average_yield = summary.AverageYield,
            harvests = summary.Harvests,
            // Sobre cuántas partidas se ha promediado: sin esto, una media sobre 2 de 20 partidas
            // parecería la de la campaña entera.
            harvests_with_oil_data = summary.HarvestsWithOilData,
            // MVP-405 (CA-3, RN-010) — kg/árbol del ámbito. `null` = ningún terreno con cosechas tiene
            // número de árboles. Los contadores explican sobre qué se calculó y cuántos quedaron fuera.
            kg_per_tree = summary.KgPerTree,
            trees_counted = summary.TreesCounted,
            plots_counted = summary.PlotsCounted,
            plots_without_tree_count = summary.PlotsWithoutTreeCount
        });
    }

    /// <summary>Kilos por destino, con la taxonomía cerrada de RN-012 incluido `desconocido` (CA-2).</summary>
    [HttpGet("kg-by-destination")]
    public async Task<IActionResult> KgByDestination(
        [FromQuery(Name = "season_id")] Guid? seasonId,
        [FromQuery(Name = "plot_ids")] Guid[]? plotIds,
        CancellationToken ct)
    {
        var (scope, totals, totalKg) = await dashboardQueryService.GetKgByDestinationAsync(
            User.GetUserId()!.Value, workspaceContext.WorkspaceId, new DashboardRequest(seasonId, plotIds), ct);

        return Ok(new
        {
            scope = ToScope(scope),
            data = totals.Select(total => new { destination = total.Destination, kg = total.Kg }),
            // El total va en servidor para que el cliente no tenga que sumar antes de calcular
            // porcentajes, y para que resumen y gráfico no puedan discrepar por un redondeo.
            meta = new { total_kg = totalKg }
        });
    }

    /// <summary>Kilos por terreno, con el orden fijo de RN-011 —kg descendentes, desempate alfabético— (CA-1).</summary>
    [HttpGet("kg-by-plot")]
    public async Task<IActionResult> KgByPlot(
        [FromQuery(Name = "season_id")] Guid? seasonId,
        [FromQuery(Name = "plot_ids")] Guid[]? plotIds,
        CancellationToken ct)
    {
        var (scope, totals, totalKg) = await dashboardQueryService.GetKgByPlotAsync(
            User.GetUserId()!.Value, workspaceContext.WorkspaceId, new DashboardRequest(seasonId, plotIds), ct);

        return Ok(new
        {
            scope = ToScope(scope),
            data = totals.Select(total => new
            {
                plot_id = total.PlotId,
                plot_name = total.PlotName,
                kg = total.Kg
            }),
            meta = new { total_kg = totalKg }
        });
    }

    /// <summary>
    /// Evolución de rendimiento en L/100kg (RN-013) por mes o semana, con la comparativa histórica
    /// básica de RN-015 —presente solo cuando hay histórico suficiente— (CA-2).
    /// </summary>
    [HttpGet("yield-evolution")]
    public async Task<IActionResult> YieldEvolution(
        [FromQuery(Name = "season_id")] Guid? seasonId,
        [FromQuery(Name = "plot_ids")] Guid[]? plotIds,
        [FromQuery] string? granularity,
        CancellationToken ct)
    {
        // Solo `week` cambia el defecto: cualquier otro valor —o su ausencia— es el mes del prototipo.
        var resolved = string.Equals(granularity, "week", StringComparison.OrdinalIgnoreCase)
            ? YieldGranularity.Week
            : YieldGranularity.Month;

        var evolution = await dashboardQueryService.GetYieldEvolutionAsync(
            User.GetUserId()!.Value, workspaceContext.WorkspaceId, new DashboardRequest(seasonId, plotIds), resolved, ct);

        return Ok(new
        {
            scope = ToScope(evolution.Scope),
            granularity = evolution.Granularity == YieldGranularity.Week ? "week" : "month",
            data = evolution.Series.Select(point => new
            {
                period = point.Period,
                yield_l_per_100kg = point.Yield,
                kg = point.Kg
            }),
            // RN-015 — la comparativa histórica de la **ventana estacional**: `null` mientras no haya
            // histórico suficiente, para que la UI no dibuje una referencia inventada. `window` es el
            // tramo de calendario (MM-DD) sobre el que se compara, para que la pantalla lo explique.
            history = new
            {
                average = evolution.History.Average,
                average_5_years = evolution.History.Average5Years,
                average_10_years = evolution.History.Average10Years,
                prior_years_with_data = evolution.History.PriorYearsWithData,
                window = evolution.History.Window is null
                    ? null
                    : new { from = evolution.History.Window.From, to = evolution.History.Window.To }
            }
        });
    }

    /// <summary>
    /// P-021 — Producción agregada por temporada. La consume el maestro de temporadas (MVP-203), que
    /// omitió el dato porque <c>HARVEST</c> no existía todavía.
    /// </summary>
    [HttpGet("kg-by-season")]
    public async Task<IActionResult> KgBySeason(CancellationToken ct)
    {
        var seasons = await dashboardQueryService.GetKgBySeasonAsync(workspaceContext.WorkspaceId, ct);

        return Ok(new
        {
            data = seasons.Select(season => new
            {
                season_id = season.SeasonId,
                season_name = season.SeasonName,
                total_kg = season.TotalKg,
                harvests = season.Harvests
            }),
            meta = new { total = seasons.Count }
        });
    }

    /// <summary>
    /// Ámbito resuelto (RN-008). <c>season</c> a <c>null</c> significa que el Workspace no tiene
    /// temporada que mirar: la pantalla debe pedirla, no mostrar ceros.
    /// </summary>
    private static object ToScope(DashboardScope scope) => new
    {
        season = scope.Season is null
            ? null
            : new
            {
                id = scope.Season.Id,
                name = scope.Season.Name,
                // MVP-209 — estado derivado (planificada/abierta/cerrada) en vez del antiguo `is_active`.
                status = scope.Season.StatusOn(DateOnly.FromDateTime(DateTime.UtcNow))
                    .ToString().ToLowerInvariant(),
                start_date = scope.Season.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                end_date = scope.Season.EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            },
        plot_ids = scope.Plots.Select(plot => plot.Id),
        plots = scope.Plots.Count
    };
}
