using Terrenario.Api.Application.Tasks.Commands;
using Terrenario.Api.Domain.Tasks;

namespace Terrenario.Api.Application.Tasks;

/// <summary>
/// MVP-205 — Edita una tarea del catálogo del Workspace activo (HU-1) o cambia su estado de
/// actividad (HU-2, CA-3). La tarea se busca acotada al Workspace: si no existe en él, devuelve
/// <c>null</c> y el borde de transporte responde 404 (no se revela la existencia de tareas de otros
/// Workspaces, CA-1).
/// </summary>
public sealed class UpdateTaskHandler(ITaskRepository taskRepository)
{
    public async Task<TaskSummary?> HandleAsync(UpdateTaskCommand command, CancellationToken ct = default)
    {
        var task = await taskRepository.FindByIdAsync(command.WorkspaceId, command.TaskId, ct);
        if (task is null) return null;

        // Edición parcial: los campos ausentes conservan su valor actual (no se borran). El nombre se
        // normaliza y valida primero (400) y solo después se comprueba el duplicado (409), sin tocar
        // el agregado hasta que ambas guardas pasan.
        if (command.Name.Present)
        {
            var normalized = TaskItem.NormalizeName(command.Name.Value!);
            await CreateTaskHandler.EnsureNameIsFreeAsync(
                taskRepository, command.WorkspaceId, normalized, task.Id, ct);
            task.Rename(normalized);
        }

        if (command.IsActive.Present)
            task.SetActive(command.IsActive.Value);

        await taskRepository.SaveChangesAsync(ct);

        return ListTasksHandler.ToSummary(task);
    }
}
