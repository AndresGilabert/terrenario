using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

public class ActiveWorkspaceResolverTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();

    private ActiveWorkspaceResolver CreateSut() => new(_workspaceRepository);

    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Deberia_DevolverNull_Cuando_UsuarioNoTieneWorkspaces()
    {
        // Arrange
        _workspaceRepository.FindDefaultForUserAsync(UserId, Arg.Any<CancellationToken>())
            .Returns((Workspace?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(UserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Deberia_DevolverWorkspacePorDefecto_Cuando_SesionNoTraeContexto()
    {
        // Arrange
        var workspace = Workspace.Create(UserId, "Finca El Olivar");
        _workspaceRepository.FindDefaultForUserAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(workspace);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(UserId);

        // Assert
        result!.Id.Should().Be(workspace.Id);
        result.Name.Should().Be("Finca El Olivar");
    }

    [Fact]
    public async Task Deberia_RespetarWorkspaceDeLaSesion_Cuando_LaMembresiaSigueActiva()
    {
        // Arrange
        var workspace = Workspace.Create(UserId, "Finca El Olivar");
        _workspaceRepository.FindForMemberAsync(workspace.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(workspace);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(UserId, workspace.Id);

        // Assert
        result!.Id.Should().Be(workspace.Id);
        await _workspaceRepository.DidNotReceive()
            .FindDefaultForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_CaerAlWorkspacePorDefecto_Cuando_ElWorkspaceDeLaSesionYaNoEsAccesible()
    {
        // Arrange
        var workspaceInaccesible = Guid.NewGuid();
        var workspacePorDefecto = Workspace.Create(UserId, "Finca El Olivar");
        _workspaceRepository.FindForMemberAsync(workspaceInaccesible, UserId, Arg.Any<CancellationToken>())
            .Returns((Workspace?)null);
        _workspaceRepository.FindDefaultForUserAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(workspacePorDefecto);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(UserId, workspaceInaccesible);

        // Assert
        result!.Id.Should().Be(workspacePorDefecto.Id);
    }
}
