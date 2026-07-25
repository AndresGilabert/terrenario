using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-104 — Devuelve los Workspaces a los que el usuario puede alternar y cuál está activo
/// ahora mismo (CA-1).
/// </summary>
public sealed class ListUserWorkspacesHandler(
    IWorkspaceRepository workspaceRepository,
    IActiveWorkspaceResolver activeWorkspaceResolver)
{
    public async Task<ListUserWorkspacesResult> HandleAsync(
        ListUserWorkspacesQuery query,
        CancellationToken ct = default)
    {
        var memberships = await workspaceRepository.ListActiveMembershipsAsync(query.UserId, ct);

        // El activo se resuelve con las mismas reglas que el resto de la API para que el
        // selector no marque un Workspace distinto del que ejecuta las operaciones.
        var activeWorkspace = await activeWorkspaceResolver.ResolveAsync(
            query.UserId,
            query.SessionWorkspaceId,
            ct);

        var workspaces = memberships
            .Select(membership => new UserWorkspaceItem(
                membership.WorkspaceId,
                membership.Name,
                membership.Role,
                membership.Status,
                membership.JoinedAt))
            .ToList();

        return new ListUserWorkspacesResult(workspaces, activeWorkspace?.Id);
    }
}
