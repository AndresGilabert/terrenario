using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Terrenario.Api.Common.Errors;

/// <summary>
/// MVP-502 (<c>P-043</c>) — Traduce el <c>ModelState</c> de ASP.NET al contrato de error de la API
/// (<c>docs/02-arquitectura/contratos-api.md</c>).
///
/// Antes esto era una lambda de tres líneas en <c>Program.cs</c> que devolvía siempre
/// <c>VALIDATION_REQUIRED</c> con el primer mensaje que encontrara. Eso producía dos defectos:
///
/// <list type="number">
/// <item>Un cliente no podía distinguir <b>«falta»</b> de <b>«demasiado largo»</b> en el alta, aunque
/// el mismo caso en <c>PATCH</c> sí devolviera el código de dominio.</item>
/// <item>Cuando el fallo lo generaba el propio framework —una fecha con formato inválido, un número
/// donde se esperaba otro tipo— el mensaje salía <b>en inglés</b> («The request field is required.»)
/// y la UI lo mostraba tal cual al usuario.</item>
/// </list>
///
/// Ahora hay tres casos, en este orden:
///
/// <list type="number">
/// <item>El error viene de una anotación <b>nuestra</b> (<see cref="RequiredFieldAttribute"/>,
/// <see cref="MaxTextLengthAttribute"/>): trae su código dentro y se usa tal cual.</item>
/// <item>El error lo generó el <b>binder</b> (hay excepción asociada, o el mensaje es el texto por
/// defecto de ASP.NET): el valor llegó pero no se puede interpretar ⇒
/// <see cref="ErrorCodes.ValidationFormatInvalid"/> y mensaje propio en español, nombrando el campo
/// tal y como el cliente lo envió.</item>
/// <item>Cualquier otro mensaje propio sin código: <see cref="ErrorCodes.ValidationRequired"/>, que
/// es el comportamiento anterior y sigue siendo el correcto para «falta un dato».</item>
/// </list>
/// </summary>
public static class ModelStateErrorTranslator
{
    public static ApiError Translate(ModelStateDictionary modelState)
    {
        foreach (var (key, entry) in modelState)
        {
            foreach (var error in entry.Errors)
            {
                // Caso 1 — anotación propia: el código viaja dentro del mensaje.
                if (ApiValidationMessage.TryDecode(error.ErrorMessage, out var code, out var message))
                    return ApiError.Validation(code, message);

                // Caso 2 — el binder no supo interpretar el valor. `Exception` lo delata cuando
                // ASP.NET no deja mensaje; cuando lo deja, viene en inglés y no debe llegar al
                // usuario.
                if (error.Exception is not null || IsFrameworkMessage(error.ErrorMessage))
                    return ApiError.Validation(
                        ErrorCodes.ValidationFormatInvalid,
                        FormatInvalidMessage(key));
            }
        }

        // Caso 3 — mensaje propio sin código todavía.
        var first = modelState
            .SelectMany(entry => entry.Value?.Errors ?? [])
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));

        return ApiError.Validation(
            ErrorCodes.ValidationRequired,
            first ?? "Datos de entrada no válidos.");
    }

    /// <summary>
    /// Los mensajes que genera ASP.NET cuando no se le da uno propio. Se detectan por su forma —son
    /// plantillas fijas— para no dejarlos salir en inglés hacia la UI.
    /// </summary>
    private static bool IsFrameworkMessage(string? message)
        => !string.IsNullOrEmpty(message)
           && (message.Contains("field is required", StringComparison.OrdinalIgnoreCase)
               || message.Contains("is not valid for", StringComparison.OrdinalIgnoreCase)
               || message.Contains("could not be converted", StringComparison.OrdinalIgnoreCase)
               || message.Contains("The JSON value", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Nombra el campo como lo envió el cliente. La clave del <c>ModelState</c> del cuerpo JSON llega
    /// como <c>$.start_date</c>; se limpia el prefijo para que el mensaje hable el idioma del
    /// contrato, no el del binder.
    /// </summary>
    private static string FormatInvalidMessage(string key)
    {
        var field = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;

        return string.IsNullOrWhiteSpace(field)
            ? "El cuerpo de la petición no tiene el formato esperado."
            : $"El campo '{field}' no tiene un formato válido.";
    }
}
