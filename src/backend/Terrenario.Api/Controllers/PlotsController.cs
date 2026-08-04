using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Plots;
using Terrenario.Api.Application.Plots.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Plots;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-202 — Maestro de terrenos del Workspace activo. Como el resto de recursos con ámbito de
/// Workspace, se apoya en <see cref="RequireWorkspaceScopeAttribute"/> (MVP-105): el Workspace activo
/// se resuelve en servidor y se lee de <see cref="IWorkspaceContext"/>, nunca del cliente (RN-034).
///
/// Alcance: alta con datos mínimos (RN-028), edición e inactivación (CA-1/CA-2/CA-3) y listado con
/// búsqueda y filtro por estado. El borrado físico queda fuera: los terrenos con histórico se
/// inactivan, no se eliminan.
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/plots")]
public sealed class PlotsController(
    CreatePlotHandler createPlotHandler,
    UpdatePlotHandler updatePlotHandler,
    ListPlotsHandler listPlotsHandler,
    IWorkspaceContext workspaceContext) : ControllerBase
{
    /// <summary>Lista los terrenos del Workspace. Filtros opcionales: <c>search</c>, <c>is_active</c>.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery(Name = "is_active")] bool? isActive,
        CancellationToken ct)
    {
        var plots = await listPlotsHandler.HandleAsync(workspaceContext.WorkspaceId, search, isActive, ct);

        return Ok(new
        {
            data = plots.Select(ToResponse),
            meta = new { total = plots.Count }
        });
    }

    /// <summary>Alta de terreno con los datos mínimos obligatorios <c>name</c> y <c>ownership_type</c> (RN-028).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlotRequest request, CancellationToken ct)
    {
        try
        {
            var plot = await createPlotHandler.HandleAsync(
                new CreatePlotCommand(
                    workspaceContext.WorkspaceId,
                    request.Name,
                    request.OwnershipType,
                    request.Alias,
                    request.OwnerName,
                    request.CadastralReference,
                    request.Location,
                    request.TreeCount),
                ct);

            return CreatedAtAction(nameof(List), new { id = plot.Id }, ToResponse(plot));
        }
        catch (PlotValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (PlotConflictException ex)
        {
            return Conflict(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    /// <summary>
    /// Edición parcial de un terreno o cambio de su estado de actividad (inactivación CA-3 con
    /// <c>is_active: false</c>). Solo se modifican los campos presentes en el cuerpo (contrato de
    /// campos parciales): omitir un campo mantiene su valor; enviarlo vacío lo limpia.
    /// </summary>
    [HttpPatch("{plotId:guid}")]
    public async Task<IActionResult> Update(
        Guid plotId,
        [FromBody] Dictionary<string, JsonElement>? body,
        CancellationToken ct)
    {
        // Lector común del borde de transporte (MVP-502, P-027): un cuerpo con bytes que no son
        // UTF-8 válido acaba en 400, no en 500.
        var fields = PartialUpdateBody.From(body);

        if (!fields.TryReadInt("tree_count", out var treeCount))
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationRangeTreeCount, "El número de árboles debe ser un entero válido.")));

        if (!fields.TryReadBool("is_active", out var isActive))
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationRequired, "El campo 'is_active' debe ser booleano.")));

        try
        {
            var plot = await updatePlotHandler.HandleAsync(
                new UpdatePlotCommand(
                    workspaceContext.WorkspaceId,
                    plotId,
                    fields.ReadString("name"),
                    fields.ReadString("ownership_type"),
                    fields.ReadNullableString("alias"),
                    fields.ReadNullableString("owner_name"),
                    fields.ReadNullableString("cadastral_reference"),
                    fields.ReadNullableString("location"),
                    treeCount,
                    isActive),
                ct);

            if (plot is null)
                return NotFound(new ApiErrorResponse(ApiError.PlotNotFound()));

            return Ok(ToResponse(plot));
        }
        catch (PlotValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (PlotConflictException ex)
        {
            return Conflict(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    private static object ToResponse(PlotSummary plot) => new
    {
        id = plot.Id,
        workspace_id = plot.WorkspaceId,
        name = plot.Name,
        ownership_type = plot.OwnershipType,
        alias = plot.Alias,
        owner_name = plot.OwnerName,
        cadastral_reference = plot.CadastralReference,
        location = plot.Location,
        tree_count = plot.TreeCount,
        is_active = plot.IsActive,
        // Señal para que la UI marque el dato incompleto de nº de árboles sin bloquear (RN-010/RN-028).
        has_tree_count = plot.HasTreeCount
    };
}

/// <summary>Alta de terreno. Solo <c>name</c> y <c>ownership_type</c> son obligatorios (RN-028).</summary>
public sealed record CreatePlotRequest(
    [RequiredField(ErrorCodes.ValidationRequiredName, "El nombre del terreno es obligatorio.")]
    [MaxTextLength(Plot.NameMaxLength, ErrorCodes.ValidationPlotNameLength, "El nombre del terreno es demasiado largo.")]
    string Name,
    [property: JsonPropertyName("ownership_type")]
    [RequiredField(ErrorCodes.ValidationRequiredPlotOwnershipType, "El tipo de propiedad del terreno es obligatorio.")]
    string OwnershipType,
    string? Alias,
    [property: JsonPropertyName("owner_name")] string? OwnerName,
    [property: JsonPropertyName("cadastral_reference")] string? CadastralReference,
    string? Location,
    [property: JsonPropertyName("tree_count")] int? TreeCount);
