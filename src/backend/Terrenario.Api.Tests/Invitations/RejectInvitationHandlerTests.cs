using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Invitations;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Tests.Invitations;

public class RejectInvitationHandlerTests
{
    private readonly IWorkspaceInvitationRepository _invitationRepository =
        Substitute.For<IWorkspaceInvitationRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IInvitationTokenService _tokenService = Substitute.For<IInvitationTokenService>();

    private static readonly User InvitedUser = User.Create("google-sub", "Vecino", "vecino@ejemplo.com");
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private RejectInvitationHandler CreateSut()
    {
        _tokenService.Hash("token-en-claro").Returns("token-hasheado");
        _userRepository.FindByIdAsync(InvitedUser.Id, Arg.Any<CancellationToken>()).Returns(InvitedUser);
        return new RejectInvitationHandler(_invitationRepository, _userRepository, _tokenService);
    }

    private WorkspaceInvitation GivenEmailInvitation(string email = "vecino@ejemplo.com")
    {
        var invitation = WorkspaceInvitation.Create(
            WorkspaceId, Guid.NewGuid(), InvitationChannels.Email, email, "token-hasheado", TimeSpan.FromDays(7));
        _invitationRepository.FindByTokenHashAsync("token-hasheado", Arg.Any<CancellationToken>())
            .Returns(invitation);
        _invitationRepository.FindByIdAsync(invitation.Id, Arg.Any<CancellationToken>()).Returns(invitation);
        return invitation;
    }

    [Fact]
    public async Task Deberia_MarcarComoRechazadaYGuardar_Cuando_SeRechazaPorToken()
    {
        // Arrange
        var invitation = GivenEmailInvitation();
        var sut = CreateSut();

        // Act
        await sut.HandleByTokenAsync(InvitedUser.Id, "token-en-claro");

        // Assert
        invitation.Status.Should().Be(InvitationStatuses.Rejected);
        invitation.RejectedByUserId.Should().Be(InvitedUser.Id);
        await _invitationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_MarcarComoRechazadaYGuardar_Cuando_SeRechazaPorIdDesdeLaBandeja()
    {
        // Arrange
        var invitation = GivenEmailInvitation();
        var sut = CreateSut();

        // Act
        await sut.HandleByIdAsync(InvitedUser.Id, invitation.Id);

        // Assert
        invitation.Status.Should().Be(InvitationStatuses.Rejected);
        await _invitationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_FallarConNoEncontrada_Cuando_ElTokenNoExiste()
    {
        // Arrange
        _invitationRepository.FindByTokenHashAsync("token-hasheado", Arg.Any<CancellationToken>())
            .Returns((WorkspaceInvitation?)null);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleByTokenAsync(InvitedUser.Id, "token-en-claro");

        // Assert
        (await act.Should().ThrowAsync<InvitationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
        await _invitationRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_OcultarLaInvitacion_Cuando_PorIdNoVaDirigidaAEstaCuenta()
    {
        // Arrange — la bandeja se autoriza por titularidad del email; si no coincide, es "inexistente"
        var invitation = GivenEmailInvitation("otra@ejemplo.com");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleByIdAsync(InvitedUser.Id, invitation.Id);

        // Assert
        (await act.Should().ThrowAsync<InvitationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
        await _invitationRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_OcultarLaInvitacion_Cuando_PorIdEsDeCanalEnlace()
    {
        // Arrange — el enlace no tiene destinatario: no forma parte de ninguna bandeja de recibidas
        var invitation = WorkspaceInvitation.Create(
            WorkspaceId, Guid.NewGuid(), InvitationChannels.Link, null, "hash-enlace", TimeSpan.FromDays(7));
        _invitationRepository.FindByIdAsync(invitation.Id, Arg.Any<CancellationToken>()).Returns(invitation);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleByIdAsync(InvitedUser.Id, invitation.Id);

        // Assert
        (await act.Should().ThrowAsync<InvitationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
    }
}
