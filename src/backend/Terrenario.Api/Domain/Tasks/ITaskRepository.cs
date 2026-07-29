namespace Terrenario.Api.Domain.Tasks;

public interface ITaskRepository
{
    /// <summary>Registra una tarea nueva en la unidad de trabajo en curso.</summary>
    Task AddAsync(TaskItem task, CancellationToken ct = default);

    /// <summary>
    /// Tarea por id dentro del Workspace activo. Devuelve <c>null</c> si no existe o pertenece a otro
    /// Workspace (el aislamiento multi-tenant se refuerza filtrando por <paramref name="workspaceId"/>).
    /// </summary>
    Task<TaskItem?> FindByIdAsync(Guid workspaceId, Guid taskId, CancellationToken ct = default);

    /// <summary>
    /// Tareas del catálogo del Workspace (MVP-205). Filtra opcionalmente por estado de actividad
    /// (<paramref name="isActive"/>). Orden estable: activas primero y luego por nombre.
    /// </summary>
    Task<IReadOnlyList<TaskItem>> ListByWorkspaceAsync(
        Guid workspaceId,
        bool? isActive,
        CancellationToken ct = default);

    /// <summary>
    /// Indica si el Workspace ya tiene una tarea con ese nombre, <b>ignorando mayúsculas</b>
    /// (prevención de duplicados evidentes del catálogo). <paramref name="excludeTaskId"/> permite
    /// excluir la propia tarea al renombrarla.
    /// </summary>
    Task<bool> ExistsWithNameAsync(
        Guid workspaceId,
        string name,
        Guid? excludeTaskId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Tarea que ocupa ese nombre en el Workspace, <b>ignorando mayúsculas</b> (MVP-302). El guardado
    /// de una tarea libre necesita saber <b>cuál</b> es la tarea existente, no solo si el nombre está
    /// ocupado: la ofrece para reutilizarla en vez de crear una segunda. Ve también las inactivas,
    /// que siguen ocupando su nombre (MVP-205, CA-3).
    /// </summary>
    Task<TaskItem?> FindByNameAsync(
        Guid workspaceId,
        string name,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
