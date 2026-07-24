namespace Terrenario.Api.Infrastructure.Auth;

public sealed record GoogleIdentity(string Sub, string DisplayName, string Email);

public interface IGoogleOidcService
{
    Task<GoogleIdentity> ExchangeCodeAsync(
        string code,
        string redirectUri,
        string codeVerifier,
        CancellationToken ct = default);
}

public sealed class GoogleOidcException(string message, string errorCode)
    : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
