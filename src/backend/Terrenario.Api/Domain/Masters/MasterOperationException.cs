namespace Terrenario.Api.Domain.Masters;

/// <summary>
/// Una regla de negocio impide depurar el maestro (MVP-806). Se traduce a <c>422</c> con el código que
/// trae, igual que el resto de <c>BUSINESS_RULE_*</c> del contrato: la petición está bien formada,
/// pero el estado del maestro no admite la operación.
/// </summary>
public sealed class MasterOperationException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
