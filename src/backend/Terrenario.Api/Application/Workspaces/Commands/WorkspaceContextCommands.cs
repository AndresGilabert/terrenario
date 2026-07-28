namespace Terrenario.Api.Application.Workspaces.Commands;

/// <summary>Consulta de las membresías vigentes del usuario (MVP-104, HU-1).</summary>
public sealed record ListUserWorkspacesQuery(Guid UserId, Guid? SessionWorkspaceId);

/// <summary>
/// Workspace disponible en el selector. <c>Status</c> viaja aunque hoy solo se listen las
/// membresías vigentes: es el estado del catálogo <c>worker_member_status</c>.
/// </summary>
public sealed record UserWorkspaceItem(
    Guid Id,
    string Name,
    string Role,
    string Status,
    DateTimeOffset JoinedAt);

/// <summary>
/// <c>ActiveWorkspaceId</c> es el que el cliente debe marcar como activo; puede ser <c>null</c>
/// solo si el usuario todavía no pertenece a ningún Workspace.
/// </summary>
public sealed record ListUserWorkspacesResult(
    IReadOnlyList<UserWorkspaceItem> Workspaces,
    Guid? ActiveWorkspaceId);

/// <summary>Cambio de Workspace activo (MVP-104, HU-2).</summary>
public sealed record SwitchActiveWorkspaceCommand(Guid UserId, string? DisplayName, Guid WorkspaceId);

/// <summary>
/// El cambio reemite la sesión con el nuevo contexto, igual que el alta de Workspace (MVP-102)
/// y la aceptación de invitación (MVP-103): el Workspace activo nunca se acepta como parámetro
/// en las operaciones de negocio.
/// </summary>
public sealed record SwitchActiveWorkspaceResult(WorkspaceSummary Workspace, string AccessToken, int ExpiresIn);
