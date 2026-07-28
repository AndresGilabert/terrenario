using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-206 (HU-3, CA-3/CA-4) — Traspaso explícito de la propiedad: la alternativa a dar de baja que
/// se ofrece al propietario único. Elige a qué <b>miembro activo</b> se la otorga; esa persona pasa a
/// <c>workspace_owner</c> y el Workspace <b>sigue vivo</b>.
///
/// Decisión de producto: quien traspasa <b>se queda como miembro normal</b>. Ceder la propiedad no
/// es irse: para salir del Workspace está la retirada de acceso (MVP-204). La salida sí es
/// automática cuando la baja se resuelve por reasignación entre copropietarios (CA-5).
/// </summary>
public sealed class TransferWorkspaceOwnershipHandler(IWorkspaceRepository workspaceRepository)
{
    public async Task<WorkspaceClosureResult> HandleAsync(
        TransferOwnershipCommand command,
        CancellationToken ct = default)
    {
        var workspace = await workspaceRepository.FindByIdAsync(command.WorkspaceId, ct)
            ?? throw new WorkspaceMemberException(
                ErrorCodes.WorkspaceNotFound,
                "El Workspace ya no está disponible.");

        var actingMember = await workspaceRepository.FindActiveMemberAsync(
            workspace.Id, command.ActingUserId, ct)
            ?? throw new WorkspaceMemberException(
                ErrorCodes.ResourceNotFound,
                "No eres miembro activo de este Workspace.");

        if (actingMember.Role != WorkspaceRoles.Owner)
            throw new WorkspaceMemberException(
                ErrorCodes.AuthWorkspaceOwnerRequired,
                "Solo el propietario del Workspace puede traspasar la propiedad.");

        // Solo se traspasa a alguien que ya está dentro y activo: así no se crea acceso nuevo por
        // esta vía (el alta de personas sigue siendo la invitación de MVP-103).
        var newOwner = await workspaceRepository.FindActiveMemberAsync(
            workspace.Id, command.NewOwnerUserId, ct)
            ?? throw new WorkspaceMemberException(
                ErrorCodes.ResourceNotFound,
                "Esa persona no es un miembro activo de tu Workspace.");

        // El agregado rechaza el traspaso a quien ya es propietario; se comprueba antes también
        // sobre la membresía para cubrir a un copropietario que no figure como owner_id.
        if (newOwner.UserId == actingMember.UserId)
            throw new WorkspaceMemberException(
                ErrorCodes.BusinessRuleOwnershipTransferToSelf,
                "Ya eres la persona propietaria del Workspace.");

        workspace.TransferOwnershipTo(newOwner.UserId);
        newOwner.PromoteToOwner();
        actingMember.DemoteToMember();

        await workspaceRepository.SaveChangesAsync(ct);

        var newOwnerName = (await workspaceRepository.ListMembersAsync(workspace.Id, ct))
            .FirstOrDefault(m => m.UserId == newOwner.UserId)?.DisplayName;

        return new WorkspaceClosureResult(
            WorkspaceClosureOutcomes.Transferred,
            workspace.Id,
            workspace.Name,
            newOwnerName,
            NotifiedMembers: 0,
            EmailsSent: 0);
    }
}
