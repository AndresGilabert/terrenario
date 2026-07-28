using Terrenario.Api.Application.Tasks.Commands;
using Terrenario.Api.Domain.Tasks;

namespace Terrenario.Api.Application.Tasks;

/// <summary>
/// MVP-205 — Lista el catálogo de tareas del Workspace activo (CA-1). Admite filtro por estado de
/// actividad, alineado con <c>GET /api/v1/tasks</c> (<c>is_active?</c>): la operativa diaria pedirá
/// solo las activas y el maestro puede ver también las inactivadas (CA-3).
/// </summary>
public sealed class ListTasksHandler(ITaskRepository taskRepository)
{
    public async Task<IReadOnlyList<TaskSummary>> HandleAsync(
        Guid workspaceId,
        bool? isActive,
        CancellationToken ct = default)
    {
        var tasks = await taskRepository.ListByWorkspaceAsync(workspaceId, isActive, ct);
        return tasks.Select(ToSummary).ToList();
    }

    internal static TaskSummary ToSummary(TaskItem task) => new(
        task.Id,
        task.WorkspaceId,
        task.Name,
        task.IsActive);
}
