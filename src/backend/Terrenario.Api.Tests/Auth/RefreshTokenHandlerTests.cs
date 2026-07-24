using NSubstitute;
using NSubstitute.ExceptionExtensions;
using FluentAssertions;
using Terrenario.Api.Application.Auth;
using Terrenario.Api.Application.Auth.Commands;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Tests.Auth;

public class RefreshTokenHandlerTests
{
    private readonly IRefreshTokenStore _refreshTokenStore = Substitute.For<IRefreshTokenStore>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IActiveWorkspaceResolver _activeWorkspaceResolver = Substitute.For<IActiveWorkspaceResolver>();

    private RefreshTokenHandler CreateSut() => new(_refreshTokenStore, _jwtService, _activeWorkspaceResolver);

    [Fact]
    public async Task Deberia_EmitirNuevoAccessToken_Cuando_RefreshTokenEsValido()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _refreshTokenStore.ValidateAndRotateAsync("valid-refresh-token").Returns(userId);
        _refreshTokenStore.CreateAsync(userId).Returns("new-refresh-token");
        _jwtService.IssueAccessToken(userId, null, Arg.Any<Guid?>())
            .Returns(new IssuedAccessToken("new-access-token", 900));

        var sut = CreateSut();

        // Act
        var (result, newRefreshToken) = await sut.HandleAsync(new RefreshTokenCommand("valid-refresh-token"));

        // Assert
        result.AccessToken.Should().Be("new-access-token");
        result.ExpiresIn.Should().Be(900);
        newRefreshToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task Deberia_LanzarExcepcion_Cuando_RefreshTokenEsInvalido()
    {
        // Arrange
        _refreshTokenStore.ValidateAndRotateAsync("invalid-token")
            .ThrowsAsync(new RefreshTokenException("AUTH_REFRESH_TOKEN_INVALID"));

        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(new RefreshTokenCommand("invalid-token"));

        // Assert
        await act.Should().ThrowAsync<RefreshTokenException>();
    }

    [Fact]
    public async Task Deberia_RotarRefreshToken_Cuando_RenovacionExitosa()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _refreshTokenStore.ValidateAndRotateAsync("old-refresh-token").Returns(userId);
        _refreshTokenStore.CreateAsync(userId).Returns("rotated-refresh-token");
        _jwtService.IssueAccessToken(userId, null, Arg.Any<Guid?>())
            .Returns(new IssuedAccessToken("new-access-token", 900));

        var sut = CreateSut();

        // Act
        var (_, newRefreshToken) = await sut.HandleAsync(new RefreshTokenCommand("old-refresh-token"));

        // Assert
        newRefreshToken.Should().Be("rotated-refresh-token");
        await _refreshTokenStore.Received(1).ValidateAndRotateAsync("old-refresh-token");
        await _refreshTokenStore.Received(1).CreateAsync(userId);
    }

    [Fact]
    public async Task Deberia_MantenerWorkspaceActivo_Cuando_SeRenuevaLaSesion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workspace = new WorkspaceSummary(Guid.NewGuid(), "Finca El Olivar");
        _refreshTokenStore.ValidateAndRotateAsync("valid-refresh-token").Returns(userId);
        _refreshTokenStore.CreateAsync(userId).Returns("new-refresh-token");
        _activeWorkspaceResolver.ResolveAsync(userId, null, Arg.Any<CancellationToken>()).Returns(workspace);
        _jwtService.IssueAccessToken(userId, null, workspace.Id)
            .Returns(new IssuedAccessToken("new-access-token", 900));

        var sut = CreateSut();

        // Act
        var (result, _) = await sut.HandleAsync(new RefreshTokenCommand("valid-refresh-token"));

        // Assert
        result.Workspace.Should().Be(workspace);
        _jwtService.Received(1).IssueAccessToken(userId, null, workspace.Id);
    }

    [Fact]
    public async Task Deberia_RenovarSinWorkspace_Cuando_UsuarioTodaviaNoTieneNinguno()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _refreshTokenStore.ValidateAndRotateAsync("valid-refresh-token").Returns(userId);
        _refreshTokenStore.CreateAsync(userId).Returns("new-refresh-token");
        _activeWorkspaceResolver.ResolveAsync(userId, null, Arg.Any<CancellationToken>())
            .Returns((WorkspaceSummary?)null);
        _jwtService.IssueAccessToken(userId, null, null)
            .Returns(new IssuedAccessToken("new-access-token", 900));

        var sut = CreateSut();

        // Act
        var (result, _) = await sut.HandleAsync(new RefreshTokenCommand("valid-refresh-token"));

        // Assert
        result.Workspace.Should().BeNull();
        _jwtService.Received(1).IssueAccessToken(userId, null, null);
    }
}
