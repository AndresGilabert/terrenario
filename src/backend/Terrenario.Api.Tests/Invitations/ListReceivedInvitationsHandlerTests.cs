using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Invitations;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Invitations;

public class ListReceivedInvitationsHandlerTests
{
    private readonly IWorkspaceInvitationRepository _invitationRepository =
        Substitute.For<IWorkspaceInvitationRepository>();
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private static readonly User Recipient = User.Create("google-sub", "Vecina", "Vecina@Ejemplo.com");
    private static readonly Workspace TargetWorkspace = Workspace.Create(Guid.NewGuid(), "Finca El Olivar");

    private ListReceivedInvitationsHandler CreateSut()
    {
        _userRepository.FindByIdAsync(Recipient.Id, Arg.Any<CancellationToken>()).Returns(Recipient);
        _workspaceRepository.FindByIdAsync(TargetWorkspace.Id, Arg.Any<CancellationToken>())
            .Returns(TargetWorkspace);
        return new ListReceivedInvitationsHandler(_invitationRepository, _workspaceRepository, _userRepository);
    }

    private WorkspaceInvitation ReceivedInvitation(TimeSpan? lifetime = null)
        => WorkspaceInvitation.Create(
            TargetWorkspace.Id, Guid.NewGuid(), InvitationChannels.Email, "vecina@ejemplo.com",
            "hash", lifetime ?? TimeSpan.FromDays(7));

    [Fact]
    public async Task Deberia_ConsultarPorEmailCanonico_Cuando_ListaLasRecibidas()
    {
        // Arrange — el email de Google llega con mayúsculas; la bandeja consulta en minúsculas
        _invitationRepository.ListReceivedPendingAsync("vecina@ejemplo.com", Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceInvitation> { ReceivedInvitation() });
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(Recipient.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].Workspace.Name.Should().Be("Finca El Olivar");
        await _invitationRepository.Received(1)
            .ListReceivedPendingAsync("vecina@ejemplo.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_ExcluirCaducadas_Cuando_ListaLasRecibidas()
    {
        // Arrange
        _invitationRepository.ListReceivedPendingAsync("vecina@ejemplo.com", Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceInvitation> { ReceivedInvitation(TimeSpan.Zero) });
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(Recipient.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Deberia_ExcluirWorkspacesDeLosQueYaEsMiembro_Cuando_ListaLasRecibidas()
    {
        // Arrange
        _invitationRepository.ListReceivedPendingAsync("vecina@ejemplo.com", Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceInvitation> { ReceivedInvitation() });
        _workspaceRepository.HasActiveMembershipAsync(TargetWorkspace.Id, Recipient.Id, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(Recipient.Id);

        // Assert
        result.Should().BeEmpty();
    }
}
