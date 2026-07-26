using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Invitations;
using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Tests.Invitations;

public class PreviewInvitationHandlerTests
{
    private readonly IWorkspaceInvitationRepository _invitationRepository =
        Substitute.For<IWorkspaceInvitationRepository>();
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IInvitationTokenService _tokenService = Substitute.For<IInvitationTokenService>();

    private static readonly User Viewer = User.Create("google-sub", "Vecino", "vecino@ejemplo.com");
    private static readonly Workspace TargetWorkspace = Workspace.Create(Guid.NewGuid(), "Finca El Olivar");

    private PreviewInvitationHandler CreateSut()
    {
        _tokenService.Hash("token-en-claro").Returns("token-hasheado");
        _userRepository.FindByIdAsync(Viewer.Id, Arg.Any<CancellationToken>()).Returns(Viewer);
        _workspaceRepository.FindByIdAsync(TargetWorkspace.Id, Arg.Any<CancellationToken>())
            .Returns(TargetWorkspace);
        return new PreviewInvitationHandler(
            _invitationRepository, _workspaceRepository, _userRepository, _tokenService);
    }

    private void GivenInvitation(WorkspaceInvitation invitation)
        => _invitationRepository.FindByTokenHashAsync("token-hasheado", Arg.Any<CancellationToken>())
            .Returns(invitation);

    private static WorkspaceInvitation EmailInvitation(string email, TimeSpan? lifetime = null)
        => WorkspaceInvitation.Create(
            TargetWorkspace.Id, Guid.NewGuid(), InvitationChannels.Email, email,
            "token-hasheado", lifetime ?? TimeSpan.FromDays(7));

    [Fact]
    public async Task Deberia_MarcarApta_Cuando_LaInvitacionVaDirigidaAEstaCuenta()
    {
        // Arrange
        GivenInvitation(EmailInvitation("vecino@ejemplo.com"));
        var sut = CreateSut();

        // Act
        var preview = await sut.HandleAsync("token-en-claro", Viewer.Id);

        // Assert
        preview.ViewerCanAccept.Should().BeTrue();
        preview.ViewerReason.Should().BeNull();
    }

    [Fact]
    public async Task Deberia_InformarDesajusteDeEmail_SinRevelarDestinatario_Cuando_EsDeOtraCuenta()
    {
        // Arrange — R-C: se anticipa el 403 en lugar de dispararlo tras pulsar
        GivenInvitation(EmailInvitation("otra@ejemplo.com"));
        var sut = CreateSut();

        // Act
        var preview = await sut.HandleAsync("token-en-claro", Viewer.Id);

        // Assert
        preview.ViewerCanAccept.Should().BeFalse();
        preview.ViewerReason.Should().Be(InvitationViewerReasons.EmailMismatch);
    }

    [Fact]
    public async Task Deberia_InformarCaducada_Cuando_LaInvitacionVencio()
    {
        // Arrange
        GivenInvitation(EmailInvitation("vecino@ejemplo.com", TimeSpan.Zero));
        var sut = CreateSut();

        // Act
        var preview = await sut.HandleAsync("token-en-claro", Viewer.Id);

        // Assert
        preview.ViewerCanAccept.Should().BeFalse();
        preview.ViewerReason.Should().Be(InvitationViewerReasons.Expired);
    }

    [Fact]
    public async Task Deberia_InformarYaMiembro_PeroApta_Cuando_ElUsuarioYaPertenece()
    {
        // Arrange — aceptar sigue siendo válido (idempotente): apta con motivo informativo
        GivenInvitation(EmailInvitation("vecino@ejemplo.com"));
        _workspaceRepository.HasActiveMembershipAsync(TargetWorkspace.Id, Viewer.Id, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        // Act
        var preview = await sut.HandleAsync("token-en-claro", Viewer.Id);

        // Assert
        preview.ViewerCanAccept.Should().BeTrue();
        preview.ViewerReason.Should().Be(InvitationViewerReasons.AlreadyMember);
    }
}
