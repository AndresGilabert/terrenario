using Terrenario.Api.Application.Auth.Commands;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Infrastructure.Auth;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Application.Auth;

public sealed class ExchangeGoogleCodeHandler
{
    private readonly IGoogleOidcService _googleOidc;
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IActiveWorkspaceResolver _activeWorkspaceResolver;
    private readonly ILoginTelemetry _telemetry;

    public ExchangeGoogleCodeHandler(
        IGoogleOidcService googleOidc,
        IUserRepository userRepository,
        IJwtService jwtService,
        IRefreshTokenStore refreshTokenStore,
        IActiveWorkspaceResolver activeWorkspaceResolver,
        ILoginTelemetry telemetry)
    {
        _googleOidc = googleOidc;
        _userRepository = userRepository;
        _jwtService = jwtService;
        _refreshTokenStore = refreshTokenStore;
        _activeWorkspaceResolver = activeWorkspaceResolver;
        _telemetry = telemetry;
    }

    public async Task<ExchangeGoogleCodeResult> HandleAsync(
        ExchangeGoogleCodeCommand command,
        string flowId,
        CancellationToken ct = default)
    {
        GoogleIdentity identity;
        try
        {
            identity = await _googleOidc.ExchangeCodeAsync(
                command.Code,
                command.RedirectUri,
                command.CodeVerifier,
                ct);
        }
        catch (GoogleOidcException ex)
        {
            _telemetry.LoginError(flowId, ex.ErrorCode);
            throw;
        }

        var user = await _userRepository.FindByGoogleSubAsync(identity.Sub, ct);

        if (user is null)
        {
            user = User.Create(identity.Sub, identity.DisplayName, identity.Email);
            await _userRepository.AddAsync(user, ct);
        }
        else
        {
            user.UpdateProfile(identity.DisplayName, identity.Email);
        }

        await _userRepository.SaveChangesAsync(ct);

        // Un usuario sin Workspace entra al onboarding de MVP-102; la sesión se emite sin contexto.
        var activeWorkspace = await _activeWorkspaceResolver.ResolveAsync(user.Id, ct: ct);

        var accessToken = _jwtService.IssueAccessToken(user.Id, user.DisplayName, activeWorkspace?.Id);
        var refreshToken = await _refreshTokenStore.CreateAsync(user.Id, ct);

        _telemetry.LoginSuccess(flowId);

        return new ExchangeGoogleCodeResult(
            accessToken.Token,
            refreshToken,
            accessToken.ExpiresIn,
            new UserInfo(user.Id, user.DisplayName),
            activeWorkspace);
    }
}
