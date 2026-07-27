namespace Terrenario.Api.Domain.Workspaces;

public interface IWorkspaceInvitationRepository
{
    Task AddAsync(WorkspaceInvitation invitation, CancellationToken ct = default);

    /// <summary>La búsqueda es siempre por hash: el token en claro no se persiste.</summary>
    Task<WorkspaceInvitation?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Localiza una invitación por su identificador. Es la vía de la bandeja de recibidas
    /// (MVP-107), donde la persona invitada nunca tuvo el token en claro (viajó por email).
    /// </summary>
    Task<WorkspaceInvitation?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Invitaciones pendientes del Workspace, de la más reciente a la más antigua.</summary>
    Task<IReadOnlyList<WorkspaceInvitation>> ListPendingAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>
    /// Invitaciones por email pendientes del Workspace (MVP-204, HU-3): las personas en estado
    /// <c>invitado</c> de la vista de personas. El canal <c>enlace</c> no tiene destinatario, así que
    /// no genera una persona invitada.
    /// </summary>
    Task<IReadOnlyList<WorkspaceInvitation>> ListPendingEmailAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>
    /// Invitaciones por email pendientes dirigidas a un correo (MVP-107, HU-3). El enlace
    /// compartible no tiene destinatario, así que nunca aparece en la bandeja de nadie.
    /// </summary>
    Task<IReadOnlyList<WorkspaceInvitation>> ListReceivedPendingAsync(
        string canonicalEmail,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
