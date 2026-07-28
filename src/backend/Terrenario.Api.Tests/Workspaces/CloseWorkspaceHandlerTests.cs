using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Terrenario.Api.Application.Workers;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workers;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Email;
using Terrenario.Api.Infrastructure.Tokens;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests de la baja de Workspace (MVP-206, HU-2/HU-4). Cubren las dos ramas del árbol de decisión:
/// con copropietarios se reasigna y el solicitante sale (CA-5); siendo propietario único es baja
/// lógica con aviso por email y enlace de reactivación por miembro (CA-2/CA-6). Además, que la baja
/// está restringida al propietario y que un fallo de correo no la invalida.
/// </summary>
public class CloseWorkspaceHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IWorkspaceReactivationRequestRepository _reactivationRepository =
        Substitute.For<IWorkspaceReactivationRequestRepository>();
    private readonly IOneTimeTokenService _tokenService = Substitute.For<IOneTimeTokenService>();
    private readonly IWorkspaceLifecycleEmailSender _emailSender =
        Substitute.For<IWorkspaceLifecycleEmailSender>();
    // MVP-299 (3ª pasada, R-25): la reasignación revoca un acceso, así que toca el maestro.
    private readonly IWorkerRepository _workerRepository = Substitute.For<IWorkerRepository>();

    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CoOwnerId = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();

    private readonly Workspace _workspace = Workspace.Create(OwnerId, "Finca El Olivar");

    private CloseWorkspaceHandler CreateSut()
    {
        _tokenService.Generate().Returns(_ => new OneTimeToken($"token-{Guid.NewGuid()}", $"hash-{Guid.NewGuid()}"));
        _emailSender.IsEnabled.Returns(true);
        _workspaceRepository.FindByIdAsync(_workspace.Id, Arg.Any<CancellationToken>()).Returns(_workspace);

        return new CloseWorkspaceHandler(
            _workspaceRepository,
            _reactivationRepository,
            _tokenService,
            _emailSender,
            new MemberRosterService(_workerRepository),
            Options.Create(new WorkspaceLifecycleOptions
            {
                ReactivationLifetimeDays = 7,
                ReactivationBaseUrl = "http://localhost:5173/reactivations"
            }),
            Substitute.For<ILogger<CloseWorkspaceHandler>>());
    }

    private CloseWorkspaceCommand Command() =>
        new(_workspace.Id, _workspace.Name, OwnerId, "Antonio");

    private void GivenActingMember(WorkspaceMember member)
        => _workspaceRepository.FindActiveMemberAsync(_workspace.Id, OwnerId, Arg.Any<CancellationToken>())
            .Returns(member);

    private void GivenMembers(params WorkspaceMemberDetail[] members)
        => _workspaceRepository.ListMembersAsync(_workspace.Id, Arg.Any<CancellationToken>())
            .Returns(members.ToList());

    private static WorkspaceMemberDetail Detail(Guid userId, string name, string role) =>
        new(userId, name, $"{name.ToLowerInvariant()}@ejemplo.com", role,
            WorkspaceMemberStatuses.Active, DateTimeOffset.UtcNow);

    [Fact]
    public async Task Deberia_Rechazar_ASiQuienLoPideNoEsPropietario()
    {
        GivenActingMember(WorkspaceMember.CreateMember(_workspace.Id, OwnerId));
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(Command());

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.AuthWorkspaceOwnerRequired);
        _workspace.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Deberia_Rechazar_ASiQuienLoPideNoEsMiembroActivo()
    {
        _workspaceRepository.FindActiveMemberAsync(_workspace.Id, OwnerId, Arg.Any<CancellationToken>())
            .Returns((WorkspaceMember?)null);
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(Command());

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ResourceNotFound);
    }

    [Fact]
    public async Task ConVariosPropietarios_Deberia_ReasignarYSacarAlSolicitante()
    {
        // CA-5 — el Workspace no se da de baja: cambia de manos y sigue vivo.
        var acting = WorkspaceMember.CreateOwner(_workspace.Id, OwnerId);
        var coOwner = WorkspaceMember.CreateOwner(_workspace.Id, CoOwnerId);
        GivenActingMember(acting);
        _workspaceRepository.FindOtherActiveOwnerAsync(_workspace.Id, OwnerId, Arg.Any<CancellationToken>())
            .Returns(coOwner);
        GivenMembers(Detail(CoOwnerId, "Marta", WorkspaceRoles.Owner));
        var sut = CreateSut();

        var result = await sut.HandleAsync(Command());

        result.Outcome.Should().Be(WorkspaceClosureOutcomes.Transferred);
        result.NewOwnerDisplayName.Should().Be("Marta");
        _workspace.IsDeleted.Should().BeFalse();
        _workspace.OwnerId.Should().Be(CoOwnerId);
        acting.Role.Should().Be(WorkspaceRoles.Member);
        acting.Status.Should().Be(WorkspaceMemberStatuses.Revoked);
        await _reactivationRepository.DidNotReceiveWithAnyArgs()
            .AddRangeAsync(default!, default);
    }

    [Fact]
    public async Task AlReasignar_Deberia_RetirarAlSolicitanteDeLosResponsablesSeleccionables()
    {
        // MVP-299 (3ª pasada, R-25) — la reasignación revoca la membresía del solicitante, así que su
        // fila del maestro debe inactivarse igual que al revocar el acceso a mano (MVP-208, CA-4). Sin
        // esto seguía apareciendo como «MIEMBRO» activo en Trabajadores de un Workspace ajeno.
        var acting = WorkspaceMember.CreateOwner(_workspace.Id, OwnerId);
        GivenActingMember(acting);
        _workspaceRepository.FindOtherActiveOwnerAsync(_workspace.Id, OwnerId, Arg.Any<CancellationToken>())
            .Returns(WorkspaceMember.CreateOwner(_workspace.Id, CoOwnerId));
        GivenMembers(Detail(CoOwnerId, "Marta", WorkspaceRoles.Owner));

        var worker = Worker.CreateForMember(_workspace.Id, OwnerId, "Antonio");
        _workerRepository.FindByUserAccountAsync(_workspace.Id, OwnerId, Arg.Any<CancellationToken>())
            .Returns(worker);
        var sut = CreateSut();

        await sut.HandleAsync(Command());

        // La fila se inactiva, nunca se borra: lo que ya la referencie sigue siendo válido.
        worker.IsActive.Should().BeFalse();
        worker.Name.Should().Be("Antonio");
        worker.UserAccountId.Should().Be(OwnerId);
    }

    [Fact]
    public async Task ComoPropietarioUnico_NoDeberia_TocarElMaestroDeResponsables()
    {
        // La baja lógica no revoca a nadie: los miembros conservan su acceso para poder pedir la
        // reactivación (CA-7 de MVP-206), así que el maestro no se toca.
        GivenActingMember(WorkspaceMember.CreateOwner(_workspace.Id, OwnerId));
        _workspaceRepository.FindOtherActiveOwnerAsync(_workspace.Id, OwnerId, Arg.Any<CancellationToken>())
            .Returns((WorkspaceMember?)null);
        GivenMembers(Detail(OwnerId, "Antonio", WorkspaceRoles.Owner));

        var worker = Worker.CreateForMember(_workspace.Id, OwnerId, "Antonio");
        _workerRepository.FindByUserAccountAsync(_workspace.Id, OwnerId, Arg.Any<CancellationToken>())
            .Returns(worker);
        var sut = CreateSut();

        await sut.HandleAsync(Command());

        worker.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ComoPropietarioUnico_Deberia_DarDeBajaYAvisarAlResto()
    {
        // CA-2/CA-6 — baja lógica y un enlace de un solo uso por miembro activo notificado.
        GivenActingMember(WorkspaceMember.CreateOwner(_workspace.Id, OwnerId));
        _workspaceRepository.FindOtherActiveOwnerAsync(_workspace.Id, OwnerId, Arg.Any<CancellationToken>())
            .Returns((WorkspaceMember?)null);
        GivenMembers(
            Detail(OwnerId, "Antonio", WorkspaceRoles.Owner),
            Detail(MemberId, "Lucia", WorkspaceRoles.Member));
        List<WorkspaceReactivationRequest>? issued = null;
        await _reactivationRepository.AddRangeAsync(
            Arg.Do<IEnumerable<WorkspaceReactivationRequest>>(r => issued = r.ToList()),
            Arg.Any<CancellationToken>());
        var sut = CreateSut();

        var result = await sut.HandleAsync(Command());

        result.Outcome.Should().Be(WorkspaceClosureOutcomes.Deleted);
        result.NotifiedMembers.Should().Be(1);
        result.EmailsSent.Should().Be(1);
        _workspace.IsDeleted.Should().BeTrue();
        _workspace.DeletedByUserId.Should().Be(OwnerId);
        issued.Should().ContainSingle().Which.RecipientUserId.Should().Be(MemberId);
        issued![0].AuthorizerUserId.Should().Be(OwnerId);
        await _emailSender.Received(1).SendWorkspaceClosedAsync(
            Arg.Is<WorkspaceClosedEmail>(m => m.ToEmail == "lucia@ejemplo.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ComoPropietarioUnicoSinNadieMas_Deberia_DarDeBajaSinAvisos()
    {
        GivenActingMember(WorkspaceMember.CreateOwner(_workspace.Id, OwnerId));
        _workspaceRepository.FindOtherActiveOwnerAsync(_workspace.Id, OwnerId, Arg.Any<CancellationToken>())
            .Returns((WorkspaceMember?)null);
        GivenMembers(Detail(OwnerId, "Antonio", WorkspaceRoles.Owner));
        var sut = CreateSut();

        var result = await sut.HandleAsync(Command());

        result.NotifiedMembers.Should().Be(0);
        _workspace.IsDeleted.Should().BeTrue();
        await _emailSender.DidNotReceiveWithAnyArgs()
            .SendWorkspaceClosedAsync(default!, default);
    }

    [Fact]
    public async Task UnFalloDeCorreo_NoDeberia_InvalidarLaBaja()
    {
        GivenActingMember(WorkspaceMember.CreateOwner(_workspace.Id, OwnerId));
        _workspaceRepository.FindOtherActiveOwnerAsync(_workspace.Id, OwnerId, Arg.Any<CancellationToken>())
            .Returns((WorkspaceMember?)null);
        GivenMembers(
            Detail(OwnerId, "Antonio", WorkspaceRoles.Owner),
            Detail(MemberId, "Lucia", WorkspaceRoles.Member));
        var sut = CreateSut();
        _emailSender.SendWorkspaceClosedAsync(Arg.Any<WorkspaceClosedEmail>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("SMTP caído")));

        var result = await sut.HandleAsync(Command());

        result.Outcome.Should().Be(WorkspaceClosureOutcomes.Deleted);
        result.NotifiedMembers.Should().Be(1);
        result.EmailsSent.Should().Be(0);
        _workspace.IsDeleted.Should().BeTrue();
    }
}
