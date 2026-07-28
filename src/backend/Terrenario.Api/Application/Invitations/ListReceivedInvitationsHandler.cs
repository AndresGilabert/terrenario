using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Invitations;

/// <summary>
/// MVP-107 — Invitaciones por email pendientes dirigidas a la cuenta autenticada (HU-3). Es la
/// fuente del centro de notificaciones: solo aparecen las accionables (no caducadas y a Workspaces
/// de los que aún no se es miembro). El email de la persona nunca viaja de vuelta; se usa solo para
/// filtrar. El enlace compartible no tiene destinatario, así que no forma parte de esta bandeja.
/// </summary>
public sealed class ListReceivedInvitationsHandler(
    IWorkspaceInvitationRepository invitationRepository,
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository)
{
    public async Task<IReadOnlyList<ReceivedInvitationSummary>> HandleAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var user = await userRepository.FindByIdAsync(userId, ct)
            ?? throw new InvitationException(
                ErrorCodes.AuthUnauthenticated,
                "Token de acceso ausente o no válido.");

        var canonicalEmail = user.Email.Trim().ToLowerInvariant();
        var invitations = await invitationRepository.ListReceivedPendingAsync(canonicalEmail, ct);

        var now = DateTimeOffset.UtcNow;
        var summaries = new List<ReceivedInvitationSummary>();

        foreach (var invitation in invitations)
        {
            // Caducadas fuera: no son accionables y se extinguen solas (no hay proceso que las marque).
            if (invitation.IsExpiredAt(now)) continue;

            var workspace = await workspaceRepository.FindByIdAsync(invitation.WorkspaceId, ct);
            // Workspace borrado o del que ya se es miembro: no tiene sentido ofrecer "unirse".
            if (workspace is null) continue;
            if (await workspaceRepository.HasActiveMembershipAsync(workspace.Id, userId, ct)) continue;

            var invitedBy = await userRepository.FindByIdAsync(invitation.InvitedByUserId, ct);

            summaries.Add(new ReceivedInvitationSummary(
                invitation.Id,
                invitation.Channel,
                new WorkspaceSummary(workspace.Id, workspace.Name),
                invitedBy?.DisplayName,
                invitation.ExpiresAt,
                invitation.CreatedAt));
        }

        return summaries;
    }
}
