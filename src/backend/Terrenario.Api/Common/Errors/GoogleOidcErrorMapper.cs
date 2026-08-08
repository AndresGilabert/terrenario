using Microsoft.AspNetCore.Http;

namespace Terrenario.Api.Common.Errors;

/// <summary>
/// MVP-713 (`P-079`) — Traduce el código de error del intercambio con Google al código HTTP de
/// <c>docs/02-arquitectura/contratos-api.md</c>. Mismo patrón que
/// <see cref="InvitationErrorMapper"/>: el servicio de identidad no conoce HTTP, la correspondencia
/// vive en el borde de transporte.
///
/// Existe como tabla y no como dos <c>catch … when</c> en el controlador porque la clasificación
/// —cliente frente a servidor— es lo que decide si una respuesta entra en el numerador del SLO de tasa
/// de error. Repartida en cláusulas de captura, añadir un código nuevo y olvidarse de clasificarlo
/// dejaba la excepción sin capturar y, por tanto, contada como <c>500</c> sin que nadie lo decidiera.
/// </summary>
public static class GoogleOidcErrorMapper
{
    /// <summary>
    /// Todo lo que no esté clasificado explícitamente como error de quien llama es <c>500</c>. El
    /// defecto va en esa dirección a propósito: un fallo propio contado como error de cliente
    /// desaparece de las alertas, que es peor que lo contrario.
    /// </summary>
    public static int StatusFor(string errorCode) => errorCode switch
    {
        // El código ya se usó o caducó, y el `id_token` que no valida: en los dos casos la credencial
        // presentada no sirve, que es exactamente lo que significa un 401.
        ErrorCodes.AuthGoogleCodeInvalid or ErrorCodes.AuthGoogleTokenInvalid
            => StatusCodes.Status401Unauthorized,
        ErrorCodes.AuthGoogleRequestInvalid => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError
    };

    public static ApiError ToApiError(string errorCode) => errorCode switch
    {
        ErrorCodes.AuthGoogleCodeInvalid => ApiError.GoogleCodeInvalid(),
        ErrorCodes.AuthGoogleTokenInvalid => ApiError.GoogleTokenInvalid(),
        ErrorCodes.AuthGoogleRequestInvalid => ApiError.GoogleRequestInvalid(),
        _ => ApiError.GoogleExchangeFailed()
    };

    /// <summary>Si la respuesta cuenta como fallo del servidor en el SLO de tasa de error (5xx).</summary>
    public static bool IsServerFault(string errorCode)
        => StatusFor(errorCode) >= StatusCodes.Status500InternalServerError;
}
