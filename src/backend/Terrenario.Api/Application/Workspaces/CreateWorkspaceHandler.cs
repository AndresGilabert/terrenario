using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-102 — Crea el Workspace, vincula al creador como miembro activo y devuelve una sesión
/// ya situada en ese contexto. Deja el Workspace nuevo como activo persistido para que la sesión
/// renovada no lo pierda (MVP-104).
/// </summary>
public sealed class CreateWorkspaceHandler(
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IJwtService jwtService)
{
    public async Task<CreateWorkspaceResult> HandleAsync(CreateWorkspaceCommand command, CancellationToken ct = default)
    {
        var workspace = Workspace.Create(command.UserId, command.Name);
        var ownerMembership = workspace.CreateOwnerMembership();

        await workspaceRepository.AddAsync(workspace, ownerMembership, ct);

        var user = await userRepository.FindByIdAsync(command.UserId, ct);
        user?.SetActiveWorkspace(workspace.Id);

        // Ambos repositorios comparten el DbContext de la petición: Workspace, membresía y la
        // preferencia del usuario se escriben en la misma transacción implícita de EF Core.
        await workspaceRepository.SaveChangesAsync(ct);

        var accessToken = jwtService.IssueAccessToken(command.UserId, command.DisplayName, workspace.Id);

        return new CreateWorkspaceResult(
            new WorkspaceSummary(workspace.Id, workspace.Name),
            accessToken.Token,
            accessToken.ExpiresIn);
    }
}
