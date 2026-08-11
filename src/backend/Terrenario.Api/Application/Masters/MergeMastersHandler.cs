using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Masters;

namespace Terrenario.Api.Application.Masters;

/// <summary>
/// MVP-806 (HU-2, CA-3/CA-4/CA-5) — Fusión de dos fichas del mismo maestro: la absorbida cede sus
/// referencias a la superviviente y desaparece.
///
/// «La fusión no es un borrado con pasos previos» (spec): lo que la separa de un <c>DELETE</c> es que
/// las claves ajenas se reapuntan dentro de una transacción y con el mismo control de concurrencia que
/// usan las entidades operativas. Eso vive en <see cref="IMasterRepository.MergeAsync"/>; aquí queda
/// lo que decide <b>si</b> la fusión es legítima.
/// </summary>
public sealed class MergeMastersHandler(IMasterRepository masterRepository)
{
    /// <returns>El resultado, o <c>null</c> si la ficha superviviente no existe en el Workspace (404).</returns>
    public async Task<MasterMergeResult?> HandleAsync(
        MasterKind kind,
        Guid workspaceId,
        Guid userId,
        Guid survivorId,
        Guid absorbedId,
        CancellationToken ct = default)
    {
        var survivor = await masterRepository.FindAsync(kind, workspaceId, survivorId, ct);
        if (survivor is null) return null;

        if (survivorId == absorbedId)
            throw new MasterOperationException(
                ErrorCodes.BusinessRuleMasterMergeSelf,
                "Una ficha no se puede fusionar consigo misma: elige otra.");

        var absorbed = await masterRepository.FindAsync(kind, workspaceId, absorbedId, ct);
        if (absorbed is null)
            // 400 y no 404, siguiendo el criterio de `FOREIGN_KEY_WORKSPACE_MISMATCH` del contrato: lo
            // que no existe llega en el cuerpo, no en la ruta.
            throw new MasterLinkException(
                $"La ficha de {MasterKinds.Singular(kind)} que quieres fusionar no existe en tu " +
                "Workspace activo.");

        // RN-036 · MVP-208 (CA-1/CA-4) — Al fusionar una fila de cuadrilla con la de un miembro,
        // sobrevive la del miembro: su nombre lo fija su cuenta de Google y no es renombrable, y
        // borrarla dejaría a alguien con acceso sin ficha de responsable, contra el índice único
        // parcial `ux_workers_workspace_user_account`, que existe justo para que cada cuenta tenga la
        // suya. La regla se enuncia sobre el absorbido —«el absorbido nunca puede ser un miembro»—
        // porque así cubre también el caso de dos cuentas homónimas, donde no hay ninguna cuadrilla y
        // fusionar seguiría borrando la ficha de una persona real.
        if (absorbed.IsIdentityManaged)
            throw new MasterOperationException(
                ErrorCodes.BusinessRuleMasterMergeMemberSurvives,
                $"«{absorbed.Name}» es un miembro del Workspace y su ficha no puede desaparecer en una " +
                "fusión: su nombre lo fija su cuenta. Fusiona en sentido contrario, conservando la suya.");

        var reassigned = await masterRepository.MergeAsync(
            kind, workspaceId, survivorId, absorbedId, userId, ct);

        return new MasterMergeResult(survivor, absorbed, reassigned);
    }
}

/// <summary>Qué sobrevivió, qué desapareció y cuántos registros cambiaron de ficha.</summary>
public sealed record MasterMergeResult(MasterRecord Survivor, MasterRecord Absorbed, int ReassignedCount);

/// <summary>
/// La ficha que llega en el cuerpo de la petición no existe en el Workspace activo. Se traduce a
/// <c>400 FOREIGN_KEY_WORKSPACE_MISMATCH</c>, el mismo código con el que el contrato responde a un
/// vínculo inexistente en las entidades operativas.
/// </summary>
public sealed class MasterLinkException(string message) : Exception(message);
