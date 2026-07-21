using Terrenario.Api.Application.Auth.Commands;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Application.Auth;

public sealed class RefreshTokenHandler
{
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IJwtService _jwtService;

    public RefreshTokenHandler(IRefreshTokenStore refreshTokenStore, IJwtService jwtService)
    {
        _refreshTokenStore = refreshTokenStore;
        _jwtService = jwtService;
    }

    public async Task<(RefreshTokenResult Result, string NewRefreshToken)> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken ct = default)
    {
        var userId = await _refreshTokenStore.ValidateAndRotateAsync(command.RefreshToken, ct);

        var accessToken = _jwtService.IssueAccessToken(userId, displayName: null);

        var newRefreshToken = await _refreshTokenStore.CreateAsync(userId, ct);

        return (new RefreshTokenResult(accessToken.Token, accessToken.ExpiresIn), newRefreshToken);
    }
}
