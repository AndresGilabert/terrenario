namespace Terrenario.Api.Domain.Workers;

/// <summary>
/// Regla de negocio del maestro de responsables (MVP-208, CA-4): la operación es válida en forma pero
/// el maestro no es quien la gobierna. Se da en las dos ediciones que un responsable <b>con cuenta</b>
/// no admite: cambiar su nombre —llega de la identidad de Google (RN-036)— e inactivarlo a mano —su
/// disponibilidad la gobierna la membresía, y RN-027 obliga a que todo miembro sea seleccionable—.
///
/// Se traduce a <c>422 Unprocessable Entity</c> en el borde de transporte, como el resto de
/// <c>BUSINESS_RULE_*</c> del contrato.
/// </summary>
public sealed class WorkerBusinessRuleException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
