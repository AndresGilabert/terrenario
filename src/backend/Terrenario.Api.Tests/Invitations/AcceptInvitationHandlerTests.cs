using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Invitations;
using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Application.Workers;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workers;
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
    // MVP-208 (CA-4): aceptar materializa la fila de responsable de quien entra.
    private readonly IWorkerRepository _workerRepository = Substitute.For<IWorkerRepository>();

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
            new MemberRosterService(_workerRepository),
            _tokenService,
            _jwtService);
    }

    private WorkspaceInvitation GivenPendingInvitation(TimeSpan? lifetime = null)
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
        _invitationRepository.FindByIdAsync(invitation.Id, Arg.Any<CancellationToken>()).Returns(invitation);
        return invitation;
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
    public async Task Deberia_MaterializarLaFilaDeResponsable_Cuando_SeAcepta()
    {
        // MVP-208 (CA-4) — RN-027: entrar al Workspace es aparecer como responsable seleccionable, sin
        // que nadie tenga que darse de alta a mano en el maestro.
        GivenPendingInvitation();
        var sut = CreateSut();

        await sut.HandleAsync(Command());

        await _workerRepository.Received(1).AddAsync(
            Arg.Is<Worker>(w => w.WorkspaceId == TargetWorkspace.Id
                && w.UserAccountId == InvitedUser.Id
                && w.Name == "Vecino"
                && w.IsActive),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RecuperarLaFilaDeResponsable_Cuando_VuelveAlgunRevocado()
    {
        // MVP-208 (CA-4) — quien fue revocado y vuelve por una invitación nueva recupera su fila en vez
        // de duplicarla: es la misma persona y los registros que la referencian siguen valiendo.
        var previa = Worker.CreateForMember(TargetWorkspace.Id, InvitedUser.Id, "Vecino");
        previa.SyncMembership(false);
        _workerRepository.FindByUserAccountAsync(
                TargetWorkspace.Id, InvitedUser.Id, Arg.Any<CancellationToken>())
            .Returns(previa);
        GivenPendingInvitation();
        var sut = CreateSut();

        await sut.HandleAsync(Command());

        previa.IsActive.Should().BeTrue();
        await _workerRepository.DidNotReceive().AddAsync(Arg.Any<Worker>(), Arg.Any<CancellationToken>());
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

    // --- MVP-107: aceptación por id desde la bandeja de recibidas ------------------------------

    [Fact]
    public async Task Deberia_CrearMembresiaYSituarLaSesion_Cuando_SeAceptaPorIdDesdeLaBandeja()
    {
        // Arrange
        var invitation = GivenPendingInvitation();
        var sut = CreateSut();

        // Act
        var result = await sut.HandleByIdAsync(InvitedUser.Id, invitation.Id);

        // Assert — mismo efecto que aceptar por token (CA-2/CA-3)
        result.Workspace.Id.Should().Be(TargetWorkspace.Id);
        result.AccessToken.Should().Be("access-token-con-workspace");
        invitation.Status.Should().Be(InvitationStatuses.Accepted);
        await _workspaceRepository.Received(1)
            .AddMemberAsync(Arg.Any<WorkspaceMember>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_OcultarLaInvitacion_Cuando_PorIdNoVaDirigidaAEstaCuenta()
    {
        // Arrange — la bandeja se autoriza por titularidad del email
        var invitation = WorkspaceInvitation.Create(
            TargetWorkspace.Id, Guid.NewGuid(), InvitationChannels.Email, "otra@ejemplo.com",
            "hash-otra", TimeSpan.FromDays(7));
        _invitationRepository.FindByIdAsync(invitation.Id, Arg.Any<CancellationToken>()).Returns(invitation);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleByIdAsync(InvitedUser.Id, invitation.Id);

        // Assert
        (await act.Should().ThrowAsync<InvitationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
        await _workspaceRepository.DidNotReceive()
            .AddMemberAsync(Arg.Any<WorkspaceMember>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_OcultarLaInvitacion_Cuando_PorIdEsDeCanalEnlace()
    {
        // Arrange — el enlace no tiene destinatario: no se acepta por id desde ninguna bandeja
        var invitation = WorkspaceInvitation.Create(
            TargetWorkspace.Id, Guid.NewGuid(), InvitationChannels.Link, null,
            "hash-enlace", TimeSpan.FromDays(7));
        _invitationRepository.FindByIdAsync(invitation.Id, Arg.Any<CancellationToken>()).Returns(invitation);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleByIdAsync(InvitedUser.Id, invitation.Id);

        // Assert
        (await act.Should().ThrowAsync<InvitationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
    }
}
