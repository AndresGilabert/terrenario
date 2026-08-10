using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Masters;
using Terrenario.Api.Application.Plots;
using Terrenario.Api.Application.Plots.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Masters;
using Terrenario.Api.Domain.Plots;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-202 — Maestro de terrenos del Workspace activo. Como el resto de recursos con ámbito de
/// Workspace, se apoya en <see cref="RequireWorkspaceScopeAttribute"/> (MVP-105): el Workspace activo
/// se resuelve en servidor y se lee de <see cref="IWorkspaceContext"/>, nunca del cliente (RN-034).
///
/// Alcance: alta con datos mínimos (RN-028), edición e inactivación (CA-1/CA-2/CA-3) y listado con
/// búsqueda y filtro por estado. Un terreno con histórico se <b>inactiva</b>, nunca se elimina; desde
/// MVP-806 sí se puede eliminar el que nunca se usó y fusionar dos que son el mismo (RN-037).
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/plots")]
public sealed class PlotsController(
    CreatePlotHandler createPlotHandler,
    UpdatePlotHandler updatePlotHandler,
    ListPlotsHandler listPlotsHandler,
    MasterUsageService masterUsageService,
    DeleteMasterHandler deleteMasterHandler,
    MergeMastersHandler mergeMastersHandler,
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
        var usage = await masterUsageService.CountByWorkspaceAsync(
            MasterKind.Plot, workspaceContext.WorkspaceId, ct);

        return Ok(new
        {
            data = plots.Select(plot => ToResponse(plot, usage.GetValueOrDefault(plot.Id))),
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

    /// <summary>
    /// MVP-806 (CA-1) — Borrado <b>físico</b> de un terreno que nunca se usó. Con histórico responde
    /// <c>422 BUSINESS_RULE_MASTER_IN_USE</c> diciendo cuántos registros lo referencian (CA-2); la vía
    /// para ese caso sigue siendo la inactivación.
    /// </summary>
    [HttpDelete("{plotId:guid}")]
    public async Task<IActionResult> Delete(Guid plotId, CancellationToken ct)
    {
        var deleted = await deleteMasterHandler.HandleAsync(
            MasterKind.Plot, workspaceContext.WorkspaceId, plotId, ct);

        return deleted is null
            ? NotFound(new ApiErrorResponse(ApiError.PlotNotFound()))
            : NoContent();
    }

    /// <summary>
    /// MVP-806 (CA-3) — Fusiona dos terrenos: el de la ruta sobrevive y el del cuerpo cede sus
    /// registros y desaparece.
    /// </summary>
    [HttpPost("{plotId:guid}/merge")]
    public async Task<IActionResult> Merge(
        Guid plotId, [FromBody] MergeMasterRequest request, CancellationToken ct)
    {
        var result = await mergeMastersHandler.HandleAsync(
            MasterKind.Plot,
            workspaceContext.WorkspaceId,
            User.GetUserId()!.Value,
            plotId,
            request.AbsorbedId,
            ct);

        return result is null
            ? NotFound(new ApiErrorResponse(ApiError.PlotNotFound()))
            : Ok(MasterMergeResponse.From(result));
    }

    private static object ToResponse(PlotSummary plot, int? usageCount = null) => new
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
        has_tree_count = plot.HasTreeCount,
        // MVP-806 (CA-2) — Cuántos registros lo referencian, para que la interfaz sepa a quién puede
        // ofrecer «Eliminar». Solo lo trae el **listado**: en el alta y la edición viaja `null`, que
        // significa «no consultado» y no «ninguno». Decir cero ahí sería mentir en el `PATCH` de un
        // terreno con histórico, y una interfaz que se lo creyera ofrecería un borrado imposible.
        usage_count = usageCount
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
