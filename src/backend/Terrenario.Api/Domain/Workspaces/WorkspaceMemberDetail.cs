namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Proyección de lectura de una persona con membresía real en el Workspace (MVP-204, HU-3). Combina
/// la fila de <c>workspace_members</c> con los datos de la cuenta (<c>users</c>) para la vista de
/// personas del Workspace. El estado sale del catálogo <c>worker_member_status</c>
/// (<c>activo</c>/<c>revocado</c>); las personas en estado <c>invitado</c> no tienen fila aquí: son
/// invitaciones por email pendientes que se combinan aparte.
/// </summary>
public sealed record WorkspaceMemberDetail(
    Guid UserId,
    string DisplayName,
    string Email,
    string Role,
    string Status,
    DateTimeOffset JoinedAt);
