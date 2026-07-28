using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-206 — Resuelve en servidor el árbol de decisión de la baja del Workspace activo (spec, §Árbol
/// de decisión) para que la UI no lo reimplemente y pueda plantear la pregunta correcta: reasignar y
/// salir (CA-5), elegir entre traspasar o dar de baja (CA-3/CA-4) o solo dar de baja.
/// </summary>
public sealed class GetWorkspaceClosureOptionsHandler(IWorkspaceRepository workspaceRepository)
{
    public async Task<WorkspaceClosureOptions> HandleAsync(
        Guid workspaceId,
        Guid actingUserId,
        CancellationToken ct = default)
    {
        var workspace = await workspaceRepository.FindByIdAsync(workspaceId, ct)
            ?? throw new WorkspaceMemberException(
                ErrorCodes.WorkspaceNotFound,
                "El Workspace ya no está disponible.");

        var members = await workspaceRepository.ListMembersAsync(workspaceId, ct);
        var actingMember = members.FirstOrDefault(m =>
            m.UserId == actingUserId && m.Status == WorkspaceMemberStatuses.Active);

        var candidates = members
            .Where(m => m.Status == WorkspaceMemberStatuses.Active && m.UserId != actingUserId)
            .Select(m => new OwnershipCandidate(m.UserId, m.DisplayName, m.Email, m.Role))
            .ToList();

        var activeOwners = members.Count(m =>
            m.Status == WorkspaceMemberStatuses.Active && m.Role == WorkspaceRoles.Owner);

        var isOwner = actingMember?.Role == WorkspaceRoles.Owner;

        // Mismo criterio que el traspaso automático del repositorio (copropietario activo más
        // antiguo): así el nombre que anuncia la confirmación es el del sucesor real (CA-5).
        var successor = members
            .Where(m => m.Status == WorkspaceMemberStatuses.Active
                && m.UserId != actingUserId
                && m.Role == WorkspaceRoles.Owner)
            .OrderBy(m => m.JoinedAt)
            .FirstOrDefault();

        var mode = (isOwner, successor) switch
        {
            (false, _) => WorkspaceClosureModes.NotOwner,
            (true, not null) => WorkspaceClosureModes.AutoTransfer,
            _ when candidates.Count > 0 => WorkspaceClosureModes.Choose,
            _ => WorkspaceClosureModes.OnlyDelete
        };

        return new WorkspaceClosureOptions(
            workspace.Id,
            workspace.Name,
            isOwner,
            mode,
            activeOwners,
            mode == WorkspaceClosureModes.AutoTransfer ? successor!.DisplayName : null,
            candidates);
    }
}
