using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-206 (HU-6, CA-7/CA-10) — Decisión de quien dio de baja el Workspace sobre una solicitud de
/// traspaso y reactivación. <b>Solo esa persona</b> puede resolverla: para cualquier otra cuenta la
/// solicitud no existe. Al autorizar, el Workspace vuelve (<c>deleted_at</c> a nulo) y la propiedad
/// pasa al solicitante en la misma transacción, de forma que nunca hay un instante sin propietario.
/// </summary>
public sealed class ResolveReactivationHandler(
    IWorkspaceReactivationRequestRepository reactivationRepository,
    IWorkspaceRepository workspaceRepository)
{
    public async Task<ReactivationOutcome> AuthorizeAsync(
        Guid requestId,
        Guid actingUserId,
        CancellationToken ct = default)
    {
        var request = await FindResolvableAsync(requestId, actingUserId, ct);
        var now = DateTimeOffset.UtcNow;

        var workspace = await workspaceRepository.FindIncludingDeletedAsync(request.WorkspaceId, ct)
            ?? throw new WorkspaceMemberException(
                ErrorCodes.WorkspaceNotFound,
                "El Workspace de esta solicitud ya no está disponible.");

        var newOwner = await workspaceRepository.FindActiveMemberAsync(
            workspace.Id, request.RecipientUserId, ct)
            ?? throw new WorkspaceMemberException(
                ErrorCodes.ResourceNotFound,
                "Quien solicitó el traspaso ya no es miembro activo del Workspace.");

        var previousOwner = await workspaceRepository.FindActiveMemberAsync(
            workspace.Id, workspace.OwnerId, ct);

        request.Authorize(actingUserId, now);
        workspace.Reactivate();
        workspace.TransferOwnershipTo(newOwner.UserId);
        newOwner.PromoteToOwner();
        // Quien lo dio de baja conserva su acceso pero deja de ser propietario: la propiedad es del
        // solicitante (CA-7). Si él mismo era el solicitante, la degradación no debe deshacerlo.
        if (previousOwner is not null && previousOwner.UserId != newOwner.UserId)
            previousOwner.DemoteToMember();

        // Los enlaces que se emitieron al resto de miembros dejan de tener sentido: el Workspace ya
        // volvió y con otro propietario. Cerrarlos evita reactivaciones en cadena (CA-10).
        foreach (var pending in await reactivationRepository.ListOpenForWorkspaceAsync(workspace.Id, ct))
        {
            if (pending.Id == request.Id) continue;
            pending.Close(now);
        }

        await reactivationRepository.SaveChangesAsync(ct);

        return new ReactivationOutcome(workspace.Id, workspace.Name, newOwner.UserId);
    }

    public async Task DenyAsync(Guid requestId, Guid actingUserId, CancellationToken ct = default)
    {
        var request = await FindResolvableAsync(requestId, actingUserId, ct);

        request.Deny(actingUserId, DateTimeOffset.UtcNow);
        await reactivationRepository.SaveChangesAsync(ct);
    }

    private async Task<WorkspaceReactivationRequest> FindResolvableAsync(
        Guid requestId,
        Guid actingUserId,
        CancellationToken ct)
    {
        var request = await reactivationRepository.FindByIdAsync(requestId, ct);

        if (request is null || request.AuthorizerUserId != actingUserId)
            throw new WorkspaceMemberException(
                ErrorCodes.ReactivationRequestNotFound,
                "Esta solicitud de reactivación no existe o no puedes resolverla.");

        return request;
    }
}
