using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

public class ListUserWorkspacesHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IActiveWorkspaceResolver _activeWorkspaceResolver =
        Substitute.For<IActiveWorkspaceResolver>();

    private static readonly Guid UserId = Guid.NewGuid();

    private ListUserWorkspacesHandler CreateSut() =>
        new(_workspaceRepository, _activeWorkspaceResolver);

    [Fact]
    public async Task Deberia_DevolverMembresiasYMarcarElActivo_Cuando_UsuarioPerteneceAVarios()
    {
        // Arrange
        var primero = new WorkspaceMembership(
            Guid.NewGuid(), "Finca El Olivar", WorkspaceRoles.Owner,
            WorkspaceMemberStatuses.Active, DateTimeOffset.UtcNow);
        var segundo = new WorkspaceMembership(
            Guid.NewGuid(), "Finca La Vega", WorkspaceRoles.Member,
            WorkspaceMemberStatuses.Active, DateTimeOffset.UtcNow);

        _workspaceRepository.ListActiveMembershipsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { primero, segundo });
        _activeWorkspaceResolver.ResolveAsync(UserId, segundo.WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(new WorkspaceSummary(segundo.WorkspaceId, segundo.Name));

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(new ListUserWorkspacesQuery(UserId, segundo.WorkspaceId));

        // Assert — CA-1: el usuario ve todos sus Workspaces y distingue el activo
        result.Workspaces.Should().HaveCount(2);
        result.ActiveWorkspaceId.Should().Be(segundo.WorkspaceId);
    }

    [Fact]
    public async Task Deberia_DevolverListaVaciaSinActivo_Cuando_UsuarioNoTieneMembresias()
    {
        // Arrange
        _workspaceRepository.ListActiveMembershipsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<WorkspaceMembership>());
        _activeWorkspaceResolver.ResolveAsync(UserId, null, Arg.Any<CancellationToken>())
            .Returns((WorkspaceSummary?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(new ListUserWorkspacesQuery(UserId, null));

        // Assert
        result.Workspaces.Should().BeEmpty();
        result.ActiveWorkspaceId.Should().BeNull();
    }
}
