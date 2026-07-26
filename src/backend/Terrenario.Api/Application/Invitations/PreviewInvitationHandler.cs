using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Application.Invitations;

/// <summary>
/// MVP-103 / MVP-107 — Datos que se muestran a quien abre un enlace de invitación antes de
/// aceptarlo. No expone el email destinatario: quien tiene el enlace no siempre es la persona
/// invitada. Añade la aptitud de la cuenta autenticada (R-C) para no toparse con un 403 tras pulsar.
/// </summary>
public sealed class PreviewInvitationHandler(
    IWorkspaceInvitationRepository invitationRepository,
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IInvitationTokenService tokenService)
{
    public async Task<InvitationPreview> HandleAsync(
        string token,
        Guid viewerUserId,
        CancellationToken ct = default)
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
        var viewer = await userRepository.FindByIdAsync(viewerUserId, ct);

        var alreadyMember = await workspaceRepository.HasActiveMembershipAsync(workspace.Id, viewerUserId, ct);
        var (canAccept, reason) = EvaluateAptitude(invitation, viewer?.Email, alreadyMember);

        return new InvitationPreview(
            invitation.Id,
            invitation.Channel,
            invitation.Status,
            new WorkspaceSummary(workspace.Id, workspace.Name),
            invitedBy?.DisplayName,
            invitation.ExpiresAt,
            invitation.IsExpiredAt(DateTimeOffset.UtcNow),
            canAccept,
            reason);
    }

    /// <summary>
    /// Traduce el estado de la invitación frente a la cuenta autenticada en un veredicto de
    /// aptitud. Es el mismo orden de validación que <c>WorkspaceInvitation.Accept</c>, de modo que
    /// el preview anticipe exactamente lo que ocurriría al pulsar "Aceptar".
    /// </summary>
    private static (bool CanAccept, string? Reason) EvaluateAptitude(
        WorkspaceInvitation invitation,
        string? viewerEmail,
        bool alreadyMember)
    {
        if (invitation.Status == InvitationStatuses.Accepted)
            return (false, InvitationViewerReasons.AlreadyUsed);

        if (invitation.Status == InvitationStatuses.Rejected)
            return (false, InvitationViewerReasons.AlreadyRejected);

        if (invitation.IsExpiredAt(DateTimeOffset.UtcNow))
            return (false, InvitationViewerReasons.Expired);

        if (!invitation.IsAddressedTo(viewerEmail))
            return (false, InvitationViewerReasons.EmailMismatch);

        // Ya es miembro: aceptar no falla (es idempotente y sitúa la sesión), solo se informa.
        return alreadyMember
            ? (true, InvitationViewerReasons.AlreadyMember)
            : (true, null);
    }
}
