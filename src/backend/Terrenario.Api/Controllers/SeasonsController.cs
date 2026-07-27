using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Seasons;
using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-201 — Temporada del Workspace activo. Es el primer recurso con ámbito de Workspace, así que
/// se apoya en <see cref="RequireWorkspaceScopeAttribute"/> (MVP-105): el Workspace activo se
/// resuelve en servidor y se lee de <see cref="IWorkspaceContext"/>, nunca del cliente.
///
/// Alcance de esta historia: consultar la temporada activa y crear la primera (oferta cancelable en
/// la UI). El maestro completo (listar varias, editar, cerrar, cambiar de activa) llega con MVP-203.
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/seasons")]
public sealed class SeasonsController(
    GetActiveSeasonHandler getActiveSeasonHandler,
    CreateSeasonHandler createSeasonHandler,
    IWorkspaceContext workspaceContext) : ControllerBase
{
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
    /// Crea la (primera) temporada activa del Workspace (MVP-201). La UI la ofrece de forma
    /// cancelable; si el Workspace ya tiene temporada activa, responde 409 (gestionar varias es
    /// alcance de MVP-203).
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
            return Conflict(new ApiErrorResponse(new ApiError(ex.ErrorCode, ex.Message)));
        }
    }

    private static object ToResponse(SeasonSummary season) => new
    {
        id = season.Id,
        name = season.Name,
        start_date = season.StartDate,
        end_date = season.EndDate,
        is_active = season.IsActive,
        is_closed = season.IsClosed
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
