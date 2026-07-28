using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Seasons;
using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Controllers;

/// <summary>
/// Maestro de temporadas del Workspace activo (MVP-201 · completado en MVP-203). Como el resto de
/// recursos con ámbito de Workspace, se apoya en <see cref="RequireWorkspaceScopeAttribute"/>
/// (MVP-105): el Workspace activo se resuelve en servidor y se lee de <see cref="IWorkspaceContext"/>,
/// nunca del cliente (RN-034).
///
/// Alcance MVP-203: listar, crear (la nueva pasa a ser la activa), editar (nombre/fechas),
/// cerrar/reabrir (RN-024, informativo) y cambiar la temporada activa (RN-022, una sola activa). No
/// hay borrado físico: las temporadas con histórico se cierran, no se eliminan.
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
    IWorkspaceContext workspaceContext) : ControllerBase
{
    /// <summary>Lista las temporadas del Workspace (activa primero, luego histórico por fecha).</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var seasons = await listSeasonsHandler.HandleAsync(workspaceContext.WorkspaceId, ct);

        return Ok(new
        {
            data = seasons.Select(ToResponse),
            meta = new { total = seasons.Count }
        });
    }

    /// <summary>Temporada activa del Workspace en curso (RN-021/RN-022). 404 si aún no tiene.</summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var season = await getActiveSeasonHandler.HandleAsync(workspaceContext.WorkspaceId, ct);

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
    /// Cambia la temporada activa del Workspace (RN-022, una sola activa): activa la indicada y desbanca
    /// a la anterior. Si estaba cerrada, se reabre al activarla.
    /// </summary>
    [HttpPost("{seasonId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid seasonId, CancellationToken ct)
    {
        var season = await activateSeasonHandler.HandleAsync(workspaceContext.WorkspaceId, seasonId, ct);

        if (season is null)
            return NotFound(new ApiErrorResponse(ApiError.SeasonNotFoundById()));

        return Ok(ToResponse(season));
    }

    private static FieldUpdate<string> ReadString(Dictionary<string, JsonElement> body, string key)
        => body.TryGetValue(key, out var el)
            ? FieldUpdate<string>.Set(el.ValueKind == JsonValueKind.Null ? null : el.GetString())
            : FieldUpdate<string>.Absent;

    private static FieldUpdate<DateOnly> ReadDate(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<DateOnly>.Absent;
        if (TryReadDate(el, out var date)) return FieldUpdate<DateOnly>.Set(date);

        throw new SeasonValidationException(
            ErrorCodes.ValidationSeasonDateRange, $"El campo '{key}' debe ser una fecha válida (YYYY-MM-DD).");
    }

    private static FieldUpdate<DateOnly?> ReadNullableDate(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<DateOnly?>.Absent;
        if (el.ValueKind == JsonValueKind.Null) return FieldUpdate<DateOnly?>.Set(null);
        if (TryReadDate(el, out var date)) return FieldUpdate<DateOnly?>.Set(date);

        throw new SeasonValidationException(
            ErrorCodes.ValidationSeasonDateRange, $"El campo '{key}' debe ser una fecha válida (YYYY-MM-DD) o nulo.");
    }

    private static bool TryReadDate(JsonElement el, out DateOnly date)
    {
        date = default;
        if (el.ValueKind != JsonValueKind.String) return false;
        var raw = el.GetString();
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

    private static object ToResponse(SeasonSummary season) => new
    {
        id = season.Id,
        workspace_id = season.WorkspaceId,
        name = season.Name,
        start_date = season.StartDate,
        end_date = season.EndDate,
        is_active = season.IsActive,
        is_closed = season.IsClosed,
        // Estado derivado (planificada/activa/cerrada) para las etiquetas y acciones de la UI.
        status = season.Status.ToString().ToLowerInvariant()
    };
}

public sealed record CreateSeasonRequest(
    [Required(ErrorMessage = "El nombre de la temporada es obligatorio.")]
    [StringLength(Season.NameMaxLength, ErrorMessage = "El nombre de la temporada es demasiado largo.")]
    string Name,
    [property: JsonPropertyName("start_date")]
    [Required(ErrorMessage = "La fecha de inicio de la temporada es obligatoria.")]
    DateOnly StartDate,
    [property: JsonPropertyName("end_date")]
    DateOnly? EndDate);
