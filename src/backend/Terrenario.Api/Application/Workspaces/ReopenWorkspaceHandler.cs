using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-206 — Contrapartida de que la baja sea **lógica y reversible** (CA-2): quien dio de baja un
/// Workspace puede volver a levantarlo por su cuenta, sin que nadie se lo solicite. Es la única vía
/// posible cuando el Workspace no tenía más miembros a los que notificar (spec, árbol de decisión,
/// punto 2), y la más directa cuando sí los tenía.
///
/// No confundir con la autorización de una solicitud ajena
/// (<see cref="ResolveReactivationHandler"/>): allí el Workspace cambia de propietario; aquí vuelve
/// tal como estaba, con quien lo dio de baja al frente.
/// </summary>
public sealed class ReopenWorkspaceHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceReactivationRequestRepository reactivationRepository)
{
    public Task<IReadOnlyList<Workspace>> ListClosedAsync(Guid actingUserId, CancellationToken ct = default)
        => workspaceRepository.ListClosedByAsync(actingUserId, ct);

    public async Task<WorkspaceSummary> HandleAsync(
        Guid workspaceId,
        Guid actingUserId,
        CancellationToken ct = default)
    {
        var workspace = await workspaceRepository.FindIncludingDeletedAsync(workspaceId, ct);

        // Solo quien lo dio de baja lo ve y lo levanta: para cualquier otra cuenta no existe (CA-10).
        if (workspace is null || !workspace.IsDeleted || workspace.DeletedByUserId != actingUserId)
            throw new WorkspaceMemberException(
                ErrorCodes.WorkspaceNotFound,
                "Este Workspace no existe o no lo diste de baja tú.");

        var member = await workspaceRepository.FindActiveMemberAsync(workspaceId, actingUserId, ct)
            ?? throw new WorkspaceMemberException(
                ErrorCodes.ResourceNotFound,
                "Ya no eres miembro activo de este Workspace.");

        workspace.Reactivate();
        // El Workspace vuelve con su propiedad intacta: quien lo levanta era ya su propietario. Se
        // reafirma por si `owner_id` hubiera quedado apuntando a alguien que ya no tiene acceso.
        member.PromoteToOwner();
        if (workspace.OwnerId != actingUserId) workspace.TransferOwnershipTo(actingUserId);

        var now = DateTimeOffset.UtcNow;
        foreach (var pending in await reactivationRepository.ListOpenForWorkspaceAsync(workspaceId, ct))
            pending.Close(now);

        await workspaceRepository.SaveChangesAsync(ct);

        return new WorkspaceSummary(workspace.Id, workspace.Name);
    }
}
