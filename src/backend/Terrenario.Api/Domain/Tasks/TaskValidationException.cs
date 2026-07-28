namespace Terrenario.Api.Domain.Tasks;

/// <summary>
/// Error de validación del catálogo de tareas (MVP-205). Transporta el código de error del contrato
/// de API (<c>docs/02-arquitectura/contratos-api.md</c>); la traducción a HTTP se hace en el borde de
/// transporte, no en el dominio.
/// </summary>
public sealed class TaskValidationException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
