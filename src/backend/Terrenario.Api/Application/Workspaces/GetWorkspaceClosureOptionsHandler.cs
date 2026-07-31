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

        // El sucesor lo decide **el mismo sitio** que lo aplica: se pregunta al repositorio en vez de
        // repetir aquí el criterio (CA-5). No es solo evitar duplicación —replicar la regla ya hizo
        // que divergiera en MVP-502— sino que reproducirla en memoria es directamente **incorrecto**:
        // el desempate va por identificador, y `Guid.CompareTo` de .NET no ordena igual que el tipo
        // `uuid` de PostgreSQL. La pantalla anunciaba a una persona y el traspaso acababa en otra
        // justo en el caso que el desempate venía a arreglar (MVP-506).
        var successorMember = await workspaceRepository.FindOtherActiveOwnerAsync(
            workspaceId, actingUserId, ct);

        var successor = successorMember is null
            ? null
            : members.FirstOrDefault(m => m.UserId == successorMember.UserId);

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
