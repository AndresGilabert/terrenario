namespace Terrenario.Api.Domain.Activities;

/// <summary>
/// Error de validación de una actividad (MVP-301). Transporta el código de error del contrato de API
/// (<c>docs/02-arquitectura/contratos-api.md</c> §5); la traducción a HTTP se hace en el borde de
/// transporte, no en el dominio.
/// </summary>
public sealed class ActivityValidationException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
