using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Auth;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Application.Invitations;

/// <summary>
/// MVP-103 — Convierte una invitación válida en membresía activa del Workspace y deja la sesión
/// situada en él (CA-2 y CA-3).
/// </summary>
public sealed class AcceptInvitationHandler(
    IWorkspaceInvitationRepository invitationRepository,
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IInvitationTokenService tokenService,
    IJwtService jwtService)
{
    /// <summary>Aceptación por token: el flujo de quien abre el enlace de invitación (MVP-103).</summary>
    public async Task<AcceptInvitationResult> HandleAsync(
        AcceptInvitationCommand command,
        CancellationToken ct = default)
    {
        var invitation = await FindInvitationAsync(command.Token, ct);
        return await AcceptAsync(invitation, command.UserId, ct);
    }

    /// <summary>
    /// Aceptación por identificador desde la bandeja de invitaciones recibidas (MVP-107, HU-3):
    /// la persona invitada nunca tuvo el token en claro. La autorización es por titularidad del
    /// email —el JWT prueba que la cuenta es su dueña— así que una invitación no dirigida a esta
    /// cuenta se trata como inexistente para no revelar su existencia.
    /// </summary>
    public async Task<AcceptInvitationResult> HandleByIdAsync(
        Guid userId,
        Guid invitationId,
        CancellationToken ct = default)
    {
        var invitation = await invitationRepository.FindByIdAsync(invitationId, ct);
        var user = await FindUserAsync(userId, ct);

        if (invitation is null
            || invitation.Channel != InvitationChannels.Email
            || !invitation.IsAddressedTo(user.Email))
            throw new InvitationException(
                ErrorCodes.InvitationNotFound,
                "Esta invitación no existe o ya no es válida.");

        return await AcceptAsync(invitation, user, ct);
    }

    private async Task<AcceptInvitationResult> AcceptAsync(
        WorkspaceInvitation invitation,
        Guid userId,
        CancellationToken ct)
        => await AcceptAsync(invitation, await FindUserAsync(userId, ct), ct);

    private async Task<AcceptInvitationResult> AcceptAsync(
        WorkspaceInvitation invitation,
        User user,
        CancellationToken ct)
    {
        var workspace = await workspaceRepository.FindByIdAsync(invitation.WorkspaceId, ct)
            ?? throw new InvitationException(
                ErrorCodes.WorkspaceNotFound,
                "El Workspace de esta invitación ya no está disponible.");

        invitation.Accept(user.Id, user.Email, DateTimeOffset.UtcNow);

        // Reaceptar desde una cuenta que ya es miembro no debe chocar con el índice único
        // (workspace_id, user_id): la invitación se consume y la membresía se deja como está.
        var alreadyMember = await workspaceRepository.HasActiveMembershipAsync(workspace.Id, user.Id, ct);

        if (!alreadyMember)
            await workspaceRepository.AddMemberAsync(WorkspaceMember.CreateMember(workspace.Id, user.Id), ct);

        // El Workspace recién aceptado queda como activo persistido para no perderlo en la
        // siguiente renovación de sesión (MVP-104).
        user.SetActiveWorkspace(workspace.Id);

        // Ambos repositorios comparten el DbContext de la petición, así que membresía,
        // invitación y preferencia se escriben en la misma transacción implícita de EF Core.
        await invitationRepository.SaveChangesAsync(ct);

        var accessToken = jwtService.IssueAccessToken(user.Id, user.DisplayName, workspace.Id);

        return new AcceptInvitationResult(
            new WorkspaceSummary(workspace.Id, workspace.Name),
            accessToken.Token,
            accessToken.ExpiresIn,
            alreadyMember);
    }

    private async Task<WorkspaceInvitation> FindInvitationAsync(string token, CancellationToken ct)
        => await invitationRepository.FindByTokenHashAsync(tokenService.Hash(token), ct)
            ?? throw new InvitationException(
                ErrorCodes.InvitationNotFound,
                "Esta invitación no existe o ya no es válida.");

    private async Task<User> FindUserAsync(Guid userId, CancellationToken ct)
        => await userRepository.FindByIdAsync(userId, ct)
            ?? throw new InvitationException(
                ErrorCodes.AuthUnauthenticated,
                "Token de acceso ausente o no válido.");
}
