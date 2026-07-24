using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Tests.Workspaces;

public class CreateWorkspaceHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();

    private CreateWorkspaceHandler CreateSut() => new(_workspaceRepository, _jwtService);

    private static readonly Guid UserId = Guid.NewGuid();

    private static CreateWorkspaceCommand CommandFor(string nombre) =>
        new(UserId, "Antonio", nombre);

    [Fact]
    public async Task Deberia_PersistirWorkspaceYMembresiaDelCreador_Cuando_NombreEsValido()
    {
        // Arrange
        _jwtService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<Guid?>())
            .Returns(new IssuedAccessToken("access-token", 900));

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(CommandFor("Finca El Olivar"));

        // Assert
        result.Workspace.Name.Should().Be("Finca El Olivar");
        await _workspaceRepository.Received(1).AddAsync(
            Arg.Is<Workspace>(w => w.OwnerId == UserId && w.Name == "Finca El Olivar"),
            Arg.Is<WorkspaceMember>(m => m.UserId == UserId && m.Active && m.Role == WorkspaceRoles.Owner),
            Arg.Any<CancellationToken>());
        await _workspaceRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_VincularMembresiaAlWorkspaceCreado_Cuando_SeGuarda()
    {
        // Arrange
        _jwtService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<Guid?>())
            .Returns(new IssuedAccessToken("access-token", 900));

        Workspace? persistedWorkspace = null;
        WorkspaceMember? persistedMembership = null;
        await _workspaceRepository.AddAsync(
            Arg.Do<Workspace>(w => persistedWorkspace = w),
            Arg.Do<WorkspaceMember>(m => persistedMembership = m),
            Arg.Any<CancellationToken>());

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(CommandFor("Finca El Olivar"));

        // Assert
        persistedMembership!.WorkspaceId.Should().Be(persistedWorkspace!.Id);
    }

    [Fact]
    public async Task Deberia_EmitirAccessTokenConWorkspaceActivo_Cuando_SeCreaElWorkspace()
    {
        // Arrange
        _jwtService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<Guid?>())
            .Returns(new IssuedAccessToken("access-token-con-workspace", 900));

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(CommandFor("Finca El Olivar"));

        // Assert — CA-2: la sesión queda situada en el Workspace recién creado
        result.AccessToken.Should().Be("access-token-con-workspace");
        result.ExpiresIn.Should().Be(900);
        _jwtService.Received(1).IssueAccessToken(UserId, "Antonio", result.Workspace.Id);
    }

    [Fact]
    public async Task Deberia_NoPersistirNada_Cuando_NombreEsInvalido()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(CommandFor("   "));

        // Assert
        (await act.Should().ThrowAsync<WorkspaceValidationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredWorkspaceName);
        await _workspaceRepository.DidNotReceive().AddAsync(
            Arg.Any<Workspace>(), Arg.Any<WorkspaceMember>(), Arg.Any<CancellationToken>());
        await _workspaceRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
