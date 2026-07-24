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

    Task SaveChangesAsync(CancellationToken ct = default);
}
