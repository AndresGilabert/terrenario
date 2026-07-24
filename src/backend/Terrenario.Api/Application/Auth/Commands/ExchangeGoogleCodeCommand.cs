namespace Terrenario.Api.Application.Auth.Commands;

public sealed record ExchangeGoogleCodeCommand(
    string Code,
    string RedirectUri,
    string CodeVerifier);

public sealed record ExchangeGoogleCodeResult(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserInfo User);

public sealed record UserInfo(Guid Id, string DisplayName);
