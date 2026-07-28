namespace Terrenario.Api.Domain.Workers;

public interface IWorkerRepository
{
    /// <summary>Registra un trabajador nuevo en la unidad de trabajo en curso.</summary>
    Task AddAsync(Worker worker, CancellationToken ct = default);

    /// <summary>
    /// Trabajador por id dentro del Workspace activo. Devuelve <c>null</c> si no existe o pertenece a
    /// otro Workspace (el aislamiento multi-tenant se refuerza filtrando por <paramref name="workspaceId"/>).
    /// </summary>
    Task<Worker?> FindByIdAsync(Guid workspaceId, Guid workerId, CancellationToken ct = default);

    /// <summary>
    /// Trabajadores del Workspace (MVP-204). Filtra opcionalmente por estado de actividad
    /// (<paramref name="isActive"/>). Orden estable: activos primero y luego por nombre.
    /// </summary>
    Task<IReadOnlyList<Worker>> ListByWorkspaceAsync(
        Guid workspaceId,
        bool? isActive,
        CancellationToken ct = default);

    /// <summary>
    /// ¿Hay ya un trabajador con ese nombre en el Workspace, ignorando mayúsculas? (MVP-207, CA-2).
    /// Cubre todo el maestro, también los inactivos: inactivar no libera el nombre.
    /// </summary>
    /// <param name="excludeWorkerId">Trabajador que se excluye de la comparación (el que se renombra).</param>
    Task<bool> ExistsWithNameAsync(
        Guid workspaceId,
        string name,
        Guid? excludeWorkerId = null,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
