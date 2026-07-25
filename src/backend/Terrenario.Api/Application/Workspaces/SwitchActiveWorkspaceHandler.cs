using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-104 — Cambia el Workspace activo del usuario y reemite la sesión situada en él (CA-2, CA-3).
/// El cambio solo procede si el usuario tiene membresía activa en el destino.
/// </summary>
public sealed class SwitchActiveWorkspaceHandler(
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IJwtService jwtService)
{
    public async Task<SwitchActiveWorkspaceResult> HandleAsync(
        SwitchActiveWorkspaceCommand command,
        CancellationToken ct = default)
    {
        // Se exige membresía activa: un Workspace ajeno, inexistente o con la membresía revocada
        // se rechazan igual, sin filtrar cuál de los tres es.
        var workspace = await workspaceRepository.FindForMemberAsync(command.WorkspaceId, command.UserId, ct)
            ?? throw new WorkspaceAccessDeniedException(
                "No puedes activar un Workspace en el que no eres miembro.");

        var user = await userRepository.FindByIdAsync(command.UserId, ct)
            ?? throw new WorkspaceAccessDeniedException("La sesión no corresponde a ningún usuario.");

        user.SetActiveWorkspace(workspace.Id);
        await userRepository.SaveChangesAsync(ct);

        var accessToken = jwtService.IssueAccessToken(user.Id, command.DisplayName ?? user.DisplayName, workspace.Id);

        return new SwitchActiveWorkspaceResult(
            new WorkspaceSummary(workspace.Id, workspace.Name),
            accessToken.Token,
            accessToken.ExpiresIn);
    }
}
