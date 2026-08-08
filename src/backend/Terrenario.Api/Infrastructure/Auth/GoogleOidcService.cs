using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Infrastructure.Auth;

public sealed class GoogleOidcService : IGoogleOidcService
{
    private readonly GoogleOidcOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleOidcService> _logger;

    public GoogleOidcService(
        IOptions<GoogleOidcOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<GoogleOidcService> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<GoogleIdentity> ExchangeCodeAsync(
        string code,
        string redirectUri,
        string codeVerifier,
        CancellationToken ct = default)
    {
        var tokenResponse = await ExchangeCodeForTokensAsync(code, redirectUri, codeVerifier, ct);

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                tokenResponse.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_options.ClientId]
                });

            return new GoogleIdentity(
                Sub: payload.Subject,
                DisplayName: payload.Name ?? payload.Email,
                Email: payload.Email);
        }
        catch (InvalidJwtException ex)
        {
            // MVP-502 (CA-2) — solo el **tipo** del fallo, nunca el mensaje. La excepción de la
            // librería de Google puede arrastrar fragmentos del propio `id_token` en su texto, y un
            // token de identidad es una credencial: no puede acabar en un log
            // (`docs/07-seguridad/privacidad-datos.md`).
            _logger.LogWarning("Validación del id_token de Google fallida ({Reason}).", ex.GetType().Name);
            throw new GoogleOidcException("Token de Google no válido.", ErrorCodes.AuthGoogleTokenInvalid);
        }
    }

    private async Task<GoogleTokenResponse> ExchangeCodeForTokensAsync(
        string code,
        string redirectUri,
        string codeVerifier,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("google-oauth");

        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = codeVerifier
        });

        var response = await client.PostAsync("https://oauth2.googleapis.com/token", formContent, ct);

        if (!response.IsSuccessStatusCode)
        {
            // MVP-502 (CA-2) — antes se registraba el cuerpo **entero** de la respuesta de Google.
            // Es una carga de un tercero sobre la que no tenemos control y que acompaña a una
            // petición que lleva el `code` y el `client_secret`: registrarla en claro contradice
            // «los tokens y credenciales del proveedor no se almacenarán en claro en logs»
            // (`privacidad-datos.md`). Se conserva solo el código de error de OAuth, que es lo único
            // que sirve para diagnosticar y es un valor de vocabulario cerrado.
            var oauthError = await ReadOAuthErrorAsync(response, ct);
            var errorCode = GoogleOAuthErrors.ToErrorCode(oauthError);

            // MVP-713 (`P-079`) — El nivel también se clasifica. Recargar la pantalla de vuelta de
            // Google es un suceso normal del uso, no una anomalía que alguien deba mirar: dejarlo en
            // `Warning` seguiría llenando de ruido el mismo canal por el que se diagnostican los fallos
            // de verdad, que es la otra mitad del problema que resuelve esta historia.
            _logger.Log(
                GoogleOidcErrorMapper.IsServerFault(errorCode) ? LogLevel.Warning : LogLevel.Information,
                "Intercambio de código con Google fallido ({StatusCode}, {OAuthError}, {ErrorCode}).",
                (int)response.StatusCode,
                oauthError,
                errorCode);

            throw new GoogleOidcException("Intercambio de código con Google fallido.", errorCode);
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(ct)
            ?? throw new GoogleOidcException("Respuesta de Google inesperada.", ErrorCodes.AuthGoogleExchangeFailed);

        return tokenResponse;
    }

    /// <summary>
    /// MVP-502 (CA-2) — Extrae <b>solo</b> el campo <c>error</c> del cuerpo de error de OAuth 2.0
    /// (RFC 6749 §5.2), que es un vocabulario cerrado (<c>invalid_grant</c>, <c>invalid_client</c>…)
    /// y por tanto seguro de registrar. Si el cuerpo no tiene esa forma no se registra nada de él:
    /// una carga ajena que no reconocemos no puede acabar en un log.
    ///
    /// MVP-713 — Desde esta historia el valor además <b>clasifica</b> la respuesta
    /// (<see cref="GoogleOAuthErrors"/>), así que ya no es solo material de diagnóstico.
    /// </summary>
    private static async Task<string> ReadOAuthErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<GoogleOAuthError>(ct);
            return string.IsNullOrWhiteSpace(error?.Error) ? GoogleOAuthErrors.Unknown : error.Error;
        }
        catch
        {
            return GoogleOAuthErrors.Unknown;
        }
    }

    private sealed record GoogleOAuthError([property: JsonPropertyName("error")] string? Error);

    private sealed record GoogleTokenResponse(
        [property: JsonPropertyName("id_token")] string IdToken,
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

public sealed class GoogleOidcOptions
{
    public const string SectionName = "Auth:Google";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
