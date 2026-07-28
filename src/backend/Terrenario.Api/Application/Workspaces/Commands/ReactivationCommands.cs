namespace Terrenario.Api.Application.Workspaces.Commands;

/// <summary>
/// MVP-206 (HU-5) — Lo que ve quien abre el enlace de reactivación antes de decidir: de qué
/// Workspace se trata, quién lo dio de baja y si el enlace sigue sirviendo. No revela nada de otras
/// personas ni del contenido del Workspace.
/// </summary>
public sealed record ReactivationPreview(
    Guid RequestId,
    Guid WorkspaceId,
    string WorkspaceName,
    string? ClosedByDisplayName,
    string Status,
    DateTimeOffset ExpiresAt,
    bool IsExpired,
    bool CanRequest);

/// <summary>
/// Resultado de autorizar una solicitud (HU-6, CA-7): el Workspace vuelve y su propiedad pasa a
/// quien la pidió.
/// </summary>
public sealed record ReactivationOutcome(
    Guid WorkspaceId,
    string WorkspaceName,
    Guid NewOwnerUserId);
