namespace Terrenario.Api.Domain.Workers;

/// <summary>
/// Conflicto del maestro de trabajadores (MVP-207, CA-2): ya existe otro trabajador con el mismo
/// nombre en el Workspace, ignorando mayúsculas y espacios sobrantes. El maestro existe justamente
/// «para evitar nombres duplicados o inconsistentes» (MVP-204, HU-1), y dos filas «Juan Pérez» no se
/// pueden distinguir al imputar una jornada.
///
/// La guarda cubre <b>todo</b> el maestro, también los inactivos: inactivar no libera el nombre.
///
/// Se traduce a <c>409 Conflict</c> en el borde de transporte.
/// </summary>
public sealed class WorkerConflictException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
