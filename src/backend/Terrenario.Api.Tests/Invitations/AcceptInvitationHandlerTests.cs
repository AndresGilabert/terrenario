using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Invitations;
using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Auth;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Tests.Invitations;

public class AcceptInvitationHandlerTests
{
    private readonly IWorkspaceInvitationRepository _invitationRepository =
        Substitute.For<IWorkspaceInvitationRepository>();
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IInvitationTokenService _tokenService = Substitute.For<IInvitationTokenService>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();

    private static readonly User InvitedUser = User.Create("google-sub", "Vecino", "vecino@ejemplo.com");
    private static readonly Workspace TargetWorkspace = Workspace.Create(Guid.NewGuid(), "Finca El Olivar");

    private AcceptInvitationHandler CreateSut()
    {
        _tokenService.Hash("token-en-claro").Returns("token-hasheado");
        _userRepository.FindByIdAsync(InvitedUser.Id, Arg.Any<CancellationToken>()).Returns(InvitedUser);
        _workspaceRepository.FindByIdAsync(TargetWorkspace.Id, Arg.Any<CancellationToken>())
            .Returns(TargetWorkspace);
        _jwtService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<Guid?>())
            .Returns(new IssuedAccessToken("access-token-con-workspace", 900));

        return new AcceptInvitationHandler(
            _invitationRepository,
            _workspaceRepository,
            _userRepository,
            _tokenService,
            _jwtService);
    }

    private void GivenPendingInvitation(TimeSpan? lifetime = null)
    {
        var invitation = WorkspaceInvitation.Create(
            TargetWorkspace.Id,
            Guid.NewGuid(),
            InvitationChannels.Email,
            InvitedUser.Email,
            "token-hasheado",
            lifetime ?? TimeSpan.FromDays(7));

        _invitationRepository.FindByTokenHashAsync("token-hasheado", Arg.Any<CancellationToken>())
            .Returns(invitation);
    }

    private static AcceptInvitationCommand Command() => new(InvitedUser.Id, "token-en-claro");

    [Fact]
    public async Task Deberia_CrearMembresiaYSituarLaSesion_Cuando_LaInvitacionEsValida()
    {
        // Arrange
        GivenPendingInvitation();
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(Command());

        // Assert — CA-2 y CA-3
        result.Workspace.Id.Should().Be(TargetWorkspace.Id);
        result.Workspace.Name.Should().Be("Finca El Olivar");
        result.AccessToken.Should().Be("access-token-con-workspace");
        result.ExpiresIn.Should().Be(900);
        result.AlreadyMember.Should().BeFalse();
        await _workspaceRepository.Received(1).AddMemberAsync(
            Arg.Is<WorkspaceMember>(m =>
                m.WorkspaceId == TargetWorkspace.Id &&
                m.UserId == InvitedUser.Id &&
                m.Role == WorkspaceRoles.Member &&
                m.IsActive),
            Arg.Any<CancellationToken>());
        await _invitationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _jwtService.Received(1).IssueAccessToken(InvitedUser.Id, "Vecino", TargetWorkspace.Id);
    }

    [Fact]
    public async Task Deberia_MarcarLaInvitacionComoAceptada_Cuando_SeUsa()
    {
        // Arrange
        GivenPendingInvitation();
        var sut = CreateSut();

        // Act
        await sut.HandleAsync(Command());

        // Assert
        var invitation = await _invitationRepository
            .FindByTokenHashAsync("token-hasheado", CancellationToken.None);
        invitation!.Status.Should().Be(InvitationStatuses.Accepted);
        invitation.AcceptedByUserId.Should().Be(InvitedUser.Id);
    }

    [Fact]
    public async Task Deberia_NoDuplicarMembresia_Cuando_ElUsuarioYaEraMiembro()
    {
        // Arrange
        GivenPendingInvitation();
        _workspaceRepository
            .HasActiveMembershipAsync(TargetWorkspace.Id, InvitedUser.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(Command());

        // Assert
        result.AlreadyMember.Should().BeTrue();
        await _workspaceRepository.DidNotReceive()
            .AddMemberAsync(Arg.Any<WorkspaceMember>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_FallarConInvitacionNoEncontrada_Cuando_ElTokenNoExiste()
    {
        // Arrange
        _invitationRepository.FindByTokenHashAsync("token-hasheado", Arg.Any<CancellationToken>())
            .Returns((WorkspaceInvitation?)null);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(Command());

        // Assert
        (await act.Should().ThrowAsync<InvitationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
        await _workspaceRepository.DidNotReceive()
            .AddMemberAsync(Arg.Any<WorkspaceMember>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_FallarConInvitacionCaducada_Cuando_HaPasadoElPlazo()
    {
        // Arrange
        GivenPendingInvitation(TimeSpan.Zero);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(Command());

        // Assert
        (await act.Should().ThrowAsync<InvitationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleInvitationExpired);
        await _invitationRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_FallarConWorkspaceNoEncontrado_Cuando_ElWorkspaceYaNoExiste()
    {
        // Arrange
        GivenPendingInvitation();
        var sut = CreateSut();
        _workspaceRepository.FindByIdAsync(TargetWorkspace.Id, Arg.Any<CancellationToken>())
            .Returns((Workspace?)null);

        // Act
        var act = async () => await sut.HandleAsync(Command());

        // Assert
        (await act.Should().ThrowAsync<InvitationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.WorkspaceNotFound);
    }
}
