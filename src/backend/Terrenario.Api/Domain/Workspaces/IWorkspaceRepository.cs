namespace Terrenario.Api.Domain.Workspaces;

public interface IWorkspaceRepository
{
    Task AddAsync(Workspace workspace, WorkspaceMember ownerMembership, CancellationToken ct = default);

    /// <summary>Devuelve el Workspace solo si el usuario tiene membresía activa en él.</summary>
    Task<Workspace?> FindForMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Workspace por defecto del usuario: la membresía activa más reciente.
    /// Se usa cuando la sesión todavía no lleva contexto de Workspace.
    /// </summary>
    Task<Workspace?> FindDefaultForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Busca el Workspace sin exigir membresía. Lo necesita la aceptación de invitaciones
    /// (MVP-103), donde el usuario todavía no es miembro.
    /// </summary>
    Task<Workspace?> FindByIdAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>
    /// Membresías vigentes del usuario, ordenadas por nombre de Workspace. Las revocadas
    /// quedan fuera: no dan acceso ni deben aparecer en el selector (MVP-104).
    /// </summary>
    Task<IReadOnlyList<WorkspaceMembership>> ListActiveMembershipsAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<bool> HasActiveMembershipAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Todas las personas con membresía real del Workspace (activas y revocadas), unidas a su cuenta,
    /// para la vista de personas (MVP-204, HU-3). Las <c>invitado</c> no salen de aquí: son
    /// invitaciones por email pendientes que se combinan aparte.
    /// </summary>
    Task<IReadOnlyList<WorkspaceMemberDetail>> ListMembersAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>Membresía <c>activo</c> de un usuario en el Workspace (para revocar, MVP-204). <c>null</c> si no la tiene.</summary>
    Task<WorkspaceMember?> FindActiveMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);

    /// <summary>Número de miembros activos del Workspace. Sostiene la invariante CA-8 (no quedarse sin ninguno).</summary>
    Task<int> CountActiveMembersAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>Número de propietarios activos del Workspace. Sostiene la invariante CA-8 (no quedarse sin propietario).</summary>
    Task<int> CountActiveOwnersAsync(Guid workspaceId, CancellationToken ct = default);

    Task AddMemberAsync(WorkspaceMember member, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
