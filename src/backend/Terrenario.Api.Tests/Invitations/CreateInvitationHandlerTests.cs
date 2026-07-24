using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Terrenario.Api.Application.Invitations;
using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Tests.Invitations;

public class CreateInvitationHandlerTests
{
    private readonly IWorkspaceInvitationRepository _invitationRepository =
        Substitute.For<IWorkspaceInvitationRepository>();
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IInvitationTokenService _tokenService = Substitute.For<IInvitationTokenService>();
    private readonly IInvitationEmailSender _emailSender = Substitute.For<IInvitationEmailSender>();

    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid InviterId = Guid.NewGuid();

    private CreateInvitationHandler CreateSut()
    {
        _tokenService.Generate().Returns(new InvitationToken("token-en-claro", "token-hasheado"));

        return new CreateInvitationHandler(
            _invitationRepository,
            _workspaceRepository,
            _userRepository,
            _tokenService,
            _emailSender,
            Options.Create(new InvitationOptions
            {
                LifetimeDays = 7,
                AcceptBaseUrl = "http://localhost:5173/invitations"
            }),
            Substitute.For<ILogger<CreateInvitationHandler>>());
    }

    private static CreateInvitationCommand CommandFor(string channel, string? email = null) =>
        new(WorkspaceId, "Finca El Olivar", InviterId, "Antonio", channel, email);

    [Fact]
    public async Task Deberia_PersistirInvitacionConTokenHasheado_Cuando_CanalEsEnlace()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(CommandFor(InvitationChannels.Link));

        // Assert — CA-1: el enlace en claro solo viaja en la respuesta
        result.AcceptUrl.Should().Be("http://localhost:5173/invitations/token-en-claro");
        result.Status.Should().Be(InvitationStatuses.Pending);
        result.Email.Should().BeNull();
        result.EmailSent.Should().BeFalse();
        await _invitationRepository.Received(1).AddAsync(
            Arg.Is<WorkspaceInvitation>(i =>
                i.WorkspaceId == WorkspaceId &&
                i.InvitedByUserId == InviterId &&
                i.TokenHash == "token-hasheado" &&
                i.Channel == InvitationChannels.Link),
            Arg.Any<CancellationToken>());
        await _invitationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<InvitationEmail>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_EnviarElEnlacePorEmail_Cuando_CanalEsEmail()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(CommandFor(InvitationChannels.Email, "Vecino@Ejemplo.com"));

        // Assert
        result.Email.Should().Be("vecino@ejemplo.com");
        result.EmailSent.Should().BeTrue();
        await _emailSender.Received(1).SendAsync(
            Arg.Is<InvitationEmail>(message =>
                message.ToEmail == "vecino@ejemplo.com" &&
                message.WorkspaceName == "Finca El Olivar" &&
                message.InviterDisplayName == "Antonio" &&
                message.AcceptUrl == "http://localhost:5173/invitations/token-en-claro"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_MantenerLaInvitacion_Cuando_ElProveedorDeEmailFalla()
    {
        // Arrange — la invitación ya está emitida; quien invita puede compartir el enlace a mano
        _emailSender.SendAsync(Arg.Any<InvitationEmail>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("proveedor caído"));

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(CommandFor(InvitationChannels.Email, "vecino@ejemplo.com"));

        // Assert
        result.EmailSent.Should().BeFalse();
        result.AcceptUrl.Should().Be("http://localhost:5173/invitations/token-en-claro");
        await _invitationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RechazarInvitacion_Cuando_ElEmailYaEsMiembroDelWorkspace()
    {
        // Arrange
        var existingUser = User.Create("google-sub", "Vecino", "vecino@ejemplo.com");
        _userRepository.FindByEmailAsync("vecino@ejemplo.com", Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _workspaceRepository.HasActiveMembershipAsync(WorkspaceId, existingUser.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(CommandFor(InvitationChannels.Email, "vecino@ejemplo.com"));

        // Assert
        (await act.Should().ThrowAsync<InvitationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleInvitationAlreadyMember);
        await _invitationRepository.DidNotReceive()
            .AddAsync(Arg.Any<WorkspaceInvitation>(), Arg.Any<CancellationToken>());
        await _invitationRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_NoPersistirNada_Cuando_ElCanalNoEsValido()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(CommandFor("paloma-mensajera"));

        // Assert
        (await act.Should().ThrowAsync<InvitationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationInvitationChannelInvalid);
        await _invitationRepository.DidNotReceive()
            .AddAsync(Arg.Any<WorkspaceInvitation>(), Arg.Any<CancellationToken>());
    }
}
