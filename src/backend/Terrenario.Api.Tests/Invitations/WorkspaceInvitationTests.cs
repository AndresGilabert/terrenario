using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Invitations;

public class WorkspaceInvitationTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid InviterId = Guid.NewGuid();
    private static readonly TimeSpan SevenDays = TimeSpan.FromDays(7);

    private static WorkspaceInvitation CreateEmailInvitation(string email = "antonio@ejemplo.com") =>
        WorkspaceInvitation.Create(WorkspaceId, InviterId, InvitationChannels.Email, email, "hash", SevenDays);

    private static WorkspaceInvitation CreateLinkInvitation() =>
        WorkspaceInvitation.Create(WorkspaceId, InviterId, InvitationChannels.Link, null, "hash", SevenDays);

    [Fact]
    public void Deberia_CrearInvitacionPendiente_Cuando_CanalEsEmailYDatosSonValidos()
    {
        // Act
        var invitation = CreateEmailInvitation("  Antonio@Ejemplo.com  ");

        // Assert
        invitation.Id.Should().NotBeEmpty();
        invitation.WorkspaceId.Should().Be(WorkspaceId);
        invitation.InvitedByUserId.Should().Be(InviterId);
        invitation.Channel.Should().Be(InvitationChannels.Email);
        invitation.Email.Should().Be("antonio@ejemplo.com");
        invitation.Status.Should().Be(InvitationStatuses.Pending);
        invitation.ExpiresAt.Should().BeAfter(invitation.CreatedAt);
        invitation.AcceptedAt.Should().BeNull();
        invitation.AcceptedByUserId.Should().BeNull();
    }

    [Fact]
    public void Deberia_NoGuardarDestinatario_Cuando_CanalEsEnlace()
    {
        // Act — el enlace compartible no va dirigido a nadie en concreto
        var invitation = WorkspaceInvitation.Create(
            WorkspaceId, InviterId, InvitationChannels.Link, "antonio@ejemplo.com", "hash", SevenDays);

        // Assert
        invitation.Email.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deberia_RechazarInvitacion_Cuando_CanalEsEmailYFaltaElEmail(string? email)
    {
        // Act
        var act = () => CreateEmailInvitation(email!);

        // Assert
        act.Should().Throw<InvitationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredInvitationEmail);
    }

    [Theory]
    [InlineData("no-es-un-email")]
    [InlineData("antonio@localhost")]
    [InlineData("antonio@@ejemplo.com")]
    public void Deberia_RechazarInvitacion_Cuando_ElEmailNoTieneFormatoValido(string email)
    {
        // Act
        var act = () => CreateEmailInvitation(email);

        // Assert
        act.Should().Throw<InvitationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationInvitationEmailInvalid);
    }

    [Fact]
    public void Deberia_RechazarInvitacion_Cuando_ElCanalNoEstaEnElCatalogo()
    {
        // Act
        var act = () => WorkspaceInvitation.Create(
            WorkspaceId, InviterId, "whatsapp", null, "hash", SevenDays);

        // Assert
        act.Should().Throw<InvitationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationInvitationChannelInvalid);
    }

    [Fact]
    public void Deberia_MarcarComoAceptada_Cuando_LaAceptaLaPersonaInvitada()
    {
        // Arrange
        var invitation = CreateEmailInvitation();
        var userId = Guid.NewGuid();
        var moment = DateTimeOffset.UtcNow;

        // Act
        invitation.Accept(userId, "ANTONIO@ejemplo.com", moment);

        // Assert
        invitation.Status.Should().Be(InvitationStatuses.Accepted);
        invitation.AcceptedByUserId.Should().Be(userId);
        invitation.AcceptedAt.Should().Be(moment);
    }

    [Fact]
    public void Deberia_AceptarDeCualquierCuenta_Cuando_ElCanalEsEnlace()
    {
        // Arrange
        var invitation = CreateLinkInvitation();

        // Act
        var act = () => invitation.Accept(Guid.NewGuid(), "cualquiera@ejemplo.com", DateTimeOffset.UtcNow);

        // Assert
        act.Should().NotThrow();
        invitation.Status.Should().Be(InvitationStatuses.Accepted);
    }

    [Fact]
    public void Deberia_RechazarAceptacion_Cuando_LaInvitacionPorEmailEsDeOtraCuenta()
    {
        // Arrange — reenviar el correo no debe abrir la puerta a un tercero
        var invitation = CreateEmailInvitation();

        // Act
        var act = () => invitation.Accept(Guid.NewGuid(), "otra@ejemplo.com", DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<InvitationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.AuthInvitationEmailMismatch);
        invitation.Status.Should().Be(InvitationStatuses.Pending);
    }

    [Fact]
    public void Deberia_RechazarAceptacion_Cuando_LaInvitacionHaCaducado()
    {
        // Arrange
        var invitation = CreateLinkInvitation();

        // Act
        var act = () => invitation.Accept(Guid.NewGuid(), "antonio@ejemplo.com", invitation.ExpiresAt);

        // Assert
        act.Should().Throw<InvitationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleInvitationExpired);
        invitation.Status.Should().Be(InvitationStatuses.Pending);
    }

    [Fact]
    public void Deberia_RechazarAceptacion_Cuando_LaInvitacionYaSeUso()
    {
        // Arrange
        var invitation = CreateLinkInvitation();
        invitation.Accept(Guid.NewGuid(), "antonio@ejemplo.com", DateTimeOffset.UtcNow);

        // Act
        var act = () => invitation.Accept(Guid.NewGuid(), "otro@ejemplo.com", DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<InvitationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleInvitationAlreadyAccepted);
    }

    // --- MVP-107 -------------------------------------------------------------------------------

    [Fact]
    public void Deberia_MarcarComoRechazada_Cuando_LaDeclinaLaPersonaInvitada()
    {
        // Arrange
        var invitation = CreateEmailInvitation();
        var userId = Guid.NewGuid();
        var moment = DateTimeOffset.UtcNow;

        // Act
        invitation.Reject(userId, "ANTONIO@ejemplo.com", moment);

        // Assert
        invitation.Status.Should().Be(InvitationStatuses.Rejected);
        invitation.RejectedByUserId.Should().Be(userId);
        invitation.RejectedAt.Should().Be(moment);
    }

    [Fact]
    public void Deberia_SerIdempotente_Cuando_SeRechazaDosVecesLaMismaCuenta()
    {
        // Arrange — doble clic en "Rechazar" no debe reventar
        var invitation = CreateEmailInvitation();
        invitation.Reject(Guid.NewGuid(), "antonio@ejemplo.com", DateTimeOffset.UtcNow);

        // Act
        var act = () => invitation.Reject(Guid.NewGuid(), "antonio@ejemplo.com", DateTimeOffset.UtcNow);

        // Assert
        act.Should().NotThrow();
        invitation.Status.Should().Be(InvitationStatuses.Rejected);
    }

    [Fact]
    public void Deberia_RechazarElRechazo_Cuando_LaInvitacionPorEmailEsDeOtraCuenta()
    {
        // Arrange — un tercero con el correo reenviado no declina la invitación de otra persona
        var invitation = CreateEmailInvitation();

        // Act
        var act = () => invitation.Reject(Guid.NewGuid(), "otra@ejemplo.com", DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<InvitationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.AuthInvitationEmailMismatch);
        invitation.Status.Should().Be(InvitationStatuses.Pending);
    }

    [Fact]
    public void Deberia_ImpedirRechazar_Cuando_LaInvitacionYaSeAcepto()
    {
        // Arrange
        var invitation = CreateEmailInvitation();
        invitation.Accept(Guid.NewGuid(), "antonio@ejemplo.com", DateTimeOffset.UtcNow);

        // Act
        var act = () => invitation.Reject(Guid.NewGuid(), "antonio@ejemplo.com", DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<InvitationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleInvitationAlreadyAccepted);
    }

    [Fact]
    public void Deberia_ImpedirAceptar_Cuando_LaInvitacionYaSeRechazo()
    {
        // Arrange
        var invitation = CreateEmailInvitation();
        invitation.Reject(Guid.NewGuid(), "antonio@ejemplo.com", DateTimeOffset.UtcNow);

        // Act
        var act = () => invitation.Accept(Guid.NewGuid(), "antonio@ejemplo.com", DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<InvitationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleInvitationAlreadyRejected);
    }

    [Fact]
    public void Deberia_PermitirRechazarCaducada_Para_LimpiarLaBandeja()
    {
        // Arrange — rechazar una invitación vencida no tiene efecto colateral y limpia la bandeja
        var invitation = CreateEmailInvitation();

        // Act
        var act = () => invitation.Reject(Guid.NewGuid(), "antonio@ejemplo.com", invitation.ExpiresAt);

        // Assert
        act.Should().NotThrow();
        invitation.Status.Should().Be(InvitationStatuses.Rejected);
    }

    [Theory]
    [InlineData(InvitationChannels.Email, "antonio@ejemplo.com", true)]
    [InlineData(InvitationChannels.Email, "otra@ejemplo.com", false)]
    public void Deberia_IdentificarDestinatario_SegunCanalYEmail(string channel, string email, bool expected)
    {
        // Arrange
        var invitation = channel == InvitationChannels.Email ? CreateEmailInvitation() : CreateLinkInvitation();

        // Act & Assert
        invitation.IsAddressedTo(email).Should().Be(expected);
    }

    [Fact]
    public void Deberia_AceptarCualquierEmail_ParaAptitud_Cuando_ElCanalEsEnlace()
    {
        // Arrange — el enlace no va dirigido a nadie: cualquiera es "destinatario" apto
        var invitation = CreateLinkInvitation();

        // Act & Assert
        invitation.IsAddressedTo("cualquiera@ejemplo.com").Should().BeTrue();
    }

    [Fact]
    public void Deberia_ImpedirAceptar_Cuando_LaInvitacionEstaAnulada()
    {
        // MVP-207 (CA-4) — anulada por el Workspace emisor: el enlace deja de permitir la aceptación.
        var invitation = CreateEmailInvitation();
        invitation.Cancel(InviterId, DateTimeOffset.UtcNow);

        var act = () => invitation.Accept(Guid.NewGuid(), "antonio@ejemplo.com", DateTimeOffset.UtcNow);

        act.Should().Throw<InvitationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleInvitationCancelled);
        invitation.Status.Should().Be(InvitationStatuses.Cancelled);
    }

    [Fact]
    public void Deberia_ImpedirRechazar_Cuando_LaInvitacionEstaAnulada()
    {
        // El rechazo no debe sobrescribir el estado que fijó el Workspace emisor.
        var invitation = CreateEmailInvitation();
        invitation.Cancel(InviterId, DateTimeOffset.UtcNow);

        var act = () => invitation.Reject(Guid.NewGuid(), "antonio@ejemplo.com", DateTimeOffset.UtcNow);

        act.Should().Throw<InvitationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleInvitationCancelled);
        invitation.Status.Should().Be(InvitationStatuses.Cancelled);
    }

    [Fact]
    public void Deberia_ImpedirAnular_Cuando_LaInvitacionYaSeAcepto()
    {
        // Una invitación aceptada ya creó membresía: se deshace revocando el acceso, no anulándola.
        var invitation = CreateEmailInvitation();
        invitation.Accept(Guid.NewGuid(), "antonio@ejemplo.com", DateTimeOffset.UtcNow);

        var act = () => invitation.Cancel(InviterId, DateTimeOffset.UtcNow);

        act.Should().Throw<InvitationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleInvitationAlreadyAccepted);
    }

    [Fact]
    public void Deberia_SerIdempotente_AnteUnaSegundaAnulacion()
    {
        // Doble clic en «Anular»: no vuelve a marcar ni cambia quién la anuló.
        var invitation = CreateEmailInvitation();
        invitation.Cancel(InviterId, DateTimeOffset.UtcNow);
        var firstMoment = invitation.CancelledAt;

        invitation.Cancel(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(1));

        invitation.Status.Should().Be(InvitationStatuses.Cancelled);
        invitation.CancelledAt.Should().Be(firstMoment);
        invitation.CancelledByUserId.Should().Be(InviterId);
    }

    [Fact]
    public void Deberia_PermitirAnularUnaInvitacionCaducada()
    {
        // Anular una caducada retira de la lista de personas a alguien que ya no iba a entrar.
        var invitation = WorkspaceInvitation.Create(
            WorkspaceId, InviterId, InvitationChannels.Email, "antonio@ejemplo.com", "hash", TimeSpan.FromDays(-1));

        invitation.Cancel(InviterId, DateTimeOffset.UtcNow);

        invitation.Status.Should().Be(InvitationStatuses.Cancelled);
    }
}
