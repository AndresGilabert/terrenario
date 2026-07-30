namespace Terrenario.Api.Domain.Harvests;

/// <summary>
/// Regla de forma o de negocio de la cosecha incumplida (MVP-401). Lleva el código del contrato para
/// que el borde de transporte lo devuelva tal cual y el cliente pueda distinguir el motivo, en vez de
/// recibir un genérico.
/// </summary>
public sealed class HarvestValidationException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
