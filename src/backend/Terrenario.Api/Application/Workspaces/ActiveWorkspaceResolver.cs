using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces;

public sealed class ActiveWorkspaceResolver(IWorkspaceRepository workspaceRepository) : IActiveWorkspaceResolver
{
    public async Task<WorkspaceSummary?> ResolveAsync(
        Guid userId,
        Guid? preferredWorkspaceId = null,
        CancellationToken ct = default)
    {
        var workspace = preferredWorkspaceId.HasValue
            ? await workspaceRepository.FindForMemberAsync(preferredWorkspaceId.Value, userId, ct)
            : null;

        workspace ??= await workspaceRepository.FindDefaultForUserAsync(userId, ct);

        return workspace is null ? null : new WorkspaceSummary(workspace.Id, workspace.Name);
    }
}
