using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces;

public sealed class ActiveWorkspaceResolver(
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository) : IActiveWorkspaceResolver
{
    public async Task<WorkspaceSummary?> ResolveAsync(
        Guid userId,
        Guid? preferredWorkspaceId = null,
        CancellationToken ct = default)
    {
        var workspace = await FindIfMemberAsync(preferredWorkspaceId, userId, ct);

        // El claim no viaja en el login ni en el refresh: sin esta preferencia persistida, la
        // sesión renovada volvería al Workspace por defecto y se perdería el cambio (CA-3).
        if (workspace is null)
        {
            var user = await userRepository.FindByIdAsync(userId, ct);
            workspace = await FindIfMemberAsync(user?.ActiveWorkspaceId, userId, ct);
        }

        workspace ??= await workspaceRepository.FindDefaultForUserAsync(userId, ct);

        return workspace is null ? null : new WorkspaceSummary(workspace.Id, workspace.Name);
    }

    private Task<Workspace?> FindIfMemberAsync(Guid? workspaceId, Guid userId, CancellationToken ct)
        => workspaceId.HasValue
            ? workspaceRepository.FindForMemberAsync(workspaceId.Value, userId, ct)
            : Task.FromResult<Workspace?>(null);
}
