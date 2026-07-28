namespace Terrenario.Api.Domain.Seasons;

/// <summary>
/// Conflicto del maestro de temporadas (MVP-207, CA-2): ya existe otra temporada con el mismo nombre
/// en el Workspace, ignorando mayúsculas y espacios sobrantes. Dos campañas «2025/2026» son
/// indistinguibles en pantalla y en cualquier informe posterior, así que el maestro no las admite.
///
/// La guarda cubre <b>todo</b> el maestro, también las cerradas: cerrar una temporada no libera su
/// nombre, igual que inactivar una tarea no libera el suyo (MVP-205).
///
/// Se traduce a <c>409 Conflict</c> en el borde de transporte.
/// </summary>
public sealed class SeasonConflictException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
