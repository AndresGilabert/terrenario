using Terrenario.Api.Application.Workspaces.Commands;

namespace Terrenario.Api.Application.Invitations.Commands;

/// <summary>
/// El Workspace no viaja en la petición: lo resuelve el servidor desde la sesión, igual que en
/// MVP-102, para que nadie pueda invitar a un Workspace ajeno.
/// </summary>
public sealed record CreateInvitationCommand(
    Guid WorkspaceId,
    string WorkspaceName,
    Guid InvitedByUserId,
    string? InviterDisplayName,
    string Channel,
    string? Email);

/// <summary>
/// <paramref name="AcceptUrl"/> es la única vez que el enlace existe en claro: en base de datos
/// solo queda su hash, así que no puede recuperarse más tarde.
/// </summary>
public sealed record CreateInvitationResult(
    Guid Id,
    string Channel,
    string? Email,
    string Status,
    string AcceptUrl,
    DateTimeOffset ExpiresAt,
    bool EmailSent);

public sealed record InvitationSummary(
    Guid Id,
    string Channel,
    string? Email,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);

public sealed record AcceptInvitationCommand(Guid UserId, string Token);

/// <summary>
/// La aceptación reemite la sesión ya situada en el Workspace de la invitación (CA-2), del mismo
/// modo que la creación de Workspace en MVP-102.
/// </summary>
public sealed record AcceptInvitationResult(
    WorkspaceSummary Workspace,
    string AccessToken,
    int ExpiresIn,
    bool AlreadyMember);

/// <summary>Datos que ve quien abre el enlace antes de decidir si acepta.</summary>
public sealed record InvitationPreview(
    Guid Id,
    string Channel,
    string Status,
    WorkspaceSummary Workspace,
    string? InvitedByDisplayName,
    DateTimeOffset ExpiresAt,
    bool IsExpired);
