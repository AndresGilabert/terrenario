using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workers;
using Terrenario.Api.Application.Workers.Commands;
using Terrenario.Api.Common;
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
}
