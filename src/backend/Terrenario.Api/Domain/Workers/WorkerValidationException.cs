namespace Terrenario.Api.Domain.Workers;

/// <summary>
/// Error de validación del maestro de trabajadores (MVP-204). Transporta el código de error del
/// contrato de API (<c>docs/02-arquitectura/contratos-api.md</c>); la traducción a HTTP se hace en
/// el borde de transporte, no en el dominio.
/// </summary>
public sealed class WorkerValidationException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
