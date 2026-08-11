using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Masters;

namespace Terrenario.Api.Application.Masters;

/// <summary>
/// MVP-806 (HU-1, CA-1/CA-2) — Borrado <b>físico</b> de una fila de maestro que nunca se usó.
///
/// La política de la épica MVP-002 no cambia para lo que sí tiene histórico: eso se inactiva
/// (`RN-037`). Lo que faltaba era el caso trivial —una ficha creada por error y jamás referenciada—,
/// que hasta ahora se quedaba para siempre en la lista de inactivos.
///
/// El orden importa: primero se comprueba el uso contra <b>todas</b> las referencias declaradas y solo
/// después se borra. Si hay uso, la respuesta dice <b>cuántos</b> registros lo referencian y de qué
/// tipo son, porque un «no se puede» sin cifra deja al usuario sin saber dónde mirar.
/// </summary>
public sealed class DeleteMasterHandler(IMasterRepository masterRepository)
{
    /// <returns>La ficha borrada, o <c>null</c> si no existe en el Workspace activo (404).</returns>
    /// <exception cref="MasterOperationException">La ficha tiene histórico o no es borrable (422).</exception>
    public async Task<MasterRecord?> HandleAsync(
        MasterKind kind, Guid workspaceId, Guid masterId, CancellationToken ct = default)
    {
        var record = await masterRepository.FindAsync(kind, workspaceId, masterId, ct);
        if (record is null) return null;

        // MVP-208 (CA-4) — La fila de un responsable con cuenta la gobierna su membresía, no el
        // maestro: si desapareciera, el miembro dejaría de tener ficha en un Workspace al que sigue
        // teniendo acceso. La vía de retirarlo es revocar el acceso, igual que para inactivarlo.
        if (record.IsIdentityManaged)
            throw new MasterOperationException(
                ErrorCodes.BusinessRuleWorkerMembershipManaged,
                "La ficha de un miembro del Workspace no se elimina desde el maestro: " +
                "depende de su acceso.");

        var usage = await masterRepository.CountUsageAsync(kind, workspaceId, masterId, ct);
        if (usage.IsUsed)
            throw new MasterOperationException(
                ErrorCodes.BusinessRuleMasterInUse,
                $"No se puede eliminar {MasterKinds.Article(kind)} {MasterKinds.Singular(kind)} " +
                $"«{record.Name}»: {usage.Describe()} {MasterKinds.ObjectPronoun(kind)} " +
                $"{(usage.Total == 1 ? "referencia" : "referencian")}.");

        await masterRepository.DeleteAsync(kind, workspaceId, masterId, ct);

        return record;
    }
}
