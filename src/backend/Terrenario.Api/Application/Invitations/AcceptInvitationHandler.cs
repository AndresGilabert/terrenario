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
    public async Task<AcceptInvitationResult> HandleAsync(
        AcceptInvitationCommand command,
        CancellationToken ct = default)
    {
        var invitation = await FindInvitationAsync(command.Token, ct);
        var user = await userRepository.FindByIdAsync(command.UserId, ct)
            ?? throw new InvitationException(
                ErrorCodes.AuthUnauthenticated,
                "Token de acceso ausente o no válido.");

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

        // Ambos repositorios comparten el DbContext de la petición, así que membresía e
        // invitación se escriben en la misma transacción implícita de EF Core.
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
}
