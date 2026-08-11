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

    /// <summary>
    /// MVP-807 (HU-1, CA-2) — Misma regla, un solo Workspace: quien es su **propietario único** no
    /// puede abandonarlo sin resolver antes la propiedad.
    ///
    /// <b>Vive aquí y no en el caso de uso a propósito.</b> Es la condición con la que se registró
    /// <c>P-024</c> y la que <c>MVP-505</c> respetó al construir la baja de cuenta: la no-orfandad se
    /// llama, no se reimplementa. Abandonar y cerrar la cuenta son dos puertas al mismo problema —una
    /// persona que se va— y si cada una decidiera por su cuenta acabarían discrepando, que es
    /// exactamente lo que le pasó a <c>can_revoke</c> con su propia guarda (<c>P-049</c>).
    ///
    /// Reutiliza <see cref="ListObligationsAsync"/>, así que la definición de «propietario único» es
    /// literalmente la misma consulta.
    /// </summary>
    public async Task EnsureCanLeaveAsync(Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var obligations = await ListObligationsAsync(userId, ct);

        if (obligations.Workspaces.Any(workspace => workspace.WorkspaceId == workspaceId))
            throw new WorkspaceMemberException(
                ErrorCodes.BusinessRuleWorkspaceOwnershipUnresolved,
                "Eres la única persona propietaria de este Workspace: traspásalo o dalo de baja "
                + "antes de abandonarlo.");
    }
}
