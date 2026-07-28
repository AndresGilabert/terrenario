using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

public class ActiveWorkspaceResolverTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private ActiveWorkspaceResolver CreateSut() => new(_workspaceRepository, _userRepository);

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
    public async Task Deberia_UsarLaPreferenciaPersistida_Cuando_LaSesionNoTraeContexto()
    {
        // Arrange — el claim no viaja (login/refresh), pero el usuario tenía un Workspace activo
        var preferido = Workspace.Create(UserId, "Finca La Vega");
        var user = User.Create("google-sub", "Antonio", "antonio@ejemplo.com");
        user.SetActiveWorkspace(preferido.Id);

        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _workspaceRepository.FindForMemberAsync(preferido.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(preferido);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(UserId);

        // Assert — CA-3: el contexto elegido sobrevive a la renovación de sesión
        result!.Id.Should().Be(preferido.Id);
        await _workspaceRepository.DidNotReceive()
            .FindDefaultForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_IgnorarPreferenciaRevocada_Cuando_YaNoHayMembresiaActiva()
    {
        // Arrange — la preferencia apunta a un Workspace del que ya no se es miembro activo
        var preferidoInaccesible = Guid.NewGuid();
        var porDefecto = Workspace.Create(UserId, "Finca El Olivar");
        var user = User.Create("google-sub", "Antonio", "antonio@ejemplo.com");
        user.SetActiveWorkspace(preferidoInaccesible);

        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _workspaceRepository.FindForMemberAsync(preferidoInaccesible, UserId, Arg.Any<CancellationToken>())
            .Returns((Workspace?)null);
        _workspaceRepository.FindDefaultForUserAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(porDefecto);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(UserId);

        // Assert
        result!.Id.Should().Be(porDefecto.Id);
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
