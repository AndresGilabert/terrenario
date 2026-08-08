using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Infrastructure.Auth;

/// <summary>
/// MVP-713 (`P-079`) — El vocabulario cerrado de errores del endpoint de token de OAuth 2.0
/// (RFC 6749 §5.2) y a qué código de la API corresponde cada uno.
///
/// Hasta esta historia <b>cualquier</b> respuesta no exitosa de Google se traducía a
/// <see cref="ErrorCodes.AuthGoogleExchangeFailed"/> → <c>500</c>. Eso metía en el numerador del SLO de
/// tasa de error algo tan cotidiano como recargar la pantalla de vuelta de Google, y un solo caso sobre
/// 70 peticiones llegó a disparar <c>HighErrorRate</c> —crítica, con correo— en la revisión de
/// `MVP-699`. El daño no es el código de estado: una alerta que salta sin motivo se acaba ignorando
/// <b>también cuando el motivo es real</b>.
///
/// La distinción se hace aquí y no en el controlador porque el valor de partida es de Google: este es
/// el único punto del sistema que habla su idioma.
/// </summary>
public static class GoogleOAuthErrors
{
    /// <summary>El código ya se usó o caducó — el caso de `P-079`. También un `code_verifier` que no casa.</summary>
    public const string InvalidGrant = "invalid_grant";

    /// <summary>Falta un parámetro del intercambio o está repetido/mal formado.</summary>
    public const string InvalidRequest = "invalid_request";

    /// <summary>Las credenciales de la aplicación no son válidas: configuración nuestra.</summary>
    public const string InvalidClient = "invalid_client";

    /// <summary>La aplicación no tiene autorizado este flujo: configuración nuestra.</summary>
    public const string UnauthorizedClient = "unauthorized_client";

    /// <summary>
    /// Lo que se registra cuando la respuesta de error no trae un <c>error</c> reconocible. Se conserva
    /// el valor de MVP-502: una carga ajena que no tiene la forma esperada no se registra.
    /// </summary>
    public const string Unknown = "sin_detalle";

    /// <summary>
    /// Traduce el <c>error</c> de OAuth al código de la API.
    ///
    /// El caso por defecto es <b>fallo del servidor</b>, no error de cliente: ampliar la lista de
    /// errores «de cliente» por descarte convertiría cualquier fallo nuevo de Google —o cualquier
    /// respuesta que no sepamos leer— en un 4xx silencioso, que es justo la avería que las alertas
    /// tienen que ver.
    /// </summary>
    public static string ToErrorCode(string? oauthError) => oauthError switch
    {
        InvalidGrant => ErrorCodes.AuthGoogleCodeInvalid,
        InvalidRequest => ErrorCodes.AuthGoogleRequestInvalid,
        InvalidClient or UnauthorizedClient => ErrorCodes.AuthGoogleExchangeFailed,
        _ => ErrorCodes.AuthGoogleExchangeFailed
    };
}
