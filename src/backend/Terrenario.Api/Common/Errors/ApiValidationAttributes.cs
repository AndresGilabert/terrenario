using System.ComponentModel.DataAnnotations;

namespace Terrenario.Api.Common.Errors;

/// <summary>
/// MVP-502 (<c>P-043</c>) — Cómo viaja el <b>código de error de dominio</b> desde una anotación de
/// validación hasta la respuesta.
///
/// El problema: <c>ModelState</c> solo guarda un <b>texto</b> por error. No hay hueco para un código,
/// y no se puede saber desde la fábrica de respuestas qué atributo falló ni qué código le
/// correspondía. Por eso <c>InvalidModelStateResponseFactory</c> colapsaba **toda** la validación de
/// alta a <c>VALIDATION_REQUIRED</c> y un cliente no podía distinguir «falta el nombre» de «el nombre
/// es demasiado largo», mientras el <c>PATCH</c> —que valida en el dominio— sí devolvía el código
/// específico.
///
/// La solución: el atributo compone el mensaje como <c>CÓDIGO␟texto</c> y la fábrica lo descompone.
/// El separador es <c>U+001F</c> (Unit Separator), que no aparece en texto escrito por personas, así
/// que no hay ambigüedad posible. La convención vive **solo aquí y en la fábrica**: nadie más la ve.
/// </summary>
public static class ApiValidationMessage
{
    private const char Separator = '';

    public static string Encode(string code, string message) => $"{code}{Separator}{message}";

    /// <summary>
    /// Descompone un mensaje de validación. Devuelve <c>false</c> si no lleva código, que es lo que
    /// pasa con los mensajes generados por el propio ASP.NET —y que además vienen en inglés—.
    /// </summary>
    public static bool TryDecode(string? raw, out string code, out string message)
    {
        code = string.Empty;
        message = string.Empty;

        if (string.IsNullOrEmpty(raw)) return false;

        var separator = raw.IndexOf(Separator);
        if (separator <= 0) return false;

        code = raw[..separator];
        message = raw[(separator + 1)..];
        return true;
    }
}

/// <summary>
/// Campo obligatorio que declara <b>su</b> código de error, para que el alta responda lo mismo que
/// responde la edición ante el mismo fallo (<c>P-043</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class RequiredFieldAttribute : RequiredAttribute
{
    public RequiredFieldAttribute(string code, string message)
    {
        ErrorMessage = ApiValidationMessage.Encode(code, message);
    }
}

/// <summary>
/// Longitud máxima que declara <b>su</b> código de error. Es el caso que <c>P-043</c> nombra: un
/// nombre demasiado largo respondía <c>VALIDATION_REQUIRED</c>, que no dice nada de lo que pasó.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class MaxTextLengthAttribute : StringLengthAttribute
{
    public MaxTextLengthAttribute(int maximumLength, string code, string message)
        : base(maximumLength)
    {
        ErrorMessage = ApiValidationMessage.Encode(code, message);
    }
}
