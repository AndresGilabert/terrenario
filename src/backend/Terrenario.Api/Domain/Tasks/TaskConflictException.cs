namespace Terrenario.Api.Domain.Tasks;

/// <summary>
/// Conflicto del catálogo de tareas (MVP-205): ya existe otra tarea con el mismo nombre en el
/// Workspace, ignorando mayúsculas y espacios sobrantes. El catálogo existe para dar consistencia
/// (RN-026), así que admitir «Poda» dos veces lo vaciaría de sentido; la guarda vive aquí para que la
/// reutilice también el guardado de tarea libre desde la operativa diaria (MVP-302).
///
/// Se traduce a <c>409 Conflict</c> en el borde de transporte.
/// </summary>
public sealed class TaskConflictException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
