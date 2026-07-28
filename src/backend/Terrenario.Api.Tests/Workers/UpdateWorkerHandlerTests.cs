using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workers;
using Terrenario.Api.Application.Workers.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Tests.Workers;

public class UpdateWorkerHandlerTests
{
    private readonly IWorkerRepository _workerRepository = Substitute.For<IWorkerRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private UpdateWorkerHandler CreateSut() => new(_workerRepository);

    /// <summary>PATCH mínimo: solo cambia el estado de actividad (el resto ausente).</summary>
    private static UpdateWorkerCommand ActiveOnly(Guid workerId, bool isActive) => new(
        WorkspaceId, workerId,
        FieldUpdate<string>.Absent,
        FieldUpdate<decimal?>.Absent,
        FieldUpdate<bool>.Set(isActive));

    [Fact]
    public async Task Deberia_DevolverNull_Cuando_ElTrabajadorNoEstaEnElWorkspace()
    {
        // Arrange — aislamiento multi-tenant: no lo encuentra en el Workspace activo
        var workerId = Guid.NewGuid();
        _workerRepository.FindByIdAsync(WorkspaceId, workerId, Arg.Any<CancellationToken>())
            .Returns((Worker?)null);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(ActiveOnly(workerId, isActive: false));

        // Assert
        result.Should().BeNull();
        await _workerRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Inactivar_SinBorrar_LaTarifa()
    {
        // Arrange — CA-3 + regresión del PATCH parcial: inactivar no debe borrar los campos omitidos
        var worker = Worker.Create(WorkspaceId, "Antonio", hourlyRate: 14m);
        _workerRepository.FindByIdAsync(WorkspaceId, worker.Id, Arg.Any<CancellationToken>()).Returns(worker);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(ActiveOnly(worker.Id, isActive: false));

        // Assert
        result!.IsActive.Should().BeFalse();
        result.Name.Should().Be("Antonio");
        result.HourlyRate.Should().Be(14m);
        await _workerRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_EditarNombreYTarifa_Y_Persistir()
    {
        // Arrange
        var worker = Worker.Create(WorkspaceId, "Antonio", hourlyRate: 10m);
        _workerRepository.FindByIdAsync(WorkspaceId, worker.Id, Arg.Any<CancellationToken>()).Returns(worker);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(new UpdateWorkerCommand(
            WorkspaceId, worker.Id,
            FieldUpdate<string>.Set("Antonio Podador"),
            FieldUpdate<decimal?>.Set(18m),
            FieldUpdate<bool>.Absent));

        // Assert
        result!.Name.Should().Be("Antonio Podador");
        result.HourlyRate.Should().Be(18m);
        await _workerRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RechazarRenombrado_Cuando_ElNombreYaExiste_DejandoElTrabajadorIntacto()
    {
        // MVP-207 (CA-2)
        var worker = Worker.Create(WorkspaceId, "Antonio", hourlyRate: 10m);
        _workerRepository.FindByIdAsync(WorkspaceId, worker.Id, Arg.Any<CancellationToken>()).Returns(worker);
        _workerRepository.ExistsWithNameAsync(
                WorkspaceId, "Juan Pérez", worker.Id, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        var act = () => sut.HandleAsync(new UpdateWorkerCommand(
            WorkspaceId, worker.Id,
            FieldUpdate<string>.Set("Juan Pérez"),
            FieldUpdate<decimal?>.Absent,
            FieldUpdate<bool>.Absent));

        (await act.Should().ThrowAsync<WorkerConflictException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ConflictWorkerNameDuplicate);
        worker.Name.Should().Be("Antonio");
        await _workerRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── MVP-208 (CA-4) · lo que un responsable con cuenta no admite editar ─────────────────────

    [Fact]
    public async Task Deberia_Rechazar_ConflictoDeIdentidad_Cuando_SeRenombraUnMiembro()
    {
        var miembro = Worker.CreateForMember(WorkspaceId, Guid.NewGuid(), "Andrés Gilabert");
        _workerRepository.FindByIdAsync(WorkspaceId, miembro.Id, Arg.Any<CancellationToken>()).Returns(miembro);
        var sut = CreateSut();

        var act = () => sut.HandleAsync(new UpdateWorkerCommand(
            WorkspaceId, miembro.Id,
            FieldUpdate<string>.Set("Otro Nombre"),
            FieldUpdate<decimal?>.Absent,
            FieldUpdate<bool>.Absent));

        (await act.Should().ThrowAsync<WorkerBusinessRuleException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleWorkerIdentityManaged);
        miembro.Name.Should().Be("Andrés Gilabert");
        await _workerRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        // No cuesta ni una consulta de duplicados: el renombrado no está permitido de entrada.
        await _workerRepository.DidNotReceive().ExistsWithNameAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Rechazar_Cuando_SeIntentaInactivarAUnMiembro()
    {
        var miembro = Worker.CreateForMember(WorkspaceId, Guid.NewGuid(), "Andrés Gilabert");
        _workerRepository.FindByIdAsync(WorkspaceId, miembro.Id, Arg.Any<CancellationToken>()).Returns(miembro);
        var sut = CreateSut();

        var act = () => sut.HandleAsync(ActiveOnly(miembro.Id, isActive: false));

        (await act.Should().ThrowAsync<WorkerBusinessRuleException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleWorkerMembershipManaged);
        miembro.IsActive.Should().BeTrue();
        await _workerRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_EditarLaTarifaDeUnMiembro_Y_DevolverloComoMiembro()
    {
        var userId = Guid.NewGuid();
        var miembro = Worker.CreateForMember(WorkspaceId, userId, "Andrés Gilabert");
        _workerRepository.FindByIdAsync(WorkspaceId, miembro.Id, Arg.Any<CancellationToken>()).Returns(miembro);
        var sut = CreateSut();

        var result = await sut.HandleAsync(new UpdateWorkerCommand(
            WorkspaceId, miembro.Id,
            FieldUpdate<string>.Absent,
            FieldUpdate<decimal?>.Set(21m),
            FieldUpdate<bool>.Absent));

        result!.HourlyRate.Should().Be(21m);
        result.Name.Should().Be("Andrés Gilabert");
        result.Kind.Should().Be(WorkerKinds.Member);
        result.UserAccountId.Should().Be(userId);
        await _workerRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_NoConsultarDuplicados_Cuando_ElPatchNoTraeNombre()
    {
        // Regresión del PATCH parcial: inactivar no es un renombrado.
        var worker = Worker.Create(WorkspaceId, "Antonio", hourlyRate: 10m);
        _workerRepository.FindByIdAsync(WorkspaceId, worker.Id, Arg.Any<CancellationToken>()).Returns(worker);
        var sut = CreateSut();

        await sut.HandleAsync(ActiveOnly(worker.Id, isActive: false));

        await _workerRepository.DidNotReceive().ExistsWithNameAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }
}
