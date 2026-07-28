using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces.Commands;

/// <summary>
/// Acceso pendiente en estado <c>invitado</c> de la vista de personas (MVP-204, HU-3). No tiene fila
/// en <c>workspace_members</c> todavía; al aceptarse pasará a <c>activo</c> sin duplicarse (CA-5).
/// <see cref="IsExpired"/> avisa de que conviene reenviarlo.
///
/// MVP-208 (CA-7) — Incluye los dos canales. El <c>enlace</c> compartible no tiene destinatario
/// (<see cref="Email"/> nulo), así que no es una «persona», pero sí es un acceso vivo que hay que
/// poder retirar: dejarlo fuera era lo que impedía anularlo desde ninguna pantalla (hallazgo R-15).
/// </summary>
public sealed record WorkspaceInvitedDetail(
    Guid InvitationId,
    string Channel,
    string? Email,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    bool IsExpired);

/// <summary>
/// Personas y accesos pendientes del Workspace (MVP-204, HU-3): membresías reales
/// (<c>activo</c>/<c>revocado</c>) más las invitaciones pendientes proyectadas como <c>invitado</c>.
/// La combinación se hace en el caso de uso, no en base de datos, según la decisión de diseño del
/// spec (no materializar <c>invitado</c> como fila).
/// </summary>
public sealed record WorkspacePeopleResult(
    IReadOnlyList<WorkspaceMemberDetail> Members,
    IReadOnlyList<WorkspaceInvitedDetail> Invited);
