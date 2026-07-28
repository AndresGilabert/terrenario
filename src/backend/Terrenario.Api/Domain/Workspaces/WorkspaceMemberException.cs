namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Error de dominio de la administración de miembros (MVP-204, HU-4/CA-7/CA-8). Transporta el
/// código de error del contrato de API; la traducción a HTTP se hace en el borde de transporte.
/// Cubre la invariante CA-8: no dejar el Workspace sin propietario ni sin ningún miembro activo.
/// </summary>
public sealed class WorkspaceMemberException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
