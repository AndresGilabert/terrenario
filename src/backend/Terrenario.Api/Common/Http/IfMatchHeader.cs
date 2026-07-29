using Microsoft.AspNetCore.Http;

namespace Terrenario.Api.Common.Http;

/// <summary>
/// Lectura de la cabecera <c>If-Match</c> de los registros operativos (ADR-0005). El contrato la
/// exige en <c>PATCH</c> y <c>DELETE</c> de las entidades críticas y publica la versión como un
/// entero (<c>version</c> de la respuesta), pero un cliente HTTP correcto puede enviarla como
/// <b>ETag</b> —entrecomillada y con el prefijo débil <c>W/</c>—, así que se aceptan las tres formas:
/// <c>3</c>, <c>"3"</c> y <c>W/"3"</c>.
///
/// Se rechaza explícitamente <c>*</c>: significa «cualquier versión», que es justo lo que el bloqueo
/// optimista existe para impedir.
///
/// Helper transversal en <c>Common</c>: lo estrena MVP-301 y lo reutilizan MVP-303/MVP-304 y, más
/// adelante, la cosecha de MVP-401.
/// </summary>
public static class IfMatchHeader
{
    /// <summary>
    /// Devuelve <c>true</c> y la versión si la cabecera trae un entero válido; <c>false</c> si falta,
    /// viene vacía, es <c>*</c> o no es un número (los cuatro casos se responden igual: 400
    /// <c>VALIDATION_REQUIRED_IF_MATCH</c>, porque en todos falta una versión con la que comparar).
    /// </summary>
    public static bool TryRead(IHeaderDictionary headers, out long version)
    {
        version = 0;

        var raw = headers.IfMatch.Count > 0 ? headers.IfMatch[0] : null;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var value = raw.Trim();
        if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
            value = value[2..].Trim();
        value = value.Trim('"').Trim();

        if (value.Length == 0 || value == "*") return false;

        return long.TryParse(value, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out version);
    }
}
