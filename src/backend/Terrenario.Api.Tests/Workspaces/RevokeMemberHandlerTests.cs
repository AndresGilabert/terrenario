using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workers;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workers;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests de la revocación de acceso (MVP-204, HU-4/CA-7/CA-8). Cubren la transición a
/// <c>revocado</c> y las dos guardas de la invariante CA-8: no dejar el Workspace sin propietario ni
/// sin ningún miembro activo.
/// </summary>
public class RevokeMemberHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    // MVP-208 (CA-4): revocar retira a la persona de los responsables seleccionables.
    private readonly IWorkerRepository _workerRepository = Substitute.For<IWorkerRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid TargetUserId = Guid.NewGuid();

    private RevokeMemberHandler CreateSut() =>
        new(_workspaceRepository, new MemberRosterService(_workerRepository));

    [Fact]
    public async Task Deberia_RevocarMiembroActivo_Y_Persistir()
    {
        // Arrange — un miembro (no propietario) con otro activo detrás para no violar CA-8
        var member = WorkspaceMember.CreateMember(WorkspaceId, TargetUserId);
        _workspaceRepository.FindActiveMemberAsync(WorkspaceId, TargetUserId, Arg.Any<CancellationToken>())
            .Returns(member);
        _workspaceRepository.CountActiveMembersAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns(2);
        var sut = CreateSut();

        // Act
        await sut.HandleAsync(WorkspaceId, TargetUserId);

        // Assert — CA-7: cambia de estado sin borrar el vínculo
        member.Status.Should().Be(WorkspaceMemberStatuses.Revoked);
        member.IsActive.Should().BeFalse();
        await _workspaceRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RetirarloDeLosResponsablesSeleccionables_SinBorrarSuFila()
    {
        // MVP-208 (CA-4) — contrapartida de CA-7 en el maestro: la fila se inactiva, así que deja de
        // ser elegible pero los registros que ya la referencian siguen siendo válidos.
        var member = WorkspaceMember.CreateMember(WorkspaceId, TargetUserId);
        _workspaceRepository.FindActiveMemberAsync(WorkspaceId, TargetUserId, Arg.Any<CancellationToken>())
            .Returns(member);
        _workspaceRepository.CountActiveMembersAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns(2);

        var worker = Worker.CreateForMember(WorkspaceId, TargetUserId, "Bruno");
        _workerRepository.FindByUserAccountAsync(WorkspaceId, TargetUserId, Arg.Any<CancellationToken>())
            .Returns(worker);
        var sut = CreateSut();

        await sut.HandleAsync(WorkspaceId, TargetUserId);

        worker.IsActive.Should().BeFalse();
        worker.Name.Should().Be("Bruno");
    }

    [Fact]
    public async Task Deberia_Rechazar_Cuando_LaPersonaNoEsMiembroActivo()
    {
        // Arrange
        _workspaceRepository.FindActiveMemberAsync(WorkspaceId, TargetUserId, Arg.Any<CancellationToken>())
            .Returns((WorkspaceMember?)null);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(WorkspaceId, TargetUserId);

        // Assert
        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ResourceNotFound);
        await _workspaceRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Rechazar_Cuando_EsElPropietarioUnico()
    {
        // Arrange — CA-8: no se puede revocar al propietario mientras sea el único
        var owner = WorkspaceMember.CreateOwner(WorkspaceId, TargetUserId);
        _workspaceRepository.FindActiveMemberAsync(WorkspaceId, TargetUserId, Arg.Any<CancellationToken>())
            .Returns(owner);
        _workspaceRepository.CountActiveOwnersAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns(1);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(WorkspaceId, TargetUserId);

        // Assert
        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleCannotRevokeOwner);
        owner.Status.Should().Be(WorkspaceMemberStatuses.Active);
        await _workspaceRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Rechazar_Cuando_EsElUltimoMiembroActivo()
    {
        // Arrange — CA-8: no dejar el Workspace sin ningún miembro activo (aquí un miembro no propietario)
        var member = WorkspaceMember.CreateMember(WorkspaceId, TargetUserId);
        _workspaceRepository.FindActiveMemberAsync(WorkspaceId, TargetUserId, Arg.Any<CancellationToken>())
            .Returns(member);
        _workspaceRepository.CountActiveOwnersAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns(0);
        _workspaceRepository.CountActiveMembersAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns(1);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(WorkspaceId, TargetUserId);

        // Assert
        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleLastActiveMember);
        await _workspaceRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
