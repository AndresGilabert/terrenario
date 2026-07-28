namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// MVP-206 — Puerto de las solicitudes de traspaso y reactivación de un Workspace dado de baja.
/// </summary>
public interface IWorkspaceReactivationRequestRepository
{
    Task AddRangeAsync(IEnumerable<WorkspaceReactivationRequest> requests, CancellationToken ct = default);

    /// <summary>La búsqueda es siempre por hash: el token en claro no se persiste (patrón de MVP-103).</summary>
    Task<WorkspaceReactivationRequest?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    Task<WorkspaceReactivationRequest?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Solicitudes que esperan la decisión de quien dio de baja el Workspace (HU-6), de la más
    /// reciente a la más antigua, con el nombre del Workspace y de quien la pide.
    /// </summary>
    Task<IReadOnlyList<ReactivationRequestDetail>> ListPendingAuthorizationsAsync(
        Guid authorizerUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Solicitudes vivas (<c>pendiente</c> o <c>solicitada</c>) de un Workspace. Al reactivarlo o al
    /// volver a darlo de baja hay que cerrarlas para que ningún enlace antiguo siga sirviendo.
    /// </summary>
    Task<IReadOnlyList<WorkspaceReactivationRequest>> ListOpenForWorkspaceAsync(
        Guid workspaceId,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Proyección de lectura de una solicitud pendiente de autorizar, con el contexto que necesita la
/// pantalla de decisión (MVP-206, HU-6): de qué Workspace se trata y quién lo pide.
/// </summary>
public sealed record ReactivationRequestDetail(
    Guid Id,
    Guid WorkspaceId,
    string WorkspaceName,
    Guid RequesterUserId,
    string RequesterDisplayName,
    string RequesterEmail,
    DateTimeOffset RequestedAt,
    DateTimeOffset ExpiresAt);
