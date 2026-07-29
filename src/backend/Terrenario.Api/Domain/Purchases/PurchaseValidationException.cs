namespace Terrenario.Api.Domain.Purchases;

/// <summary>
/// Error de validación de una compra (MVP-303). Transporta el código de error del contrato de API
/// (<c>docs/02-arquitectura/contratos-api.md</c> §7); la traducción a HTTP se hace en el borde de
/// transporte, no en el dominio.
/// </summary>
public sealed class PurchaseValidationException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
