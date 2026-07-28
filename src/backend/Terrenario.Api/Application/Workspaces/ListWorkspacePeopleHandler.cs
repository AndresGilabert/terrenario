using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-204 (HU-3, CA-4/CA-5) — Personas del Workspace con su estado de membresía. Combina las
/// membresías reales (<c>activo</c>/<c>revocado</c> desde <c>workspace_members</c>) con las
/// invitaciones pendientes (<c>invitado</c> desde <c>workspace_invitations</c>), tal como decide el
/// spec: el estado <c>invitado</c> no se materializa como fila de membresía porque <c>user_id</c> es
/// NOT NULL y la persona invitada puede no tener cuenta todavía.
///
/// MVP-208 (CA-7) — Proyecta los <b>dos canales</b>, no solo <c>email</c>. Esta es la superficie
/// única de administración de invitaciones pendientes: es la que ya tenía las acciones (reenviar,
/// anular, revocar), así que extenderla es lo que cierra a la vez R-15 (un enlace no se podía anular
/// desde ninguna pantalla) y R-21 (dos listas del mismo concepto con reglas distintas).
/// </summary>
public sealed class ListWorkspacePeopleHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceInvitationRepository invitationRepository)
{
    public async Task<WorkspacePeopleResult> HandleAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var members = await workspaceRepository.ListMembersAsync(workspaceId, ct);
        var invitations = await invitationRepository.ListPendingAsync(workspaceId, ct);

        var now = DateTimeOffset.UtcNow;
        var invited = invitations
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new WorkspaceInvitedDetail(
                i.Id,
                i.Channel,
                i.Email,
                i.ExpiresAt,
                i.CreatedAt,
                i.IsExpiredAt(now)))
            .ToList();

        return new WorkspacePeopleResult(members, invited);
    }
}
