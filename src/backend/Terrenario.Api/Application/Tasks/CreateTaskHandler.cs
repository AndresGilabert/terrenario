using Terrenario.Api.Application.Tasks.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Tasks;

namespace Terrenario.Api.Application.Tasks;

/// <summary>
/// MVP-205 — Da de alta una tarea en el catálogo del Workspace activo (HU-1, CA-2). Solo el nombre
/// es obligatorio: el catálogo arranca vacío y se puebla sin configuración externa (CA-2).
///
/// Antes de persistir se comprueba que el Workspace no tenga ya una tarea con el mismo nombre
/// ignorando mayúsculas: el catálogo existe para dar consistencia (RN-026) y esa misma guarda la
/// reutilizará el guardado de tarea libre desde la operativa diaria (MVP-302).
/// </summary>
public sealed class CreateTaskHandler(ITaskRepository taskRepository)
{
    public async Task<TaskSummary> HandleAsync(CreateTaskCommand command, CancellationToken ct = default)
    {
        // El dominio normaliza y valida el nombre; se construye primero para no comprobar duplicados
        // contra un texto sin normalizar.
        var task = TaskItem.Create(command.WorkspaceId, command.Name, command.IsActive ?? true);

        await EnsureNameIsFreeAsync(taskRepository, command.WorkspaceId, task.Name, null, ct);

        await taskRepository.AddAsync(task, ct);
        await taskRepository.SaveChangesAsync(ct);

        return ListTasksHandler.ToSummary(task);
    }

    /// <summary>
    /// Guarda de duplicados del catálogo, compartida con la edición. Lanza
    /// <see cref="TaskConflictException"/> (409) si el nombre ya existe en el Workspace.
    /// </summary>
    internal static async Task EnsureNameIsFreeAsync(
        ITaskRepository taskRepository,
        Guid workspaceId,
        string normalizedName,
        Guid? excludeTaskId,
        CancellationToken ct)
    {
        var exists = await taskRepository.ExistsWithNameAsync(workspaceId, normalizedName, excludeTaskId, ct);
        if (exists)
            throw new TaskConflictException(
                ErrorCodes.ConflictTaskNameDuplicate,
                $"Ya existe una tarea «{normalizedName}» en el catálogo de este Workspace.");
    }
}
