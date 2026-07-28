using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Invitations;
using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Invitations;

/// <summary>
/// Tests de la anulación de invitación (MVP-207, HU-2/CA-4): retira una invitación pendiente desde el
/// Workspace emisor, dejándola inservible sin esperar a que caduque, y oculta como 404 todo lo que no
/// sea una invitación pendiente del Workspace activo.
/// </summary>
public class CancelInvitationHandlerTests
{
    private readonly IWorkspaceInvitationRepository _invitationRepository =
        Substitute.For<IWorkspaceInvitationRepository>();

    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid ActingUserId = Guid.NewGuid();

    private CancelInvitationHandler CreateSut() => new(_invitationRepository);

    private static WorkspaceInvitation PendingInvitation(
        string channel = InvitationChannels.Email,
        Guid? workspaceId = null) => WorkspaceInvitation.Create(
            workspaceId ?? WorkspaceId,
            ActingUserId,
            channel,
            channel == InvitationChannels.Email ? "vecino@ejemplo.com" : null,
            "hash",
            TimeSpan.FromDays(7));

    private void Seed(WorkspaceInvitation invitation) =>
        _invitationRepository.FindByIdAsync(invitation.Id, Arg.Any<CancellationToken>()).Returns(invitation);

    [Fact]
    public async Task Deberia_AnularLaInvitacionPendiente_Y_Persistir()
    {
        // CA-4 — se invitó al email equivocado: la invitación deja de ser aceptable.
        var invitation = PendingInvitation();
        Seed(invitation);
        var sut = CreateSut();

        await sut.HandleAsync(new CancelInvitationCommand(WorkspaceId, ActingUserId, invitation.Id));

        invitation.Status.Should().Be(InvitationStatuses.Cancelled);
        invitation.CancelledByUserId.Should().Be(ActingUserId);
        invitation.CancelledAt.Should().NotBeNull();
        await _invitationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_AnularTambienLasDeCanalEnlace()
    {
        // A diferencia del reenvío, la anulación no se limita al canal email: un enlace compartible
        // que se ha ido de las manos es justo el caso en el que hace falta retirarlo.
        var invitation = PendingInvitation(InvitationChannels.Link);
        Seed(invitation);
        var sut = CreateSut();

        await sut.HandleAsync(new CancelInvitationCommand(WorkspaceId, ActingUserId, invitation.Id));

        invitation.Status.Should().Be(InvitationStatuses.Cancelled);
    }

    [Fact]
    public async Task Deberia_Ocultar_Como404_LaInvitacionDeOtroWorkspace()
    {
        // No se revela el estado de invitaciones ajenas (mismo criterio que el reenvío).
        var invitation = PendingInvitation(workspaceId: Guid.NewGuid());
        Seed(invitation);
        var sut = CreateSut();

        var act = () => sut.HandleAsync(new CancelInvitationCommand(WorkspaceId, ActingUserId, invitation.Id));

        (await act.Should().ThrowAsync<InvitationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
        invitation.Status.Should().Be(InvitationStatuses.Pending);
        await _invitationRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Ocultar_Como404_LaInvitacionInexistente()
    {
        var invitationId = Guid.NewGuid();
        _invitationRepository.FindByIdAsync(invitationId, Arg.Any<CancellationToken>())
            .Returns((WorkspaceInvitation?)null);
        var sut = CreateSut();

        var act = () => sut.HandleAsync(new CancelInvitationCommand(WorkspaceId, ActingUserId, invitationId));

        (await act.Should().ThrowAsync<InvitationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
    }

    [Fact]
    public async Task Deberia_Ocultar_Como404_LaInvitacionYaAceptada()
    {
        // Una invitación aceptada ya creó membresía: se deshace revocando el acceso (MVP-204, CA-7),
        // no anulando el enlace.
        var invitation = PendingInvitation();
        invitation.Accept(Guid.NewGuid(), "vecino@ejemplo.com", DateTimeOffset.UtcNow);
        Seed(invitation);
        var sut = CreateSut();

        var act = () => sut.HandleAsync(new CancelInvitationCommand(WorkspaceId, ActingUserId, invitation.Id));

        (await act.Should().ThrowAsync<InvitationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
        invitation.Status.Should().Be(InvitationStatuses.Accepted);
    }
}
