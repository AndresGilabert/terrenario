using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Terrenario.Api.Application.Diary;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Workspaces;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-305 — Diario cronológico unificado del Workspace activo (RN-033). Es la **vista principal del
/// MVP**: mezcla actividades, cosechas, compras y consumos en una sola secuencia por fecha de negocio,
/// para que revisar la operativa no obligue a pasear por cuatro listados.
///
/// De solo lectura a propósito. Cada registro se crea, corrige y elimina por el recurso al que
/// pertenece (<c>/activities</c>, <c>/harvests</c>, <c>/purchases</c>, <c>/consumptions</c>), que es
/// donde viven sus reglas; el diario solo agrega. Por eso cada entrada trae su <c>version</c>: es lo
/// que permite eliminar desde aquí con <c>If-Match</c> sin abrir antes el registro (ADR-0005).
///
/// La <b>cosecha</b> la enciende <c>MVP-401</c>, que es quien crea <c>HARVEST</c> (hallazgo
/// <c>G-4</c>). Con los cuatro tipos vivos, RN-033 queda cumplida entera.
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/diary")]
public sealed class DiaryController(
    DiaryQueryService diaryQueryService,
    IWorkspaceContext workspaceContext) : ControllerBase
{
    /// <summary>
    /// Muro cronológico del Workspace. Filtros opcionales: <c>from</c>, <c>to</c>, <c>plot_id</c>,
    /// <c>season_id</c> y <c>type</c> (repetible).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery(Name = "plot_id")] Guid? plotId,
        [FromQuery(Name = "season_id")] Guid? seasonId,
        [FromQuery(Name = "type")] string[]? types,
        CancellationToken ct)
    {
        if (!TryParseDate(from, out var fromDate) || !TryParseDate(to, out var toDate))
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationRequired, "Las fechas de filtro deben tener el formato YYYY-MM-DD.")));

        var requestedTypes = types?.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray() ?? [];
        if (requestedTypes.Any(type => !DiaryEntryTypes.IsSupported(type)))
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationRequired,
                $"Los tipos admitidos son: {string.Join(", ", DiaryEntryTypes.Supported)}.")));

        var result = await diaryQueryService.HandleAsync(
            workspaceContext.WorkspaceId,
            new DiaryFilter(fromDate, toDate, plotId, seasonId, requestedTypes),
            ct);

        return Ok(new
        {
            data = result.Entries.Select(ToResponse),
            meta = new
            {
                total = result.Entries.Count,
                // Gasto real de lo que se está viendo. **No** incluye las imputaciones: reparten
                // dinero que la compra ya aportó (MVP-399, `R-01`).
                total_cost = result.TotalCost,
                // Lo repartido por terrenos, aparte: desglose de `total_cost`, no gasto añadido.
                imputed_cost = result.ImputedCost,
                activities = result.TotalActivities,
                purchases = result.TotalPurchases,
                consumptions = result.TotalConsumptions,
                // MVP-401 — la cosecha no aporta gasto (RN-029), así que se resume por kilos: es la
                // magnitud que la hace legible en la cabecera del diario.
                harvests = result.TotalHarvests,
                total_kg = result.TotalKg,
                // RN-032 — cuántos consumos no tienen compra detrás: su coste consta como 0 porque se
                // desconoce, y el diario lo dice en vez de dejar creer que fue gratis.
                consumptions_without_purchase = result.ConsumptionsWithoutPurchase
            }
        });
    }

    private static bool TryParseDate(string? raw, out DateOnly? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) return true;

        if (!DateOnly.TryParseExact(raw.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
            return false;

        value = parsed;
        return true;
    }

    private static object ToResponse(DiaryEntry entry) => new
    {
        type = entry.Type,
        id = entry.Id,
        date = entry.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        title = entry.Title,
        description = entry.Description,
        plot_id = entry.PlotId,
        plot_name = entry.PlotName,
        season_id = entry.SeasonId,
        season_name = entry.SeasonName,
        cost = entry.Cost,
        // Necesaria para poder eliminar desde el diario con If-Match (ADR-0005).
        version = entry.Version,
        is_out_of_season_range = entry.IsOutOfSeasonRange,
        created_at = entry.CreatedAt,
        worker_name = entry.WorkerName,
        hours = entry.Hours,
        // MVP-302 — `null` en una actividad significa tarea escrita a mano: es lo que permite ofrecer
        // guardarla en el catálogo solo cuando tiene sentido.
        task_id = entry.TaskId,
        quantity = entry.Quantity,
        has_purchase = entry.HasPurchase,
        // MVP-401 — solo en cosechas: kilos recolectados y destino (RN-012).
        kgs = entry.Kgs,
        destination = entry.Destination,
        // MVP-402 — rendimiento efectivo en L/100kg (RN-013/RN-014).
        yield = entry.Yield
    };
}
