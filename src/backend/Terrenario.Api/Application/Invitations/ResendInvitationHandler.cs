using Microsoft.Extensions.Options;
using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Application.Invitations;

/// <summary>
/// MVP-204 (HU-5, CA-6) — Reenvía una invitación por email pendiente del Workspace activo. Reutiliza
/// el emisor de MVP-103: genera un token nuevo de un solo uso y renueva la caducidad (
/// <see cref="WorkspaceInvitation.Reissue"/>), con el mismo resultado que la emisión original. La
/// persona sigue en estado <c>invitado</c>: no cambia el destinatario ni el canal.
/// </summary>
public sealed class ResendInvitationHandler(
    IWorkspaceInvitationRepository invitationRepository,
    IInvitationTokenService tokenService,
    IInvitationEmailSender emailSender,
    IOptions<InvitationOptions> options,
    ILogger<ResendInvitationHandler> logger)
{
    private readonly InvitationOptions _options = options.Value;

    public async Task<ResendInvitationResult> HandleAsync(
        ResendInvitationCommand command,
        CancellationToken ct = default)
    {
        var invitation = await invitationRepository.FindByIdAsync(command.InvitationId, ct);

        // Solo se reenvía una invitación por email pendiente del Workspace activo. Cualquier otra
        // (inexistente, de otro Workspace, canal enlace o ya aceptada/rechazada) se oculta como 404
        // para no revelar invitaciones ajenas ni el estado de las que ya no son "invitado".
        if (invitation is null
            || invitation.WorkspaceId != command.WorkspaceId
            || invitation.Channel != InvitationChannels.Email
            || invitation.Status != InvitationStatuses.Pending)
            throw new InvitationException(
                ErrorCodes.InvitationNotFound,
                "La invitación no existe o ya no está disponible en tu Workspace.");

        var token = tokenService.Generate();
        invitation.Reissue(token.Hash, _options.Lifetime);
        await invitationRepository.SaveChangesAsync(ct);

        var acceptUrl = _options.BuildAcceptUrl(token.Value);

        // Por email se reenvía el correo; por enlace solo se devuelve el nuevo accept_url.
        var emailSent = command.DeliverEmail && await TrySendEmailAsync(invitation, command, acceptUrl, ct);

        return new ResendInvitationResult(
            invitation.Id,
            invitation.Email!,
            acceptUrl,
            invitation.ExpiresAt,
            emailSent);
    }

    /// <summary>
    /// Ni la falta de cuenta de envío ni un fallo del proveedor invalidan el reenvío: el token ya se
    /// rotó y quien reenvía se queda con el enlace nuevo para compartirlo por otro canal.
    /// </summary>
    private async Task<bool> TrySendEmailAsync(
        WorkspaceInvitation invitation,
        ResendInvitationCommand command,
        string acceptUrl,
        CancellationToken ct)
    {
        if (!emailSender.IsEnabled)
        {
            logger.LogWarning(
                "Sin cuenta de envío configurada: el reenvío de la invitación {InvitationId} no sale por correo.",
                invitation.Id);
            return false;
        }

        try
        {
            await emailSender.SendAsync(
                new InvitationEmail(invitation.Email!, command.WorkspaceName, command.ActingDisplayName, acceptUrl),
                ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo reenviar el email de la invitación {InvitationId}.", invitation.Id);
            return false;
        }
    }
}
