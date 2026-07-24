using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-102 — Crea el Workspace, vincula al creador como miembro activo y devuelve una sesión
/// ya situada en ese contexto.
/// </summary>
public sealed class CreateWorkspaceHandler(
    IWorkspaceRepository workspaceRepository,
    IJwtService jwtService)
{
    public async Task<CreateWorkspaceResult> HandleAsync(CreateWorkspaceCommand command, CancellationToken ct = default)
    {
        var workspace = Workspace.Create(command.UserId, command.Name);
        var ownerMembership = workspace.CreateOwnerMembership();

        await workspaceRepository.AddAsync(workspace, ownerMembership, ct);
        await workspaceRepository.SaveChangesAsync(ct);

        var accessToken = jwtService.IssueAccessToken(command.UserId, command.DisplayName, workspace.Id);

        return new CreateWorkspaceResult(
            new WorkspaceSummary(workspace.Id, workspace.Name),
            accessToken.Token,
            accessToken.ExpiresIn);
    }
}
