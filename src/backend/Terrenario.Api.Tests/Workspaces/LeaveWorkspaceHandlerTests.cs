using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workers;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workers;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// MVP-807 (HU-1, <c>P-048</c>) — La salida voluntaria de un Workspace.
///
/// Lo que se prueba aquí no es que la membresía cambie de estado —eso lo hace el mismo método que la
/// revocación— sino que **las dos guardas se llaman en vez de reimplementarse**: es la condición con
/// la que se registró <c>P-024</c> y lo que exige el <c>CA-2</c>.
/// </summary>
public class LeaveWorkspaceHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IWorkerRepository _workerRepository = Substitute.For<IWorkerRepository>();

    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private LeaveWorkspaceHandler CreateSut() => new(
        _workspaceRepository,
        new WorkspaceOwnershipGuard(_workspaceRepository),
        new MemberRosterService(_workerRepository));

    private WorkspaceMember SeedActiveMember(bool owner = false)
    {
        var member = owner
            ? WorkspaceMember.CreateOwner(WorkspaceId, UserId)
            : WorkspaceMember.CreateMember(WorkspaceId, UserId);

        _workspaceRepository.FindActiveMemberAsync(WorkspaceId, UserId, Arg.Any<CancellationToken>())
            .Returns(member);
        _workspaceRepository.CountActiveMembersAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns(2);
        _workspaceRepository.ListSoleOwnedAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SoleOwnedWorkspace>());

        return member;
    }

    [Fact]
    public async Task Deberia_DejarLaMembresiaRevocada_Y_Persistir()
    {
        // El efecto es **el mismo que revocar**: la membresía deja de resolver contexto pero el vínculo
        // no se borra, así que los registros que ya lo referencian siguen siendo válidos (CA-7 de
        // MVP-204). Reingresar exige invitación nueva, igual que para quien fue revocado (CA-5).
        var member = SeedActiveMember();

        await CreateSut().HandleAsync(WorkspaceId, UserId);

        member.Status.Should().Be(WorkspaceMemberStatuses.Revoked);
        await _workspaceRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RetirarloDeLosResponsablesSeleccionables_SinBorrarSuFila()
    {
        // CA-4 (MVP-208) — deja de ofrecerse como responsable, y las labores que ya tenía asignadas
        // siguen mostrando su nombre porque la fila se inactiva, no se borra.
        var worker = Worker.CreateForMember(WorkspaceId, UserId, "Antonio Ruiz");
        SeedActiveMember();
        _workerRepository.FindByUserAccountAsync(WorkspaceId, UserId, Arg.Any<CancellationToken>())
            .Returns(worker);

        await CreateSut().HandleAsync(WorkspaceId, UserId);

        worker.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task NoDeberia_DejarSalir_AlPropietarioUnico()
    {
        // CA-2 — la misma obligación que impone la baja de cuenta, y **por la misma guarda**: lo que
        // decide es `ListSoleOwnedAsync`, que es la consulta de `WorkspaceOwnershipGuard`.
        SeedActiveMember(owner: true);
        _workspaceRepository.ListSoleOwnedAsync(UserId, Arg.Any<CancellationToken>())
            .Returns([new SoleOwnedWorkspace(WorkspaceId, "Finca El Olivar", 1)]);

        var act = () => CreateSut().HandleAsync(WorkspaceId, UserId);

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleWorkspaceOwnershipUnresolved);
        await _workspaceRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_PasarPorLaGuardaDeNoOrfandad()
    {
        // CA-2 exige comprobar que **la llamada pasa por esa guarda**, no que el resultado coincida:
        // una comprobación equivalente escrita a mano daría el mismo veredicto hoy y divergiría en
        // cuanto la regla cambiara, que es exactamente lo que le pasó a `can_revoke` (`P-049`).
        SeedActiveMember();

        await CreateSut().HandleAsync(WorkspaceId, UserId);

        await _workspaceRepository.Received().ListSoleOwnedAsync(UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NoDeberia_DejarSalir_AlUltimoMiembroActivo()
    {
        // CA-3 — la regla es «no dejarlo vacío», no «no dejarlo sin propietario»: se comprueba aunque
        // quien se va no sea propietario.
        SeedActiveMember();
        _workspaceRepository.CountActiveMembersAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns(1);

        var act = () => CreateSut().HandleAsync(WorkspaceId, UserId);

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleLastActiveMember);
    }

    [Fact]
    public async Task Deberia_Rechazar_Cuando_NoEsMiembroActivo()
    {
        _workspaceRepository.FindActiveMemberAsync(WorkspaceId, UserId, Arg.Any<CancellationToken>())
            .Returns((WorkspaceMember?)null);

        var act = () => CreateSut().HandleAsync(WorkspaceId, UserId);

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ResourceNotFound);
    }
}
