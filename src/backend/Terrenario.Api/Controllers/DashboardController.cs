using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Terrenario.Api.Application.Dashboard;
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
    IWorkspaceContext workspaceContext) : ControllerBase
{
    /// <summary>Resumen de temporada: kilos, litros y rendimiento medio (CA-1).</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(
        [FromQuery(Name = "season_id")] Guid? seasonId,
        [FromQuery(Name = "plot_ids")] Guid[]? plotIds,
        CancellationToken ct)
    {
        var summary = await dashboardQueryService.GetSummaryAsync(
            workspaceContext.WorkspaceId, new DashboardRequest(seasonId, plotIds), ct);

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
            harvests_with_oil_data = summary.HarvestsWithOilData
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
            workspaceContext.WorkspaceId, new DashboardRequest(seasonId, plotIds), ct);

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
            workspaceContext.WorkspaceId, new DashboardRequest(seasonId, plotIds), ct);

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
            workspaceContext.WorkspaceId, new DashboardRequest(seasonId, plotIds), resolved, ct);

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
            // RN-015 — la comparativa histórica: `null` mientras no haya histórico suficiente, para que
            // la UI no dibuje una línea de referencia inventada.
            history = new
            {
                average = evolution.History.Average,
                average_5_seasons = evolution.History.Average5Seasons,
                average_10_seasons = evolution.History.Average10Seasons,
                prior_seasons_with_data = evolution.History.PriorSeasonsWithData
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
                is_active = scope.Season.IsActive,
                start_date = scope.Season.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                end_date = scope.Season.EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            },
        plot_ids = scope.Plots.Select(plot => plot.Id),
        plots = scope.Plots.Count
    };
}
