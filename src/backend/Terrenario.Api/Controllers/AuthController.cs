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

        try
        {
            var result = await exchangeHandler.HandleAsync(
                new ExchangeGoogleCodeCommand(request.Code, request.RedirectUri, request.CodeVerifier),
                flowId,
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
        catch (GoogleOidcException ex) when (ex.ErrorCode == ErrorCodes.AuthGoogleTokenInvalid)
        {
            return Unauthorized(new ApiErrorResponse(ApiError.GoogleTokenInvalid()));
        }
        catch (GoogleOidcException ex) when (ex.ErrorCode == ErrorCodes.AuthGoogleExchangeFailed)
        {
            logger.LogError(ex, "Google code exchange failed for flow {FlowId}", flowId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse(ApiError.GoogleExchangeFailed()));
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

        switch (request.Event)
        {
            case LoginFunnelEvents.ScreenViewed:
                telemetry.LoginScreenViewed(request.FlowId!);
                break;
            case LoginFunnelEvents.GoogleClicked:
                telemetry.LoginGoogleClicked(request.FlowId!);
                break;
            case LoginFunnelEvents.Abandonment:
                telemetry.LoginAbandoned(request.FlowId!);
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
    [property: JsonPropertyName("flow_id")] string? FlowId = null);

/// <summary>Evento del embudo de login originado en el cliente (MVP-105).</summary>
public sealed record LoginTelemetryRequest(
    [property: JsonPropertyName("event")] string? Event,
    [property: JsonPropertyName("flow_id")] string? FlowId);
