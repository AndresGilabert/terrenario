namespace Terrenario.Api.Domain.Workspaces;

public interface IWorkspaceInvitationRepository
{
    Task AddAsync(WorkspaceInvitation invitation, CancellationToken ct = default);

    /// <summary>La búsqueda es siempre por hash: el token en claro no se persiste.</summary>
    Task<WorkspaceInvitation?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Invitaciones pendientes del Workspace, de la más reciente a la más antigua.</summary>
    Task<IReadOnlyList<WorkspaceInvitation>> ListPendingAsync(Guid workspaceId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
