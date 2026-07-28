using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Terrenario.Api.Application.Invitations;
using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Tests.Invitations;

/// <summary>
/// Tests del reenvío de invitación (MVP-204, HU-5/CA-6). Verifica que rota el token y renueva la
/// caducidad reutilizando el emisor, que distingue reenviar por email de por enlace, y que oculta
/// como 404 las invitaciones no reenviables (otro Workspace o no pendiente).
///
/// MVP-208 (CA-7): el canal <c>enlace</c> ya no es una de las ocultas; renovar un enlace compartible
/// es la misma operación y hacía falta para que la superficie única tenga las mismas acciones en los
/// dos canales.
/// </summary>
public class ResendInvitationHandlerTests
{
    private readonly IWorkspaceInvitationRepository _invitationRepository =
        Substitute.For<IWorkspaceInvitationRepository>();
    private readonly IInvitationTokenService _tokenService = Substitute.For<IInvitationTokenService>();
    private readonly IInvitationEmailSender _emailSender = Substitute.For<IInvitationEmailSender>();

    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid ActingUserId = Guid.NewGuid();

    private ResendInvitationHandler CreateSut()
    {
        _tokenService.Generate().Returns(new InvitationToken("token-nuevo", "hash-nuevo"));
        _emailSender.IsEnabled.Returns(true);

        return new ResendInvitationHandler(
            _invitationRepository,
            _tokenService,
            _emailSender,
            Options.Create(new InvitationOptions
            {
                LifetimeDays = 7,
                AcceptBaseUrl = "http://localhost:5173/invitations"
            }),
            Substitute.For<ILogger<ResendInvitationHandler>>());
    }

    private static WorkspaceInvitation PendingEmailInvitation() => WorkspaceInvitation.Create(
        WorkspaceId, ActingUserId, InvitationChannels.Email, "vecino@ejemplo.com", "hash-viejo", TimeSpan.FromDays(-1));

    private static ResendInvitationCommand CommandFor(Guid invitationId, bool deliverEmail) =>
        new(WorkspaceId, "Finca El Olivar", ActingUserId, "Antonio", invitationId, deliverEmail);

    [Fact]
    public async Task Deberia_RotarTokenYRenovarCaducidad_Y_ReenviarPorEmail()
    {
        // Arrange
        var invitation = PendingEmailInvitation();
        _invitationRepository.FindByIdAsync(invitation.Id, Arg.Any<CancellationToken>()).Returns(invitation);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(CommandFor(invitation.Id, deliverEmail: true));

        // Assert — nuevo enlace de un solo uso, caducidad renovada (ya no está caducada) y correo enviado
        result.AcceptUrl.Should().Be("http://localhost:5173/invitations/token-nuevo");
        result.EmailSent.Should().BeTrue();
        invitation.TokenHash.Should().Be("hash-nuevo");
        invitation.Status.Should().Be(InvitationStatuses.Pending);
        invitation.IsExpiredAt(DateTimeOffset.UtcNow).Should().BeFalse();
        await _invitationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendAsync(
            Arg.Is<InvitationEmail>(m => m.ToEmail == "vecino@ejemplo.com"
                && m.AcceptUrl == "http://localhost:5173/invitations/token-nuevo"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_DevolverEnlaceSinEnviarCorreo_Cuando_ReenvioPorEnlace()
    {
        // Arrange
        var invitation = PendingEmailInvitation();
        _invitationRepository.FindByIdAsync(invitation.Id, Arg.Any<CancellationToken>()).Returns(invitation);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(CommandFor(invitation.Id, deliverEmail: false));

        // Assert
        result.EmailSent.Should().BeFalse();
        result.AcceptUrl.Should().Be("http://localhost:5173/invitations/token-nuevo");
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<InvitationEmail>(), Arg.Any<CancellationToken>());
        await _invitationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Ocultar_Cuando_LaInvitacionEsDeOtroWorkspace()
    {
        // Arrange — pertenece a otro Workspace: se oculta como 404 para no revelar invitaciones ajenas
        var ajena = WorkspaceInvitation.Create(
            Guid.NewGuid(), ActingUserId, InvitationChannels.Email, "ajeno@ejemplo.com", "hash", TimeSpan.FromDays(7));
        _invitationRepository.FindByIdAsync(ajena.Id, Arg.Any<CancellationToken>()).Returns(ajena);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(CommandFor(ajena.Id, deliverEmail: true));

        // Assert
        (await act.Should().ThrowAsync<InvitationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
        await _invitationRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RenovarElEnlaceCompartible_SinEnviarCorreo()
    {
        // MVP-208 (CA-7) — el canal enlace también se reemite: es la otra mitad de la simetría de la
        // superficie única (antes respondía 404 y el enlace solo se podía dejar caducar). No tiene
        // destinatario, así que nunca sale un correo, ni siquiera pidiendo deliver_email.
        var link = WorkspaceInvitation.Create(
            WorkspaceId, ActingUserId, InvitationChannels.Link, null, "hash", TimeSpan.FromDays(7));
        _invitationRepository.FindByIdAsync(link.Id, Arg.Any<CancellationToken>()).Returns(link);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(CommandFor(link.Id, deliverEmail: true));

        // Assert
        result.Channel.Should().Be(InvitationChannels.Link);
        result.Email.Should().BeNull();
        result.EmailSent.Should().BeFalse();
        result.AcceptUrl.Should().NotBeNullOrWhiteSpace();
        link.TokenHash.Should().NotBe("hash");
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<InvitationEmail>(), Arg.Any<CancellationToken>());
        await _invitationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
