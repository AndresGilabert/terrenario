using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Application.Invitations;

/// <summary>
/// MVP-103 — Datos que se muestran a quien abre un enlace de invitación antes de aceptarlo.
/// No expone el email destinatario: quien tiene el enlace no siempre es la persona invitada.
/// </summary>
public sealed class PreviewInvitationHandler(
    IWorkspaceInvitationRepository invitationRepository,
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IInvitationTokenService tokenService)
{
    public async Task<InvitationPreview> HandleAsync(string token, CancellationToken ct = default)
    {
        var invitation = await invitationRepository.FindByTokenHashAsync(tokenService.Hash(token), ct)
            ?? throw new InvitationException(
                ErrorCodes.InvitationNotFound,
                "Esta invitación no existe o ya no es válida.");

        var workspace = await workspaceRepository.FindByIdAsync(invitation.WorkspaceId, ct)
            ?? throw new InvitationException(
                ErrorCodes.WorkspaceNotFound,
                "El Workspace de esta invitación ya no está disponible.");

        var invitedBy = await userRepository.FindByIdAsync(invitation.InvitedByUserId, ct);

        return new InvitationPreview(
            invitation.Id,
            invitation.Channel,
            invitation.Status,
            new WorkspaceSummary(workspace.Id, workspace.Name),
            invitedBy?.DisplayName,
            invitation.ExpiresAt,
            invitation.IsExpiredAt(DateTimeOffset.UtcNow));
    }
}
