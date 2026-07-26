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
/// Resumen de una invitación recibida por la cuenta autenticada (MVP-107, HU-3). Se identifica
/// por <paramref name="Id"/> —no por token— porque quien la recibe por email nunca tuvo el enlace
/// en claro. No expone el email destinatario: es siempre el de quien consulta.
/// </summary>
public sealed record ReceivedInvitationSummary(
    Guid Id,
    string Channel,
    WorkspaceSummary Workspace,
    string? InvitedByDisplayName,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);

/// <summary>
/// Motivo por el que la cuenta autenticada no puede aceptar una invitación (MVP-107, R-C). Se
/// muestra en el preview antes de aceptar para sustituir el error tardío. Los valores son
/// vocabulario de contrato de API, estables para el cliente.
/// </summary>
public static class InvitationViewerReasons
{
    public const string EmailMismatch = "email_mismatch";
    public const string Expired = "expired";
    public const string AlreadyUsed = "already_used";
    public const string AlreadyRejected = "already_rejected";
    /// <summary>Sí puede "entrar": aceptar es idempotente y sitúa la sesión en el Workspace.</summary>
    public const string AlreadyMember = "already_member";
}

/// <summary>
/// La aceptación reemite la sesión ya situada en el Workspace de la invitación (CA-2), del mismo
/// modo que la creación de Workspace en MVP-102.
/// </summary>
public sealed record AcceptInvitationResult(
    WorkspaceSummary Workspace,
    string AccessToken,
    int ExpiresIn,
    bool AlreadyMember);

/// <summary>
/// Datos que ve quien abre el enlace antes de decidir si acepta. Incluye la aptitud de la cuenta
/// autenticada (MVP-107, R-C): <paramref name="ViewerCanAccept"/> anticipa si aceptar funcionará y
/// <paramref name="ViewerReason"/> explica el porqué cuando no, evitando el error tardío tras pulsar.
/// </summary>
public sealed record InvitationPreview(
    Guid Id,
    string Channel,
    string Status,
    WorkspaceSummary Workspace,
    string? InvitedByDisplayName,
    DateTimeOffset ExpiresAt,
    bool IsExpired,
    bool ViewerCanAccept,
    string? ViewerReason);
