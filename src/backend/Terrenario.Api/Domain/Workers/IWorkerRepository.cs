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
    /// Fila de responsable de un miembro del Workspace (MVP-208, CA-1). Es la clave por la que el
    /// maestro sigue a la membresía sin intervención manual: alta al aceptar la invitación,
    /// inactivación al revocar el acceso y reactivación al readmitirlo.
    /// </summary>
    Task<Worker?> FindByUserAccountAsync(
        Guid workspaceId,
        Guid userAccountId,
        CancellationToken ct = default);

    /// <summary>
    /// Filas de responsable que una cuenta tiene en cualquier Workspace (MVP-208). Lo usa la
    /// resincronización del nombre de display de Google (RN-036), que afecta a todas a la vez.
    /// </summary>
    Task<IReadOnlyList<Worker>> ListByUserAccountAsync(Guid userAccountId, CancellationToken ct = default);

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
    /// Cubre todo el maestro, también los inactivos: inactivar no libera el nombre. Desde MVP-208
    /// cubre además la unión miembro/cuadrilla, porque los miembros son filas de este maestro (CA-3).
    /// </summary>
    /// <param name="excludeWorkerId">Trabajador que se excluye de la comparación (el que se renombra).</param>
    Task<bool> ExistsWithNameAsync(
        Guid workspaceId,
        string name,
        Guid? excludeWorkerId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Trabajador que ocupa ese nombre en el Workspace, ignorando mayúsculas (MVP-208). La
    /// materialización de un miembro necesita saber <b>quién</b> ocupa el nombre, no solo si está
    /// ocupado: la fila que se renombra con sufijo es la de cuadrilla, nunca la del miembro.
    /// </summary>
    Task<Worker?> FindByNameAsync(
        Guid workspaceId,
        string name,
        Guid? excludeWorkerId = null,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
