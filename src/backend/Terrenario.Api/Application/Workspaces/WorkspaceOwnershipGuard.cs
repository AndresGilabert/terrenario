using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-206 (HU-3, CA-9) — <b>Regla de no-orfandad</b> de la baja de cuenta: una cuenta que sea
/// propietaria única de uno o más Workspaces no puede darse de baja hasta resolverlos todos, con la
/// misma decisión de HU-3 (traspasar a un miembro activo o dar de baja el Workspace).
///
/// El <b>flujo completo de baja de cuenta</b> (RGPD, borrado/anonimización de datos personales,
/// plazos de retención, revocación de sesiones) queda <b>fuera de alcance</b> de esta historia y se
/// planifica aparte (<c>MVP-999</c>, P-024). Lo que se entrega aquí es la regla que ese flujo tendrá
/// que respetar, ya implementada y verificable: la lista de obligaciones pendientes y la guarda que
/// impide completar la baja mientras quede alguna.
/// </summary>
public sealed class WorkspaceOwnershipGuard(IWorkspaceRepository workspaceRepository)
{
    /// <summary>Workspaces que el usuario debe resolver antes de poder cerrar su cuenta.</summary>
    public async Task<OwnershipObligations> ListObligationsAsync(Guid userId, CancellationToken ct = default)
        => new(await workspaceRepository.ListSoleOwnedAsync(userId, ct));

    /// <summary>
    /// Punto de enganche del futuro flujo de baja de cuenta: lanza mientras quede algún Workspace
    /// sin resolver, de forma que ninguna implementación posterior pueda dejar Workspaces huérfanos.
    /// </summary>
    public async Task EnsureAccountClosureAllowedAsync(Guid userId, CancellationToken ct = default)
    {
        var obligations = await ListObligationsAsync(userId, ct);

        if (!obligations.IsClear)
            throw new WorkspaceMemberException(
                ErrorCodes.BusinessRuleWorkspaceOwnershipUnresolved,
                "Eres la única persona propietaria de algún Workspace: traspásalo o dalo de baja "
                + "antes de cerrar tu cuenta.");
    }
}
