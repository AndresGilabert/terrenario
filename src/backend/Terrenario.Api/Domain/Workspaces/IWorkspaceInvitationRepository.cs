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

    /// <summary>
    /// Invitaciones pendientes del Workspace, de <b>cualquier canal</b>. Es la superficie única de
    /// administración de pendientes que decide MVP-208 (CA-7): antes había dos listados con reglas
    /// distintas y el canal <c>enlace</c> —el de mayor riesgo si se filtra— se quedaba sin acciones.
    /// Sin <c>ORDER BY</c> en base de datos: el caso de uso ordena en memoria para no ordenar por
    /// <c>DateTimeOffset</c>, que EF+SQLite no traduce (aunque PostgreSQL sí).
    /// </summary>
    Task<IReadOnlyList<WorkspaceInvitation>> ListPendingAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>
    /// Invitaciones por email pendientes dirigidas a un correo (MVP-107, HU-3). El enlace
    /// compartible no tiene destinatario, así que nunca aparece en la bandeja de nadie.
    /// </summary>
    Task<IReadOnlyList<WorkspaceInvitation>> ListReceivedPendingAsync(
        string canonicalEmail,
        CancellationToken ct = default);

    /// <summary>
    /// MVP-505 (CA-3) — Anula las invitaciones pendientes dirigidas a un correo. Lo usa la baja de
    /// cuenta: una invitacion pendiente lleva el email escrito, asi que sin esto el dato personal
    /// sobreviviria a la supresion. Devuelve cuantas se anularon.
    /// </summary>
    Task<int> CancelPendingForEmailAsync(string email, Guid cancelledByUserId, DateTimeOffset now, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
