namespace Terrenario.Api.Domain.Purchases;

/// <summary>
/// Regla de negocio de compras (MVP-304): la operación es válida en forma, pero el estado del
/// sistema no la admite. Hoy solo se da al intentar dar de baja una compra que todavía tiene
/// imputaciones vivas.
///
/// Se traduce a <c>422 Unprocessable Entity</c> en el borde de transporte, como el resto de
/// <c>BUSINESS_RULE_*</c> del contrato.
/// </summary>
public sealed class PurchaseBusinessRuleException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
