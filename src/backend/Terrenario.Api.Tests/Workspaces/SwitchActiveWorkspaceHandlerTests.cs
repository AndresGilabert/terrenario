using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Tests.Workspaces;

public class SwitchActiveWorkspaceHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();

    private static readonly User Member = User.Create("google-sub", "Antonio", "antonio@ejemplo.com");
    private static readonly Workspace Target = Workspace.Create(Guid.NewGuid(), "Finca El Olivar");

    private SwitchActiveWorkspaceHandler CreateSut() =>
        new(_workspaceRepository, _userRepository, _jwtService);

    private static SwitchActiveWorkspaceCommand Command() =>
        new(Member.Id, "Antonio", Target.Id);

    [Fact]
    public async Task Deberia_ReemitirLaSesionEnElNuevoContexto_Cuando_HayMembresiaActiva()
    {
        // Arrange
        _workspaceRepository.FindForMemberAsync(Target.Id, Member.Id, Arg.Any<CancellationToken>())
            .Returns(Target);
        _userRepository.FindByIdAsync(Member.Id, Arg.Any<CancellationToken>()).Returns(Member);
        _jwtService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<Guid?>())
            .Returns(new IssuedAccessToken("access-token-nuevo-contexto", 900));

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(Command());

        // Assert — CA-2: la sesión queda situada en el Workspace elegido
        result.Workspace.Id.Should().Be(Target.Id);
        result.AccessToken.Should().Be("access-token-nuevo-contexto");
        _jwtService.Received(1).IssueAccessToken(Member.Id, "Antonio", Target.Id);
    }

    [Fact]
    public async Task Deberia_PersistirLaPreferenciaDeWorkspaceActivo_Cuando_SeCambia()
    {
        // Arrange
        _workspaceRepository.FindForMemberAsync(Target.Id, Member.Id, Arg.Any<CancellationToken>())
            .Returns(Target);
        _userRepository.FindByIdAsync(Member.Id, Arg.Any<CancellationToken>()).Returns(Member);
        _jwtService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<Guid?>())
            .Returns(new IssuedAccessToken("access-token", 900));

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(Command());

        // Assert — CA-3: el contexto queda disponible para operaciones posteriores
        Member.ActiveWorkspaceId.Should().Be(Target.Id);
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Rechazar_Cuando_NoHayMembresiaActivaEnElDestino()
    {
        // Arrange — un Workspace ajeno, inexistente o con la membresía revocada resuelven a null
        _workspaceRepository.FindForMemberAsync(Target.Id, Member.Id, Arg.Any<CancellationToken>())
            .Returns((Workspace?)null);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(Command());

        // Assert
        await act.Should().ThrowAsync<WorkspaceAccessDeniedException>();
        await _userRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        _jwtService.DidNotReceive().IssueAccessToken(
            Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<Guid?>());
    }
}
