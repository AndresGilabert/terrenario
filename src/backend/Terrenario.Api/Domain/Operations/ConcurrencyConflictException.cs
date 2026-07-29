namespace Terrenario.Api.Domain.Operations;

/// <summary>
/// Colisión de versión al editar o eliminar un registro operativo (<c>ADR-0005</c>): la versión que
/// el cliente envió en <c>If-Match</c> ya no es la vigente porque otra persona del Workspace tocó el
/// registro entretanto. Se traduce a <c>409 CONFLICT_VERSION_MISMATCH</c> en el borde de transporte.
///
/// Vive en <c>Domain.Operations</c> —y no en el dominio de actividades, que es quien estrena el
/// patrón en MVP-301— porque el contrato publica <b>un único código</b> para todas las entidades
/// operativas críticas: lo reutilizan las compras (MVP-303), las imputaciones y consumos (MVP-304) y
/// lo hará la cosecha (MVP-401).
/// </summary>
public sealed class ConcurrencyConflictException(string message) : Exception(message)
{
    /// <summary>
    /// Versión vigente del registro en servidor. Viaja en la respuesta para que el cliente pueda
    /// resolver el conflicto refrescando el registro en vez de dejar al usuario en un callejón
    /// (MVP-301, CA-4).
    /// </summary>
    public long? CurrentVersion { get; init; }
}
