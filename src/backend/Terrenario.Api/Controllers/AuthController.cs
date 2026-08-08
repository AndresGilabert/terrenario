using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Auth;
using Terrenario.Api.Application.Auth.Commands;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Infrastructure.Auth;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    ExchangeGoogleCodeHandler exchangeHandler,
    RefreshTokenHandler refreshHandler,
    IRefreshTokenStore refreshTokenStore,
    IUserRepository userRepository,
    ILoginTelemetry telemetry,
    ILogger<AuthController> logger) : ControllerBase
{
    private const string RefreshTokenCookieName = "refresh_token";

    [HttpPost("google/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(
        [FromBody] GoogleCallbackRequest request,
        CancellationToken ct)
    {
        // El cliente correlaciona el embudo con un flow_id generado al ver la pantalla de login; si
        // no llega (o no es válido) se genera uno para no perder la traza de éxito/error (MVP-105).
        var flowId = LoginFunnelEvents.IsValidFlowId(request.FlowId)
            ? request.FlowId!
            : Guid.NewGuid().ToString("N");

        // MVP-601 — El éxito y el error se emiten aquí, así que las dimensiones que solo conoce el
        // cliente (sesión y tipo de dispositivo) tienen que viajar en el intercambio o el embudo
        // quedaría medido a medias: los eventos de entrada con ellas y los de salida sin ellas.
        var telemetryContext = LoginEventContext.Create(flowId, request.SessionId, request.DeviceType);

        try
        {
            var result = await exchangeHandler.HandleAsync(
                new ExchangeGoogleCodeCommand(request.Code, request.RedirectUri, request.CodeVerifier),
                telemetryContext,
                ct);

            SetRefreshTokenCookie(result.RefreshToken);

            return Ok(new
            {
                access_token = result.AccessToken,
                expires_in = result.ExpiresIn,
                user = new { id = result.User.Id, display_name = result.User.DisplayName },
                workspace = ToWorkspacePayload(result.Workspace)
            });
        }
        catch (GoogleOidcException ex)
        {
            // MVP-713 (`P-079`) — Una sola captura sobre la tabla de clasificación, en vez de una
            // cláusula por código: un código nuevo sin clasificar se resuelve como 500 (el defecto
            // conservador) en lugar de escaparse sin capturar, que era lo que ocurría antes.
            var statusCode = GoogleOidcErrorMapper.StatusFor(ex.ErrorCode);

            // `LogError` solo para lo que de verdad es un fallo del servidor. Un código de Google
            // caducado no es una incidencia: es alguien recargando la pantalla de vuelta.
            if (GoogleOidcErrorMapper.IsServerFault(ex.ErrorCode))
                logger.LogError(ex, "Intercambio de código con Google fallido en el flujo {FlowId}.", flowId);

            return StatusCode(statusCode,
                new ApiErrorResponse(GoogleOidcErrorMapper.ToApiError(ex.ErrorCode)));
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized(new ApiErrorResponse(ApiError.RefreshTokenInvalid()));

        try
        {
            var (result, newRefreshToken) = await refreshHandler.HandleAsync(
                new RefreshTokenCommand(refreshToken),
                ct);

            SetRefreshTokenCookie(newRefreshToken);

            return Ok(new
            {
                access_token = result.AccessToken,
                expires_in = result.ExpiresIn,
                workspace = ToWorkspacePayload(result.Workspace)
            });
        }
        catch (RefreshTokenException)
        {
            RemoveRefreshTokenCookie();
            return Unauthorized(new ApiErrorResponse(ApiError.RefreshTokenInvalid()));
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (!string.IsNullOrWhiteSpace(refreshToken))
            await refreshTokenStore.RevokeAsync(refreshToken, ct);

        RemoveRefreshTokenCookie();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        var user = await userRepository.FindByIdAsync(userId.Value, ct);

        if (user is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        return Ok(new { id = user.Id, display_name = user.DisplayName });
    }

    /// <summary>
    /// MVP-105 — Ingesta de la señal mínima del embudo de login originada en el cliente
    /// (pantalla vista, clic en Google, abandono). El éxito y el error se emiten en servidor durante
    /// el intercambio con Google, así que no se aceptan aquí. La traza no contiene PII: solo el
    /// nombre del evento y un flow_id aleatorio (RN-020, CA-2/CA-3).
    ///
    /// MVP-601 — El cuerpo admite además <c>session_id</c> y <c>device_type</c>, las dos dimensiones
    /// mínimas que faltaban. Un valor ausente o mal formado **no descarta el evento**: se registra como
    /// <c>unknown</c>. Perder la conversión entera por una dimensión secundaria sería peor medida que
    /// tener una dimensión con huecos, y además convertiría la telemetría en una forma de que el
    /// cliente decidiera qué se cuenta.
    /// </summary>
    [HttpPost("telemetry/login")]
    [AllowAnonymous]
    public IActionResult LoginTelemetry([FromBody] LoginTelemetryRequest request)
    {
        if (!LoginFunnelEvents.IsValidFlowId(request.FlowId))
            return BadRequest(new ApiErrorResponse(
                ApiError.Validation(ErrorCodes.ValidationRequired, "flow_id inválido.")));

        if (request.Event is null || !LoginFunnelEvents.ClientIngestable.Contains(request.Event))
            return BadRequest(new ApiErrorResponse(
                ApiError.Validation(ErrorCodes.ValidationRequired, "Evento de login no reconocido.")));

        var context = LoginEventContext.Create(request.FlowId!, request.SessionId, request.DeviceType);

        switch (request.Event)
        {
            case LoginFunnelEvents.ScreenViewed:
                telemetry.LoginScreenViewed(context);
                break;
            case LoginFunnelEvents.GoogleClicked:
                telemetry.LoginGoogleClicked(context);
                break;
            case LoginFunnelEvents.Abandonment:
                telemetry.LoginAbandoned(context);
                break;
        }

        // Fire-and-forget desde la perspectiva del cliente: la telemetría no debe frenar el login.
        return Accepted();
    }

    private static object? ToWorkspacePayload(WorkspaceSummary? workspace)
        => workspace is null ? null : new { id = workspace.Id, name = workspace.Name };

    private void SetRefreshTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !HttpContext.Request.IsHttps ? false : true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
            MaxAge = TimeSpan.FromSeconds(2_592_000)
        };

        Response.Cookies.Append(RefreshTokenCookieName, token, cookieOptions);
    }

    private void RemoveRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth"
        });
    }
}

public sealed record GoogleCallbackRequest(
    [Required] string Code,
    [Required][property: JsonPropertyName("redirect_uri")] string RedirectUri,
    [Required][property: JsonPropertyName("code_verifier")] string CodeVerifier,
    // Correlador del embudo de login emitido por el cliente (MVP-105). Opcional: si no llega, el
    // servidor genera uno para no perder la traza de éxito/error.
    [property: JsonPropertyName("flow_id")] string? FlowId = null,
    // Dimensiones mínimas del embudo que solo conoce el cliente (MVP-601). Opcionales: su ausencia
    // degrada la dimensión a `unknown`, nunca impide el acceso.
    [property: JsonPropertyName("session_id")] string? SessionId = null,
    [property: JsonPropertyName("device_type")] string? DeviceType = null);

/// <summary>Evento del embudo de login originado en el cliente (MVP-105 · MVP-601).</summary>
public sealed record LoginTelemetryRequest(
    [property: JsonPropertyName("event")] string? Event,
    [property: JsonPropertyName("flow_id")] string? FlowId,
    [property: JsonPropertyName("session_id")] string? SessionId = null,
    [property: JsonPropertyName("device_type")] string? DeviceType = null);
