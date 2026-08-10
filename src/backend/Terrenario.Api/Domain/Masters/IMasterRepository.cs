namespace Terrenario.Api.Domain.Masters;

/// <summary>
/// Depuración de maestros (MVP-806): comprobar el uso, borrar lo nunca usado y fusionar dos filas.
///
/// Es <b>un</b> puerto para los cuatro maestros y no cuatro puertos paralelos por la razón que da el
/// spec: la parte delicada no es el borrado, es la comprobación del «sin uso» contra todas las
/// entidades que pueden referenciar al registro. Repartirla en cuatro repositorios garantizaría que
/// tarde o temprano una de las cuatro copias se quedara sin actualizar al aparecer una entidad
/// operativa nueva.
/// </summary>
public interface IMasterRepository
{
    /// <summary>
    /// La fila de maestro dentro del Workspace activo, con lo que la depuración necesita saber de
    /// ella. <c>null</c> si no existe o es de otro Workspace (aislamiento multi-tenant).
    /// </summary>
    Task<MasterRecord?> FindAsync(
        MasterKind kind, Guid workspaceId, Guid masterId, CancellationToken ct = default);

    /// <summary>
    /// Cuántos registros referencian a esta fila, desglosado por tipo (CA-2). Cuenta también los
    /// registros <b>eliminados lógicamente</b>: su clave ajena sigue apuntando aquí, así que un
    /// borrado físico los dejaría huérfanos —y de hecho lo impediría la propia FK <c>RESTRICT</c>—.
    /// </summary>
    Task<MasterUsage> CountUsageAsync(
        MasterKind kind, Guid workspaceId, Guid masterId, CancellationToken ct = default);

    /// <summary>
    /// Recuento de uso de <b>todas</b> las filas del maestro en el Workspace, en una sola pasada por
    /// tipo de referencia. Lo consume el listado: la UI no puede ofrecer «Eliminar» en una fila usada
    /// (CA-2) y preguntarlo fila a fila sería una consulta por registro.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> CountUsageByWorkspaceAsync(
        MasterKind kind, Guid workspaceId, CancellationToken ct = default);

    /// <summary>
    /// Borrado <b>físico</b> de la fila (CA-1). La comprobación de uso la hace el caso de uso antes;
    /// aquí la red por debajo es la propia FK <c>RESTRICT</c>, que se traduce a la misma
    /// <see cref="MasterOperationException"/> si alguien registra algo entre la comprobación y el
    /// borrado.
    /// </summary>
    Task DeleteAsync(MasterKind kind, Guid workspaceId, Guid masterId, CancellationToken ct = default);

    /// <summary>
    /// Fusiona dos filas del mismo maestro: reapunta al superviviente todo lo que referenciaba al
    /// absorbido y borra el absorbido, <b>en una sola transacción</b> (CA-3/CA-5).
    ///
    /// Los registros operativos se reapuntan por el agregado, no con un <c>UPDATE</c> masivo, para que
    /// su token de concurrencia entre en juego (ADR-0005): si alguien está editando uno de ellos, la
    /// fusión falla con <see cref="Operations.ConcurrencyConflictException"/> en vez de pisar el cambio.
    /// </summary>
    /// <returns>Cuántos registros operativos se reapuntaron.</returns>
    Task<int> MergeAsync(
        MasterKind kind,
        Guid workspaceId,
        Guid survivorId,
        Guid absorbedId,
        Guid userId,
        CancellationToken ct = default);
}
