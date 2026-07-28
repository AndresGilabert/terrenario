using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workers;
using Terrenario.Api.Application.Workers.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Tests.Workers;

public class CreateWorkerHandlerTests
{
    private readonly IWorkerRepository _workerRepository = Substitute.For<IWorkerRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private CreateWorkerHandler CreateSut() => new(_workerRepository);

    [Fact]
    public async Task Deberia_DarDeAltaTrabajador_Y_Persistir()
    {
        // Arrange
        Worker? added = null;
        await _workerRepository.AddAsync(Arg.Do<Worker>(w => added = w), Arg.Any<CancellationToken>());
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(new CreateWorkerCommand(WorkspaceId, "Antonio", 12m));

        // Assert
        result.Name.Should().Be("Antonio");
        result.HourlyRate.Should().Be(12m);
        result.IsActive.Should().BeTrue();
        added.Should().NotBeNull();
        added!.WorkspaceId.Should().Be(WorkspaceId);
        await _workerRepository.Received(1).AddAsync(Arg.Any<Worker>(), Arg.Any<CancellationToken>());
        await _workerRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_ComprobarDuplicadosConElNombreYaNormalizado()
    {
        // MVP-207 (CA-2) — la guarda se consulta con el texto que se persistiría, no con el crudo.
        var sut = CreateSut();

        await sut.HandleAsync(new CreateWorkerCommand(WorkspaceId, "  Juan Pérez  ", null));

        await _workerRepository.Received(1).ExistsWithNameAsync(
            WorkspaceId, "Juan Pérez", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RechazarNombreDuplicado_SinPersistir()
    {
        // MVP-207 (CA-2) — el maestro existe «para evitar nombres duplicados» (MVP-204, HU-1).
        _workerRepository.ExistsWithNameAsync(
                WorkspaceId, "Juan Pérez", null, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        var act = () => sut.HandleAsync(new CreateWorkerCommand(WorkspaceId, "Juan Pérez", null));

        (await act.Should().ThrowAsync<WorkerConflictException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ConflictWorkerNameDuplicate);
        await _workerRepository.DidNotReceive().AddAsync(Arg.Any<Worker>(), Arg.Any<CancellationToken>());
        await _workerRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RechazarNombreInvalido_AntesDeConsultarDuplicados()
    {
        // El 400 de validación va antes que el 409 de conflicto.
        var sut = CreateSut();

        var act = () => sut.HandleAsync(new CreateWorkerCommand(WorkspaceId, "   ", null));

        (await act.Should().ThrowAsync<WorkerValidationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredName);
        await _workerRepository.DidNotReceive().ExistsWithNameAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }
}
