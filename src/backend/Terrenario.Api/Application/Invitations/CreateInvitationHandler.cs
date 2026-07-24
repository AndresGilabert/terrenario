using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Application.Invitations;

/// <summary>
/// MVP-103 — Emite una invitación al Workspace activo por email o por enlace (CA-1).
/// </summary>
public sealed class CreateInvitationHandler(
    IWorkspaceInvitationRepository invitationRepository,
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IInvitationTokenService tokenService,
    IInvitationEmailSender emailSender,
    IOptions<InvitationOptions> options,
    ILogger<CreateInvitationHandler> logger)
{
    private readonly InvitationOptions _options = options.Value;

    public async Task<CreateInvitationResult> HandleAsync(
        CreateInvitationCommand command,
        CancellationToken ct = default)
    {
        var token = tokenService.Generate();

        var invitation = WorkspaceInvitation.Create(
            command.WorkspaceId,
            command.InvitedByUserId,
            command.Channel,
            command.Email,
            token.Hash,
            _options.Lifetime);

        if (invitation.Email is not null)
            await GuardAgainstExistingMemberAsync(command.WorkspaceId, invitation.Email, ct);

        await invitationRepository.AddAsync(invitation, ct);
        await invitationRepository.SaveChangesAsync(ct);

        var acceptUrl = _options.BuildAcceptUrl(token.Value);

        var emailSent = invitation.Email is not null &&
            await TrySendEmailAsync(invitation, command, acceptUrl, ct);

        return new CreateInvitationResult(
            invitation.Id,
            invitation.Channel,
            invitation.Email,
            invitation.Status,
            acceptUrl,
            invitation.ExpiresAt,
            emailSent);
    }

    private async Task GuardAgainstExistingMemberAsync(Guid workspaceId, string email, CancellationToken ct)
    {
        var invitedUser = await userRepository.FindByEmailAsync(email, ct);

        if (invitedUser is not null &&
            await workspaceRepository.HasActiveMembershipAsync(workspaceId, invitedUser.Id, ct))
            throw new InvitationException(
                ErrorCodes.BusinessRuleInvitationAlreadyMember,
                "Esa persona ya forma parte de este Workspace.");
    }

    /// <summary>
    /// Ni la falta de cuenta de envío ni un fallo del proveedor invalidan la invitación: ya está
    /// emitida y quien invita se queda con el enlace para compartirlo por otro canal.
    /// </summary>
    private async Task<bool> TrySendEmailAsync(
        WorkspaceInvitation invitation,
        CreateInvitationCommand command,
        string acceptUrl,
        CancellationToken ct)
    {
        if (!emailSender.IsEnabled)
        {
            logger.LogWarning(
                "Sin cuenta de envío configurada: la invitación {InvitationId} no se envía por correo.",
                invitation.Id);

            return false;
        }

        try
        {
            await emailSender.SendAsync(
                new InvitationEmail(invitation.Email!, command.WorkspaceName, command.InviterDisplayName, acceptUrl),
                ct);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo enviar el email de la invitación {InvitationId}.", invitation.Id);
            return false;
        }
    }
}
