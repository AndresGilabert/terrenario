using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Auth;
using Terrenario.Api.Application.Auth.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    ExchangeGoogleCodeHandler exchangeHandler,
    RefreshTokenHandler refreshHandler,
    IRefreshTokenStore refreshTokenStore,
    IUserRepository userRepository,
    ILogger<AuthController> logger) : ControllerBase
{
    private const string RefreshTokenCookieName = "refresh_token";

    [HttpPost("google/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(
        [FromBody] GoogleCallbackRequest request,
        CancellationToken ct)
    {
        var flowId = Guid.NewGuid().ToString("N");

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
                user = new { id = result.User.Id, display_name = result.User.DisplayName }
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

            return Ok(new { access_token = result.AccessToken, expires_in = result.ExpiresIn });
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
        var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        var user = await userRepository.FindByIdAsync(userId, ct);

        if (user is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        return Ok(new { id = user.Id, display_name = user.DisplayName });
    }

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
    [Required][property: JsonPropertyName("code_verifier")] string CodeVerifier);
