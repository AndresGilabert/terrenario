using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Tasks;
using Terrenario.Api.Application.Tasks.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Tasks;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-205 — Catálogo de tareas del Workspace activo (RN-026). Como el resto de recursos con ámbito
/// de Workspace, se apoya en <see cref="RequireWorkspaceScopeAttribute"/> (MVP-105): el Workspace
/// activo se resuelve en servidor y se lee de <see cref="IWorkspaceContext"/>, nunca del cliente
/// (RN-034). Ese aislamiento es lo que garantiza que el catálogo de un Workspace no afecte al de
/// otro (CA-1).
///
/// Alcance: alta, edición, listado e inactivación (HU-1/HU-2, CA-2/CA-3). El borrado físico queda
/// fuera: las tareas con histórico se inactivan para no invalidar los registros que las referencian.
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/tasks")]
public sealed class TasksController(
    CreateTaskHandler createTaskHandler,
    UpdateTaskHandler updateTaskHandler,
    ListTasksHandler listTasksHandler,
    IWorkspaceContext workspaceContext) : ControllerBase
{
    /// <summary>Lista el catálogo del Workspace. Filtro opcional: <c>is_active</c>.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery(Name = "is_active")] bool? isActive,
        CancellationToken ct)
    {
        var tasks = await listTasksHandler.HandleAsync(workspaceContext.WorkspaceId, isActive, ct);

        return Ok(new
        {
            data = tasks.Select(ToResponse),
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
        body ??= new Dictionary<string, JsonElement>();

        FieldUpdate<bool> isActive;
        try
        {
            isActive = ReadBool(body, "is_active");
        }
        catch (TaskValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }

        try
        {
            var task = await updateTaskHandler.HandleAsync(
                new UpdateTaskCommand(
                    workspaceContext.WorkspaceId,
                    taskId,
                    ReadString(body, "name"),
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

    private static FieldUpdate<string> ReadString(Dictionary<string, JsonElement> body, string key)
        => body.TryGetValue(key, out var el)
            ? FieldUpdate<string>.Set(el.ValueKind == JsonValueKind.Null ? null : el.GetString())
            : FieldUpdate<string>.Absent;

    private static FieldUpdate<bool> ReadBool(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<bool>.Absent;
        if (el.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return FieldUpdate<bool>.Set(el.GetBoolean());

        throw new TaskValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser booleano.");
    }

    private static object ToResponse(TaskSummary task) => new
    {
        id = task.Id,
        workspace_id = task.WorkspaceId,
        name = task.Name,
        is_active = task.IsActive
    };
}

/// <summary>
/// Alta de tarea. Solo <c>name</c> es obligatorio (CA-2); <c>is_active</c> permite dar de alta una
/// tarea ya inactiva (por defecto nace activa).
/// </summary>
public sealed record CreateTaskRequest(
    [Required(ErrorMessage = "El nombre de la tarea es obligatorio.")]
    [StringLength(TaskItem.NameMaxLength, ErrorMessage = "El nombre de la tarea es demasiado largo.")]
    string Name,
    [property: JsonPropertyName("is_active")] bool? IsActive);
