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

    Task AddMemberAsync(WorkspaceMember member, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
