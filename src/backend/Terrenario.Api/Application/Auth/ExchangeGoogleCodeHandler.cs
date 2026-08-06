using Terrenario.Api.Application.Auth.Commands;
using Terrenario.Api.Application.Workers;
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
    private readonly MemberRosterService _memberRoster;
    private readonly ILoginTelemetry _telemetry;

    public ExchangeGoogleCodeHandler(
        IGoogleOidcService googleOidc,
        IUserRepository userRepository,
        IJwtService jwtService,
        IRefreshTokenStore refreshTokenStore,
        IActiveWorkspaceResolver activeWorkspaceResolver,
        MemberRosterService memberRoster,
        ILoginTelemetry telemetry)
    {
        _googleOidc = googleOidc;
        _userRepository = userRepository;
        _jwtService = jwtService;
        _refreshTokenStore = refreshTokenStore;
        _activeWorkspaceResolver = activeWorkspaceResolver;
        _memberRoster = memberRoster;
        _telemetry = telemetry;
    }

    public async Task<ExchangeGoogleCodeResult> HandleAsync(
        ExchangeGoogleCodeCommand command,
        LoginEventContext telemetryContext,
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
            _telemetry.LoginError(telemetryContext, ex.ErrorCode);
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
            var previousDisplayName = user.DisplayName;
            user.UpdateProfile(identity.DisplayName, identity.Email);

            // RN-036 — el nombre de un responsable con cuenta es el de su cuenta de Google, así que un
            // cambio de nombre de display se propaga al maestro de todos sus Workspaces (MVP-208,
            // CA-4). Solo cuando cambia de verdad: en el login normal no hay nada que resincronizar.
            if (!string.Equals(previousDisplayName, user.DisplayName, StringComparison.Ordinal))
                await _memberRoster.SyncIdentityAsync(user.Id, user.DisplayName, ct);
        }

        await _userRepository.SaveChangesAsync(ct);

        // Un usuario sin Workspace entra al onboarding de MVP-102; la sesión se emite sin contexto.
        var activeWorkspace = await _activeWorkspaceResolver.ResolveAsync(user.Id, ct: ct);

        var accessToken = _jwtService.IssueAccessToken(user.Id, user.DisplayName, activeWorkspace?.Id);
        var refreshToken = await _refreshTokenStore.CreateAsync(user.Id, ct);

        _telemetry.LoginSuccess(telemetryContext);

        return new ExchangeGoogleCodeResult(
            accessToken.Token,
            refreshToken,
            accessToken.ExpiresIn,
            new UserInfo(user.Id, user.DisplayName),
            activeWorkspace);
    }
}
