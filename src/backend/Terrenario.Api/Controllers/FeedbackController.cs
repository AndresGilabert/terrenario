using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Feedback;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Infrastructure.Feedback;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-711 (`P-088`) — Canal de sugerencias e incidencias.
///
/// <b>Autenticado, sin ámbito de Workspace.</b> Autenticado porque el reporte lleva quién lo manda y
/// porque el límite anti-abuso es por cuenta (CA-6); sin ámbito de Workspace por el mismo motivo que
/// la baja de cuenta (<c>MVP-505</c>): quien no tiene Workspace activo —o acaba de perderlo— es
/// exactamente quien más motivos puede tener para escribir, y exigir contexto lo dejaría fuera.
///
/// <b>El servidor no se fía del contexto que le mandan</b>: la versión la pone él, el navegador lo
/// lee de la cabecera de esta misma petición, y de la ruta y la correlación solo acepta lo que tiene
/// forma de serlo. No es desconfianza hacia el cliente, es que este cuerpo acaba en una bandeja de
/// correo y todo lo que llegue allí tiene que estar acotado.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/feedback")]
public sealed class FeedbackController(
    SubmitFeedbackHandler handler,
    FeedbackRateLimiter limiter,
    ILogger<FeedbackController> logger) : ControllerBase
{
    /// <summary>
    /// Tope del texto libre. Cabe de sobra una explicación con pasos para reproducir, y evita que el
    /// canal se convierta en una vía para meter medio megabyte en un correo.
    /// </summary>
    public const int MaxMessageLength = 2000;

    /// <summary>Tope de la ruta. Las del producto no pasan de treinta caracteres.</summary>
    private const int MaxPathLength = 200;

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] FeedbackRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        if (request.Kind is null || !FeedbackKinds.All.Contains(request.Kind))
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationFeedbackKindInvalid,
                "Indica si es una incidencia o una sugerencia.")));

        var message = request.Message?.Trim();

        if (string.IsNullOrEmpty(message))
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationRequiredFeedbackMessage,
                "Cuéntanos qué ha pasado antes de enviar.")));

        if (message.Length > MaxMessageLength)
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationFeedbackMessageLength,
                $"El mensaje no puede pasar de {MaxMessageLength} caracteres.")));

        if (!handler.IsChannelAvailable)
        {
            // Ni siquiera se comprueba el cupo: no hay a dónde enviar, así que nadie debería gastar
            // uno. Se traza porque es un fallo de configuración del despliegue, no del usuario.
            logger.LogError(
                "feedback.unavailable — falta 'Feedback:Recipient' o la cuenta de envío. "
                + "El reporte del usuario no ha salido.");

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiErrorResponse(
                ApiError.Validation(
                    ErrorCodes.FeedbackChannelUnavailable,
                    "El canal de sugerencias no está disponible ahora mismo. Inténtalo más tarde.")));
        }

        if (!limiter.IsAllowed(userId.Value, out var retryAfter))
        {
            Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

            return StatusCode(StatusCodes.Status429TooManyRequests, new ApiErrorResponse(
                ApiError.Validation(
                    ErrorCodes.RateLimitFeedback,
                    $"Has enviado {FeedbackRateLimiter.MaxPerWindow} mensajes en la última hora. "
                    + "Espera un rato antes de mandar otro.")));
        }

        var submission = new FeedbackSubmission(
            request.Kind,
            message,
            SanitizePath(request.Path),
            SanitizeRequestId(request.LastFailedRequestId),
            Request.Headers.UserAgent.ToString() is { Length: > 0 } agent ? agent : null);

        try
        {
            if (!await handler.HandleAsync(userId.Value, submission, ct))
                return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));
        }
        catch (Exception ex)
        {
            // Un fallo del proveedor de correo es infraestructura: se traza y se traduce a un error de
            // aplicación (estándares de código), en vez de salir como 500 sin explicación. Y sobre
            // todo **no se confirma nada**: decir «enviado» sin haber enviado es peor que el fallo.
            logger.LogError(ex, "feedback.delivery_failed kind={Kind}", submission.Kind);

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiErrorResponse(
                ApiError.Validation(
                    ErrorCodes.FeedbackDeliveryFailed,
                    "No hemos podido enviar tu mensaje. Vuelve a intentarlo en unos minutos.")));
        }

        limiter.Register(userId.Value);

        return Accepted();
    }

    /// <summary>
    /// Se queda con la ruta y <b>tira query y fragmento</b>.
    ///
    /// Es la garantía de que el contexto técnico no arrastra datos del Workspace: los filtros del
    /// panel viven en la URL desde <c>MVP-403</c> y llevan identificadores de terreno y de temporada.
    /// El cliente ya manda solo <c>location.pathname</c>, pero recortar aquí es lo que lo convierte en
    /// una propiedad del sistema y no en una costumbre del cliente.
    /// </summary>
    private static string? SanitizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var trimmed = path.Trim();
        if (!trimmed.StartsWith('/')) return null;

        var cut = trimmed.IndexOfAny(['?', '#']);
        if (cut >= 0) trimmed = trimmed[..cut];

        if (trimmed.Length is 0 or > MaxPathLength) return null;

        // Un juego de caracteres cerrado, no una lista de prohibidos: lo que va a una bandeja de
        // correo se define por lo que se acepta.
        return trimmed.All(c => char.IsAsciiLetterOrDigit(c) || c is '/' or '-' or '_' or '.')
            ? trimmed
            : null;
    }

    /// <summary>
    /// Acepta la correlación solo si tiene la forma que emite <see cref="RequestIdMiddleware"/>. Un
    /// valor que no la tenga no sirve para buscar en la traza, así que se descarta en vez de copiarlo
    /// al correo: un identificador inventado manda a quien lo lea a buscar algo que no existe.
    /// </summary>
    private static string? SanitizeRequestId(string? requestId) =>
        RequestIdMiddleware.IsValidRequestId(requestId) ? requestId : null;
}

/// <summary>
/// MVP-711 — Reporte enviado desde el cliente.
///
/// Solo cuatro campos, y los dos últimos son el contexto que el servidor no puede saber por su
/// cuenta: en qué pantalla estaba la persona y qué petición le falló. Todo lo demás del contexto
/// técnico lo resuelve el servidor.
/// </summary>
public sealed record FeedbackRequest(
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("path")] string? Path = null,
    [property: JsonPropertyName("last_failed_request_id")] string? LastFailedRequestId = null);
