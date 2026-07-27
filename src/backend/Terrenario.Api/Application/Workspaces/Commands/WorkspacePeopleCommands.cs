using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces.Commands;

/// <summary>
/// Persona en estado <c>invitado</c> de la vista de personas (MVP-204, HU-3): una invitación por
/// email pendiente. No tiene fila en <c>workspace_members</c> todavía; al aceptarse pasará a
/// <c>activo</c> sin duplicarse (CA-5). <see cref="IsExpired"/> avisa de que conviene reenviarla.
/// </summary>
public sealed record WorkspaceInvitedDetail(
    Guid InvitationId,
    string Email,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    bool IsExpired);

/// <summary>
/// Personas del Workspace (MVP-204, HU-3): membresías reales (<c>activo</c>/<c>revocado</c>) más las
/// invitaciones por email pendientes proyectadas como <c>invitado</c>. La combinación se hace en el
/// caso de uso, no en base de datos, según la decisión de diseño del spec (no materializar
/// <c>invitado</c> como fila).
/// </summary>
public sealed record WorkspacePeopleResult(
    IReadOnlyList<WorkspaceMemberDetail> Members,
    IReadOnlyList<WorkspaceInvitedDetail> Invited);
