using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-206 (HU-1, CA-1) — Renombra el Workspace activo. Permisos planos (RN-034): lo puede hacer
/// cualquier miembro activo, igual que invitar o retirar accesos (MVP-204). No reemite la sesión: el
/// nombre no viaja en el token, así que el cambio se ve en el selector y en la cabecera sin
/// recrearla; el cliente solo tiene que refrescar su contexto.
/// </summary>
public sealed class RenameWorkspaceHandler(IWorkspaceRepository workspaceRepository)
{
    public async Task<WorkspaceSummary> HandleAsync(
        Guid workspaceId,
        string name,
        CancellationToken ct = default)
    {
        var workspace = await workspaceRepository.FindByIdAsync(workspaceId, ct)
            ?? throw new WorkspaceMemberException(
                ErrorCodes.WorkspaceNotFound,
                "El Workspace ya no está disponible.");

        workspace.Rename(name);
        await workspaceRepository.SaveChangesAsync(ct);

        return new WorkspaceSummary(workspace.Id, workspace.Name);
    }
}
