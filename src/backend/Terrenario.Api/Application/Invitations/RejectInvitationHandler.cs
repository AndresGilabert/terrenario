using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Application.Invitations;

/// <summary>
/// MVP-107 — Declina una invitación sin crear membresía (HU-2, punto 6). No cierra sesión ni
/// cambia el Workspace activo: la persona puede seguir operando. Cubre las dos vías de la historia,
/// la del enlace (por token) y la de la bandeja de recibidas (por identificador).
/// </summary>
public sealed class RejectInvitationHandler(
    IWorkspaceInvitationRepository invitationRepository,
    IUserRepository userRepository,
    IInvitationTokenService tokenService)
{
    /// <summary>Rechazo por token: quien tiene el enlace declina la invitación que está viendo.</summary>
    public async Task HandleByTokenAsync(Guid userId, string token, CancellationToken ct = default)
    {
        var invitation = await invitationRepository.FindByTokenHashAsync(tokenService.Hash(token), ct)
            ?? throw NotFound();

        await RejectAsync(invitation, userId, ct);
    }

    /// <summary>
    /// Rechazo por identificador desde la bandeja de recibidas (HU-3): la autorización es por
    /// titularidad del email, como en la aceptación por id. Una invitación no dirigida a esta
    /// cuenta se trata como inexistente para no revelar su existencia.
    /// </summary>
    public async Task HandleByIdAsync(Guid userId, Guid invitationId, CancellationToken ct = default)
    {
        var invitation = await invitationRepository.FindByIdAsync(invitationId, ct);
        var user = await FindUserAsync(userId, ct);

        if (invitation is null
            || invitation.Channel != InvitationChannels.Email
            || !invitation.IsAddressedTo(user.Email))
            throw NotFound();

        invitation.Reject(user.Id, user.Email, DateTimeOffset.UtcNow);
        await invitationRepository.SaveChangesAsync(ct);
    }

    private async Task RejectAsync(WorkspaceInvitation invitation, Guid userId, CancellationToken ct)
    {
        var user = await FindUserAsync(userId, ct);

        invitation.Reject(user.Id, user.Email, DateTimeOffset.UtcNow);
        await invitationRepository.SaveChangesAsync(ct);
    }

    private async Task<User> FindUserAsync(Guid userId, CancellationToken ct)
        => await userRepository.FindByIdAsync(userId, ct)
            ?? throw new InvitationException(
                ErrorCodes.AuthUnauthenticated,
                "Token de acceso ausente o no válido.");

    private static InvitationException NotFound()
        => new(ErrorCodes.InvitationNotFound, "Esta invitación no existe o ya no es válida.");
}
