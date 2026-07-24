using NSubstitute;
using NSubstitute.ExceptionExtensions;
using FluentAssertions;
using Terrenario.Api.Application.Auth;
using Terrenario.Api.Application.Auth.Commands;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Infrastructure.Auth;
using Terrenario.Api.Infrastructure.Telemetry;
using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Tests.Auth;

public class ExchangeGoogleCodeHandlerTests
{
    private readonly IGoogleOidcService _googleOidc = Substitute.For<IGoogleOidcService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IRefreshTokenStore _refreshTokenStore = Substitute.For<IRefreshTokenStore>();
    private readonly IActiveWorkspaceResolver _activeWorkspaceResolver = Substitute.For<IActiveWorkspaceResolver>();
    private readonly ILoginTelemetry _telemetry = Substitute.For<ILoginTelemetry>();

    private ExchangeGoogleCodeHandler CreateSut() => new(
        _googleOidc, _userRepository, _jwtService, _refreshTokenStore, _activeWorkspaceResolver, _telemetry);

    private static readonly ExchangeGoogleCodeCommand ValidCommand = new(
        Code: "auth-code",
        RedirectUri: "http://localhost:5173/auth/callback",
        CodeVerifier: "code-verifier");

    [Fact]
    public async Task Deberia_CrearUsuarioNuevo_Cuando_GoogleSubNoExisteEnDb()
    {
        // Arrange
        var identity = new GoogleIdentity("google-sub-123", "Test User", "test@example.com");
        _googleOidc.ExchangeCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(identity);
        _userRepository.FindByGoogleSubAsync("google-sub-123").Returns((User?)null);
        _jwtService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<Guid?>())
            .Returns(new IssuedAccessToken("access-token", 900));
        _refreshTokenStore.CreateAsync(Arg.Any<Guid>()).Returns("refresh-token");

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(ValidCommand, "flow-id");

        // Assert
        result.AccessToken.Should().Be("access-token");
        result.ExpiresIn.Should().Be(900);
        result.User.DisplayName.Should().Be("Test User");
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u =>
            u.GoogleSub == "google-sub-123" &&
            u.DisplayName == "Test User" &&
            u.Email == "test@example.com"));
    }

    [Fact]
    public async Task Deberia_ActualizarPerfil_Cuando_UsuarioYaExiste()
    {
        // Arrange
        var existingUser = User.Create("google-sub-123", "Old Name", "old@example.com");
        var identity = new GoogleIdentity("google-sub-123", "New Name", "new@example.com");
        _googleOidc.ExchangeCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(identity);
        _userRepository.FindByGoogleSubAsync("google-sub-123").Returns(existingUser);
        _jwtService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<Guid?>())
            .Returns(new IssuedAccessToken("access-token", 900));
        _refreshTokenStore.CreateAsync(Arg.Any<Guid>()).Returns("refresh-token");

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(ValidCommand, "flow-id");

        // Assert
        existingUser.DisplayName.Should().Be("New Name");
        existingUser.Email.Should().Be("new@example.com");
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task Deberia_LanzarExcepcion_Cuando_GoogleTokenEsInvalido()
    {
        // Arrange
        _googleOidc.ExchangeCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new GoogleOidcException("Token inválido", ErrorCodes.AuthGoogleTokenInvalid));

        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(ValidCommand, "flow-id");

        // Assert
        await act.Should().ThrowAsync<GoogleOidcException>()
            .Where(ex => ex.ErrorCode == ErrorCodes.AuthGoogleTokenInvalid);
        _telemetry.Received(1).LoginError("flow-id", ErrorCodes.AuthGoogleTokenInvalid);
    }

    [Fact]
    public async Task Deberia_NotificarTelemetria_Cuando_LoginExitoso()
    {
        // Arrange
        var identity = new GoogleIdentity("google-sub-123", "Test User", "test@example.com");
        _googleOidc.ExchangeCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(identity);
        _userRepository.FindByGoogleSubAsync("google-sub-123").Returns((User?)null);
        _jwtService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<Guid?>())
            .Returns(new IssuedAccessToken("access-token", 900));
        _refreshTokenStore.CreateAsync(Arg.Any<Guid>()).Returns("refresh-token");

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(ValidCommand, "flow-id");

        // Assert
        _telemetry.Received(1).LoginSuccess("flow-id");
    }

    [Fact]
    public async Task Deberia_DevolverSesionSinWorkspace_Cuando_UsuarioNoTieneNinguno()
    {
        // Arrange
        var identity = new GoogleIdentity("google-sub-123", "Test User", "test@example.com");
        _googleOidc.ExchangeCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(identity);
        _userRepository.FindByGoogleSubAsync("google-sub-123").Returns((User?)null);
        _activeWorkspaceResolver.ResolveAsync(Arg.Any<Guid>(), null, Arg.Any<CancellationToken>())
            .Returns((WorkspaceSummary?)null);
        _jwtService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<Guid?>())
            .Returns(new IssuedAccessToken("access-token", 900));
        _refreshTokenStore.CreateAsync(Arg.Any<Guid>()).Returns("refresh-token");

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(ValidCommand, "flow-id");

        // Assert — el cliente usa la ausencia de Workspace para entrar al onboarding de MVP-102
        result.Workspace.Should().BeNull();
        _jwtService.Received(1).IssueAccessToken(Arg.Any<Guid>(), Arg.Any<string?>(), null);
    }

    [Fact]
    public async Task Deberia_SituarSesionEnWorkspaceActivo_Cuando_UsuarioYaTieneUno()
    {
        // Arrange
        var existingUser = User.Create("google-sub-123", "Test User", "test@example.com");
        var workspace = new WorkspaceSummary(Guid.NewGuid(), "Finca El Olivar");
        var identity = new GoogleIdentity("google-sub-123", "Test User", "test@example.com");
        _googleOidc.ExchangeCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(identity);
        _userRepository.FindByGoogleSubAsync("google-sub-123").Returns(existingUser);
        _activeWorkspaceResolver.ResolveAsync(existingUser.Id, null, Arg.Any<CancellationToken>())
            .Returns(workspace);
        _jwtService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<Guid?>())
            .Returns(new IssuedAccessToken("access-token", 900));
        _refreshTokenStore.CreateAsync(Arg.Any<Guid>()).Returns("refresh-token");

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(ValidCommand, "flow-id");

        // Assert
        result.Workspace.Should().Be(workspace);
        _jwtService.Received(1).IssueAccessToken(existingUser.Id, "Test User", workspace.Id);
    }
}
