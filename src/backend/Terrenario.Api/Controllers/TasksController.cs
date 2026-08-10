using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Masters;
using Terrenario.Api.Application.Tasks;
using Terrenario.Api.Application.Tasks.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Masters;
using Terrenario.Api.Domain.Tasks;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-205 — Catálogo de tareas del Workspace activo (RN-026). Como el resto de recursos con ámbito
/// de Workspace, se apoya en <see cref="RequireWorkspaceScopeAttribute"/> (MVP-105): el Workspace
/// activo se resuelve en servidor y se lee de <see cref="IWorkspaceContext"/>, nunca del cliente
/// (RN-034). Ese aislamiento es lo que garantiza que el catálogo de un Workspace no afecte al de
/// otro (CA-1).
///
/// Alcance: alta, edición, listado e inactivación (HU-1/HU-2, CA-2/CA-3). Una tarea con histórico se
/// <b>inactiva</b> para no invalidar los registros que la referencian; desde MVP-806 sí se puede
/// eliminar la que nunca se usó y fusionar dos que son la misma labor (RN-037).
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/tasks")]
public sealed class TasksController(
    CreateTaskHandler createTaskHandler,
    UpdateTaskHandler updateTaskHandler,
    ListTasksHandler listTasksHandler,
    MasterUsageService masterUsageService,
    DeleteMasterHandler deleteMasterHandler,
    MergeMastersHandler mergeMastersHandler,
    IWorkspaceContext workspaceContext) : ControllerBase
{
    /// <summary>Lista el catálogo del Workspace. Filtro opcional: <c>is_active</c>.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery(Name = "is_active")] bool? isActive,
        CancellationToken ct)
    {
        var tasks = await listTasksHandler.HandleAsync(workspaceContext.WorkspaceId, isActive, ct);
        var usage = await masterUsageService.CountByWorkspaceAsync(
            MasterKind.Task, workspaceContext.WorkspaceId, ct);

        return Ok(new
        {
            data = tasks.Select(task => ToResponse(task, usage.GetValueOrDefault(task.Id))),
            meta = new { total = tasks.Count }
        });
    }

    /// <summary>Alta de tarea en el catálogo. Solo <c>name</c> es obligatorio (CA-2).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        try
        {
            var task = await createTaskHandler.HandleAsync(
                new CreateTaskCommand(
                    workspaceContext.WorkspaceId,
                    request.Name,
                    request.IsActive),
                ct);

            return CreatedAtAction(nameof(List), new { id = task.Id }, ToResponse(task));
        }
        catch (TaskValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (TaskConflictException ex)
        {
            return Conflict(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    /// <summary>
    /// Edición parcial de una tarea o cambio de su estado de actividad (inactivación CA-3 con
    /// <c>is_active: false</c>). Solo se modifican los campos presentes en el cuerpo: omitir un campo
    /// mantiene su valor.
    /// </summary>
    [HttpPatch("{taskId:guid}")]
    public async Task<IActionResult> Update(
        Guid taskId,
        [FromBody] Dictionary<string, JsonElement>? body,
        CancellationToken ct)
    {
        // Lector común del borde de transporte (MVP-502, P-027).
        var fields = PartialUpdateBody.From(body);

        if (!fields.TryReadBool("is_active", out var isActive))
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationRequired, "El campo 'is_active' debe ser booleano.")));

        try
        {
            var task = await updateTaskHandler.HandleAsync(
                new UpdateTaskCommand(
                    workspaceContext.WorkspaceId,
                    taskId,
                    fields.ReadString("name"),
                    isActive),
                ct);

            if (task is null)
                return NotFound(new ApiErrorResponse(ApiError.TaskNotFound()));

            return Ok(ToResponse(task));
        }
        catch (TaskValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (TaskConflictException ex)
        {
            return Conflict(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    /// <summary>
    /// MVP-806 (CA-1) — Borrado <b>físico</b> de una tarea que nunca se usó. Con histórico responde
    /// <c>422 BUSINESS_RULE_MASTER_IN_USE</c> diciendo cuántas actividades la referencian (CA-2).
    /// Solo cuentan las actividades que la eligieron del catálogo: la tarea escrita en texto libre
    /// (RN-025) no referencia a ninguna fila.
    /// </summary>
    [HttpDelete("{taskId:guid}")]
    public async Task<IActionResult> Delete(Guid taskId, CancellationToken ct)
    {
        var deleted = await deleteMasterHandler.HandleAsync(
            MasterKind.Task, workspaceContext.WorkspaceId, taskId, ct);

        return deleted is null
            ? NotFound(new ApiErrorResponse(ApiError.TaskNotFound()))
            : NoContent();
    }

    /// <summary>
    /// MVP-806 (CA-3) — Fusiona dos tareas: la de la ruta sobrevive y la del cuerpo cede sus
    /// actividades y desaparece.
    /// </summary>
    [HttpPost("{taskId:guid}/merge")]
    public async Task<IActionResult> Merge(
        Guid taskId, [FromBody] MergeMasterRequest request, CancellationToken ct)
    {
        var result = await mergeMastersHandler.HandleAsync(
            MasterKind.Task,
            workspaceContext.WorkspaceId,
            User.GetUserId()!.Value,
            taskId,
            request.AbsorbedId,
            ct);

        return result is null
            ? NotFound(new ApiErrorResponse(ApiError.TaskNotFound()))
            : Ok(MasterMergeResponse.From(result));
    }

    private static object ToResponse(TaskSummary task, int? usageCount = null) => new
    {
        id = task.Id,
        workspace_id = task.WorkspaceId,
        name = task.Name,
        is_active = task.IsActive,
        // MVP-806 (CA-2) — Ver la nota de `PlotsController`: `null` significa «no consultado».
        usage_count = usageCount
    };
}

/// <summary>
/// Alta de tarea. Solo <c>name</c> es obligatorio (CA-2); <c>is_active</c> permite dar de alta una
/// tarea ya inactiva (por defecto nace activa).
/// </summary>
public sealed record CreateTaskRequest(
    [RequiredField(ErrorCodes.ValidationRequiredTaskName, "El nombre de la tarea es obligatorio.")]
    [MaxTextLength(TaskItem.NameMaxLength, ErrorCodes.ValidationTaskNameLength, "El nombre de la tarea es demasiado largo.")]
    string Name,
    [property: JsonPropertyName("is_active")] bool? IsActive);
