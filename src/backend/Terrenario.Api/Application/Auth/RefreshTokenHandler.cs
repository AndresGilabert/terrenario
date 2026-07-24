using Terrenario.Api.Application.Auth.Commands;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Application.Auth;

public sealed class RefreshTokenHandler
{
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IJwtService _jwtService;
    private readonly IActiveWorkspaceResolver _activeWorkspaceResolver;

    public RefreshTokenHandler(
        IRefreshTokenStore refreshTokenStore,
        IJwtService jwtService,
        IActiveWorkspaceResolver activeWorkspaceResolver)
    {
        _refreshTokenStore = refreshTokenStore;
        _jwtService = jwtService;
        _activeWorkspaceResolver = activeWorkspaceResolver;
    }

    public async Task<(RefreshTokenResult Result, string NewRefreshToken)> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken ct = default)
    {
        var userId = await _refreshTokenStore.ValidateAndRotateAsync(command.RefreshToken, ct);

        // El contexto de Workspace se re-resuelve en cada renovación para que la sesión
        // renovada no pierda el Workspace activo.
        var activeWorkspace = await _activeWorkspaceResolver.ResolveAsync(userId, ct: ct);

        var accessToken = _jwtService.IssueAccessToken(userId, displayName: null, activeWorkspace?.Id);

        var newRefreshToken = await _refreshTokenStore.CreateAsync(userId, ct);

        return (new RefreshTokenResult(accessToken.Token, accessToken.ExpiresIn, activeWorkspace), newRefreshToken);
    }
}
