namespace Terrenario.Api.Domain.Consumptions;

/// <summary>
/// Error de validación de un consumo o imputación (MVP-304). Transporta el código de error del
/// contrato de API (<c>docs/02-arquitectura/contratos-api.md</c> §7).
/// </summary>
public sealed class ConsumptionValidationException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
