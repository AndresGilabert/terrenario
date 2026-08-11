using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Masters;
using Terrenario.Api.Application.Seasons;
using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Masters;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Controllers;

/// <summary>
/// Maestro de temporadas del Workspace activo (MVP-201 · completado en MVP-203). Como el resto de
/// recursos con ámbito de Workspace, se apoya en <see cref="RequireWorkspaceScopeAttribute"/>
/// (MVP-105): el Workspace activo se resuelve en servidor y se lee de <see cref="IWorkspaceContext"/>,
/// nunca del cliente (RN-034).
///
/// Alcance MVP-203: listar, crear, editar (nombre/fechas) y cerrar/reabrir (RN-024, informativo). Desde
/// MVP-209, el estado (planificada/abierta/cerrada) es derivado e independiente de la temporada de
/// <b>trabajo</b>, que es por usuario y se fija con <c>POST /seasons/{id}/activate</c>. Una temporada
/// con histórico se <b>cierra</b>, nunca se elimina; desde MVP-806 sí se puede eliminar la que nunca
/// se usó y fusionar dos que son la misma campaña (RN-037).
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/seasons")]
public sealed class SeasonsController(
    GetActiveSeasonHandler getActiveSeasonHandler,
    ListSeasonsHandler listSeasonsHandler,
    CreateSeasonHandler createSeasonHandler,
    UpdateSeasonHandler updateSeasonHandler,
    ActivateSeasonHandler activateSeasonHandler,
    MasterUsageService masterUsageService,
    DeleteMasterHandler deleteMasterHandler,
    MergeMastersHandler mergeMastersHandler,
    IWorkspaceContext workspaceContext) : ControllerBase
{
    /// <summary>Lista las temporadas del Workspace (activa primero, luego histórico por fecha).</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var seasons = await listSeasonsHandler.HandleAsync(
            User.GetUserId()!.Value, workspaceContext.WorkspaceId, ct);
        var usage = await masterUsageService.CountByWorkspaceAsync(
            MasterKind.Season, workspaceContext.WorkspaceId, ct);

        return Ok(new
        {
            data = seasons.Select(season => ToResponse(season, usage.GetValueOrDefault(season.Id))),
            meta = new { total = seasons.Count }
        });
    }

    /// <summary>
    /// Temporada de <b>trabajo del usuario</b> en el Workspace (RN-021, MVP-209). 404 si el Workspace no
    /// tiene ninguna temporada.
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var season = await getActiveSeasonHandler.HandleAsync(
            User.GetUserId()!.Value, workspaceContext.WorkspaceId, ct);

        if (season is null)
            return NotFound(new ApiErrorResponse(ApiError.SeasonNotFound()));

        return Ok(ToResponse(season));
    }

    /// <summary>
    /// Crea una temporada del Workspace. La nueva pasa a ser la activa, desbancando a la anterior si la
    /// hubiera (MVP-203); la primera de un Workspace simplemente nace activa (onboarding MVP-201).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSeasonRequest request, CancellationToken ct)
    {
        try
        {
            var season = await createSeasonHandler.HandleAsync(
                User.GetUserId()!.Value,
                new CreateSeasonCommand(
                    workspaceContext.WorkspaceId,
                    request.Name,
                    request.StartDate,
                    request.EndDate),
                ct);

            return CreatedAtAction(nameof(GetActive), ToResponse(season));
        }
        catch (SeasonValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (SeasonConflictException ex)
        {
            return Conflict(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    /// <summary>
    /// Edición parcial de una temporada: nombre, fechas y cierre/reapertura (<c>is_closed</c>). Solo se
    /// modifican los campos presentes en el cuerpo; omitir un campo mantiene su valor. El cambio de
    /// temporada activa no va aquí: usa <c>POST /seasons/{id}/activate</c>.
    /// </summary>
    [HttpPatch("{seasonId:guid}")]
    public async Task<IActionResult> Update(
        Guid seasonId,
        [FromBody] Dictionary<string, JsonElement>? body,
        CancellationToken ct)
    {
        body ??= new Dictionary<string, JsonElement>();

        FieldUpdate<DateOnly> startDate;
        FieldUpdate<DateOnly?> endDate;
        FieldUpdate<bool> isClosed;
        try
        {
            startDate = ReadDate(body, "start_date");
            endDate = ReadNullableDate(body, "end_date");
            isClosed = ReadBool(body, "is_closed");
        }
        catch (SeasonValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }

        try
        {
            var season = await updateSeasonHandler.HandleAsync(
                User.GetUserId()!.Value,
                new UpdateSeasonCommand(
                    workspaceContext.WorkspaceId,
                    seasonId,
                    ReadString(body, "name"),
                    startDate,
                    endDate,
                    isClosed),
                ct);

            if (season is null)
                return NotFound(new ApiErrorResponse(ApiError.SeasonNotFoundById()));

            return Ok(ToResponse(season));
        }
        catch (SeasonValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (SeasonConflictException ex)
        {
            return Conflict(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    /// <summary>
    /// Fija esta temporada como la de <b>trabajo del usuario</b> (MVP-209): sobre ella registrará por
    /// defecto. No afecta a otros miembros ni reabre una temporada cerrada.
    /// </summary>
    [HttpPost("{seasonId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid seasonId, CancellationToken ct)
    {
        var season = await activateSeasonHandler.HandleAsync(
            User.GetUserId()!.Value, workspaceContext.WorkspaceId, seasonId, ct);

        if (season is null)
            return NotFound(new ApiErrorResponse(ApiError.SeasonNotFoundById()));

        return Ok(ToResponse(season));
    }

    /// <summary>
    /// MVP-806 (CA-1) — Borrado <b>físico</b> de una temporada que nunca se usó. Con histórico
    /// responde <c>422 BUSINESS_RULE_MASTER_IN_USE</c> diciendo cuántos registros la referencian
    /// (CA-2); la vía para ese caso sigue siendo el cierre.
    ///
    /// Tenerla fijada como temporada de trabajo <b>no</b> lo impide: es una preferencia por usuario
    /// (MVP-209) con <c>ON DELETE SET NULL</c>, y al desaparecer se resuelve el defecto de
    /// <c>WorkingSeasonPolicy</c>, que es exactamente lo que hace un Workspace recién creado.
    /// </summary>
    [HttpDelete("{seasonId:guid}")]
    public async Task<IActionResult> Delete(Guid seasonId, CancellationToken ct)
    {
        var deleted = await deleteMasterHandler.HandleAsync(
            MasterKind.Season, workspaceContext.WorkspaceId, seasonId, ct);

        return deleted is null
            ? NotFound(new ApiErrorResponse(ApiError.SeasonNotFoundById()))
            : NoContent();
    }

    /// <summary>
    /// MVP-806 (CA-3) — Fusiona dos temporadas: la de la ruta sobrevive y la del cuerpo cede sus
    /// registros y desaparece. Quien tuviera fijada la absorbida como temporada de trabajo pasa a
    /// tener la superviviente, en vez de volver al defecto sin haberlo pedido.
    /// </summary>
    [HttpPost("{seasonId:guid}/merge")]
    public async Task<IActionResult> Merge(
        Guid seasonId, [FromBody] MergeMasterRequest request, CancellationToken ct)
    {
        var result = await mergeMastersHandler.HandleAsync(
            MasterKind.Season,
            workspaceContext.WorkspaceId,
            User.GetUserId()!.Value,
            seasonId,
            request.AbsorbedId,
            ct);

        return result is null
            ? NotFound(new ApiErrorResponse(ApiError.SeasonNotFoundById()))
            : Ok(MasterMergeResponse.From(result));
    }

    private static FieldUpdate<string> ReadString(Dictionary<string, JsonElement> body, string key)
        => body.TryGetValue(key, out var el)
            ? FieldUpdate<string>.Set(JsonText.Read(el, key))
            : FieldUpdate<string>.Absent;

    private static FieldUpdate<DateOnly> ReadDate(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<DateOnly>.Absent;
        if (TryReadDate(el, key, out var date)) return FieldUpdate<DateOnly>.Set(date);

        throw new SeasonValidationException(
            ErrorCodes.ValidationSeasonDateRange, $"El campo '{key}' debe ser una fecha válida (YYYY-MM-DD).");
    }

    private static FieldUpdate<DateOnly?> ReadNullableDate(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<DateOnly?>.Absent;
        if (el.ValueKind == JsonValueKind.Null) return FieldUpdate<DateOnly?>.Set(null);
        if (TryReadDate(el, key, out var date)) return FieldUpdate<DateOnly?>.Set(date);

        throw new SeasonValidationException(
            ErrorCodes.ValidationSeasonDateRange, $"El campo '{key}' debe ser una fecha válida (YYYY-MM-DD) o nulo.");
    }

    private static bool TryReadDate(JsonElement el, string key, out DateOnly date)
    {
        date = default;
        if (el.ValueKind != JsonValueKind.String) return false;
        var raw = JsonText.Read(el, key);
        return raw is not null
            && DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static FieldUpdate<bool> ReadBool(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<bool>.Absent;
        if (el.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return FieldUpdate<bool>.Set(el.GetBoolean());

        throw new SeasonValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser booleano.");
    }

    private static object ToResponse(SeasonSummary season, int? usageCount = null) => new
    {
        id = season.Id,
        workspace_id = season.WorkspaceId,
        name = season.Name,
        start_date = season.StartDate,
        end_date = season.EndDate,
        is_closed = season.IsClosed,
        // MVP-209 — la temporada de trabajo del usuario que consulta (antes `is_active`, per-Workspace).
        is_working = season.IsWorking,
        // Estado derivado (planificada/abierta/cerrada) para las etiquetas de la UI, independiente de
        // `is_working`.
        status = season.Status.ToString().ToLowerInvariant(),
        // MVP-806 (CA-2) — Ver la nota de `PlotsController`: `null` significa «no consultado».
        usage_count = usageCount
    };
}

public sealed record CreateSeasonRequest(
    [RequiredField(ErrorCodes.ValidationRequiredSeasonName, "El nombre de la temporada es obligatorio.")]
    [MaxTextLength(Season.NameMaxLength, ErrorCodes.ValidationSeasonNameLength, "El nombre de la temporada es demasiado largo.")]
    string Name,
    [property: JsonPropertyName("start_date")]
    [RequiredField(ErrorCodes.ValidationRequired, "La fecha de inicio de la temporada es obligatoria.")]
    DateOnly StartDate,
    [property: JsonPropertyName("end_date")]
    DateOnly? EndDate);
