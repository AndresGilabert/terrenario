using Terrenario.Api.Application.Workers;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-204 (HU-4, CA-7/CA-8) — Retira el acceso de un miembro activo del Workspace. La membresía
/// pasa a <c>revocado</c> (<see cref="WorkspaceMember.Revoke"/>): deja de resolver contexto y de
/// aparecer en el selector, pero no se borra el vínculo ni se invalidan los registros que ese usuario
/// ya hubiera creado (CA-7). Permisos planos (RN-034): cualquier miembro activo puede revocar a otro.
///
/// Invariante CA-8: no se puede dejar el Workspace sin propietario ni sin ningún miembro activo. El
/// reingreso de un revocado se hace por la vía normal de una invitación nueva (MVP-103), no aquí.
///
/// MVP-208 (CA-4) — Retirar el acceso retira también a la persona de los responsables seleccionables:
/// su fila de <c>workers</c> se inactiva. Es la contrapartida exacta de CA-7 en el maestro —no se
/// borra nada, así que los registros que ya la referencian siguen siendo válidos— y la única vía de
/// retirar a un miembro del maestro, porque RN-027 impide inactivarlo a mano.
/// </summary>
public sealed class RevokeMemberHandler(
    IWorkspaceRepository workspaceRepository,
    MemberRosterService memberRoster)
{
    public async Task HandleAsync(Guid workspaceId, Guid targetUserId, CancellationToken ct = default)
    {
        var member = await workspaceRepository.FindActiveMemberAsync(workspaceId, targetUserId, ct);
        if (member is null)
            throw new WorkspaceMemberException(
                ErrorCodes.ResourceNotFound,
                "La persona no es un miembro activo de tu Workspace.");

        // CA-8 — no dejar el Workspace sin propietario. Con la transferencia de propiedad fuera de
        // alcance, el propietario es siempre el único, así que su acceso no se puede retirar.
        if (member.Role == WorkspaceRoles.Owner
            && await workspaceRepository.CountActiveOwnersAsync(workspaceId, ct) <= 1)
            throw new WorkspaceMemberException(
                ErrorCodes.BusinessRuleCannotRevokeOwner,
                "No puedes retirar el acceso al propietario del Workspace.");

        // CA-8 — no dejar el Workspace sin ningún miembro activo.
        if (await workspaceRepository.CountActiveMembersAsync(workspaceId, ct) <= 1)
            throw new WorkspaceMemberException(
                ErrorCodes.BusinessRuleLastActiveMember,
                "No puedes retirar el acceso al último miembro activo del Workspace.");

        member.Revoke();
        await memberRoster.SuspendMemberAsync(workspaceId, targetUserId, ct);

        // Los dos repositorios comparten el DbContext de la petición: membresía y fila de responsable
        // se escriben juntas.
        await workspaceRepository.SaveChangesAsync(ct);
    }
}
