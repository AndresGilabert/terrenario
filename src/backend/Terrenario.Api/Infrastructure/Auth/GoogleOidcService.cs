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
            _logger.LogWarning("Google id_token validation failed: {Reason}", ex.Message);
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
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Google token exchange failed: {StatusCode} {Body}", response.StatusCode, body);
            throw new GoogleOidcException("Intercambio de código con Google fallido.", ErrorCodes.AuthGoogleExchangeFailed);
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(ct)
            ?? throw new GoogleOidcException("Respuesta de Google inesperada.", ErrorCodes.AuthGoogleExchangeFailed);

        return tokenResponse;
    }

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
