using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Invitations;

/// <summary>
/// MVP-103 — Invitaciones pendientes del Workspace activo, para que sus miembros vean qué hay
/// en circulación (CA-3). No devuelve el enlace: en base de datos solo está su hash.
/// </summary>
public sealed class ListWorkspaceInvitationsHandler(IWorkspaceInvitationRepository invitationRepository)
{
    public async Task<IReadOnlyList<InvitationSummary>> HandleAsync(
        Guid workspaceId,
        CancellationToken ct = default)
    {
        var invitations = await invitationRepository.ListPendingAsync(workspaceId, ct);

        return invitations
            .Select(invitation => new InvitationSummary(
                invitation.Id,
                invitation.Channel,
                invitation.Email,
                invitation.Status,
                invitation.ExpiresAt,
                invitation.CreatedAt))
            .ToList();
    }
}
