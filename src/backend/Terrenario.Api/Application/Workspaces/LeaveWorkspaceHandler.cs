using Terrenario.Api.Application.Workers;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-807 (HU-1, <c>P-048</c>) — <b>Abandonar un Workspace</b> por voluntad propia.
///
/// Era el hueco simétrico del ciclo de vida de la membresía: `MVP-204` cubre retirar el acceso <b>a
/// otra</b> persona y la pantalla oculta la acción sobre uno mismo; `MVP-206` cubre la salida <b>del
/// propietario</b>, con traspaso o baja. Un miembro corriente que ya no colabora no tenía ninguna vía
/// —ni de API ni de UI— y arrastraba ese Workspace en su selector indefinidamente; desde `MVP-208`,
/// además, seguía apareciendo como responsable seleccionable dentro de él.
///
/// Con `RN-035` —invitaciones por email y por enlace compartible— entrar en un Workspace ajeno es
/// fácil. Salir no existía.
///
/// <b>Ninguna de las dos guardas se reimplementa.</b> La de no-orfandad la resuelve
/// <see cref="WorkspaceOwnershipGuard"/>, igual que en la baja de cuenta de `MVP-505`; la de no dejar
/// el Workspace sin nadie es la misma comprobación del `CA-8` de `MVP-204`. Es la condición con la que
/// se registró `P-024`, y `CA-2` la comprueba en vez de confiarla.
///
/// El efecto sobre la membresía es <b>exactamente el mismo que revocar</b>: pasa a <c>revocado</c>, la
/// fila de responsable se inactiva y el histórico no se toca. Reingresar exige invitación nueva
/// (`MVP-103`), igual que para quien fue revocado; no hay readmisión automática.
/// </summary>
public sealed class LeaveWorkspaceHandler(
    IWorkspaceRepository workspaceRepository,
    WorkspaceOwnershipGuard ownershipGuard,
    MemberRosterService memberRoster)
{
    public async Task HandleAsync(Guid workspaceId, Guid userId, CancellationToken ct = default)
    {
        var member = await workspaceRepository.FindActiveMemberAsync(workspaceId, userId, ct);
        if (member is null)
            throw new WorkspaceMemberException(
                ErrorCodes.ResourceNotFound,
                "No eres un miembro activo de este Workspace.");

        // CA-2 — la guarda de no-orfandad, llamada tal cual. Un propietario único tiene que traspasar
        // o dar de baja el Workspace antes de irse, exactamente igual que para cerrar su cuenta.
        await ownershipGuard.EnsureCanLeaveAsync(userId, workspaceId, ct);

        // CA-3 — y tampoco se puede dejar el Workspace sin ningún miembro activo. Con la guarda
        // anterior este caso solo se alcanza sin ser propietario, pero se comprueba igual: la regla es
        // «no dejarlo vacío», no «no dejarlo sin propietario».
        if (await workspaceRepository.CountActiveMembersAsync(workspaceId, ct) <= 1)
            throw new WorkspaceMemberException(
                ErrorCodes.BusinessRuleLastActiveMember,
                "Eres la única persona activa de este Workspace: no puedes abandonarlo y dejarlo sin "
                + "nadie. Dalo de baja desde Ajustes si ya no lo necesitas.");

        member.Revoke();
        // CA-4 (MVP-208) — deja de ofrecerse como responsable seleccionable, sin borrar el histórico
        // que ya tenga: las labores que tenía asignadas siguen mostrando su nombre.
        await memberRoster.SuspendMemberAsync(workspaceId, userId, ct);

        // Los dos repositorios comparten el DbContext de la petición: membresía y fila de responsable
        // se escriben juntas.
        await workspaceRepository.SaveChangesAsync(ct);
    }
}
