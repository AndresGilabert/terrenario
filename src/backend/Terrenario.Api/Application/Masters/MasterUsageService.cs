using Terrenario.Api.Domain.Masters;

namespace Terrenario.Api.Application.Masters;

/// <summary>
/// MVP-806 (CA-2) — Cuántos registros referencian a cada ficha de un maestro, para que el listado
/// pueda decidir a quién ofrecer «Eliminar».
///
/// El recuento viaja en el listado y no en un endpoint aparte por dos motivos. El primero es que la
/// decisión de la UI —ofrecer o no la acción— se toma al pintar la lista, y pedirlo ficha a ficha
/// serían tantas idas al servidor como filas. El segundo es que ese mismo número es el que la
/// confirmación de la fusión necesita para decir cuántos registros se van a reapuntar.
///
/// Es una **pista para la interfaz**, no la guarda: la comprobación que manda la hace el servidor al
/// recibir el borrado (<see cref="DeleteMasterHandler"/>), y por debajo están las claves ajenas.
/// </summary>
public sealed class MasterUsageService(IMasterRepository masterRepository)
{
    public Task<IReadOnlyDictionary<Guid, int>> CountByWorkspaceAsync(
        MasterKind kind, Guid workspaceId, CancellationToken ct = default)
        => masterRepository.CountUsageByWorkspaceAsync(kind, workspaceId, ct);
}
