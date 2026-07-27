namespace Terrenario.Api.Domain.Plots;

/// <summary>
/// Error de validación del agregado <see cref="Plot"/>. Lleva el código de error del contrato para
/// que el borde de transporte lo traduzca a una respuesta 400 uniforme.
/// </summary>
public sealed class PlotValidationException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
