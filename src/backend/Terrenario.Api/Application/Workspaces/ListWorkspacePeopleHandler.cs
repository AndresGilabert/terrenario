using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-204 (HU-3, CA-4/CA-5) — Personas del Workspace con su estado de membresía. Combina las
/// membresías reales (<c>activo</c>/<c>revocado</c> desde <c>workspace_members</c>) con las
/// invitaciones por email pendientes (<c>invitado</c> desde <c>workspace_invitations</c>), tal como
/// decide el spec: el estado <c>invitado</c> no se materializa como fila de membresía porque
/// <c>user_id</c> es NOT NULL y la persona invitada puede no tener cuenta todavía.
/// </summary>
public sealed class ListWorkspacePeopleHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceInvitationRepository invitationRepository)
{
    public async Task<WorkspacePeopleResult> HandleAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var members = await workspaceRepository.ListMembersAsync(workspaceId, ct);
        var invitations = await invitationRepository.ListPendingEmailAsync(workspaceId, ct);

        var now = DateTimeOffset.UtcNow;
        var invited = invitations
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new WorkspaceInvitedDetail(
                i.Id,
                i.Email!,
                i.ExpiresAt,
                i.CreatedAt,
                i.IsExpiredAt(now)))
            .ToList();

        return new WorkspacePeopleResult(members, invited);
    }
}
