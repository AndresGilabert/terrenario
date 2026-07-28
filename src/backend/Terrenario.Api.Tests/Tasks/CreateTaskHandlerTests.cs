using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Tasks;
using Terrenario.Api.Application.Tasks.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Tasks;

namespace Terrenario.Api.Tests.Tasks;

public class CreateTaskHandlerTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private CreateTaskHandler CreateSut() => new(_taskRepository);

    [Fact]
    public async Task Deberia_DarDeAltaTarea_Y_Persistir()
    {
        // Arrange — CA-2: el catálogo se puebla con solo el nombre
        TaskItem? added = null;
        await _taskRepository.AddAsync(Arg.Do<TaskItem>(t => added = t), Arg.Any<CancellationToken>());
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(new CreateTaskCommand(WorkspaceId, "Poda", null));

        // Assert
        result.Name.Should().Be("Poda");
        result.IsActive.Should().BeTrue();
        added.Should().NotBeNull();
        added!.WorkspaceId.Should().Be(WorkspaceId);
        await _taskRepository.Received(1).AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
        await _taskRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_ComprobarDuplicadosConElNombreYaNormalizado()
    {
        // Arrange — la guarda se consulta con el texto que se persistiría, no con el crudo
        var sut = CreateSut();

        await sut.HandleAsync(new CreateTaskCommand(WorkspaceId, "  Abonado  ", null));

        await _taskRepository.Received(1).ExistsWithNameAsync(
            WorkspaceId, "Abonado", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RechazarNombreDuplicado_SinPersistir()
    {
        // Arrange — el catálogo existe para dar consistencia (RN-026): no admite la misma tarea dos veces
        _taskRepository.ExistsWithNameAsync(
                WorkspaceId, "Poda", null, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        // Act
        var act = () => sut.HandleAsync(new CreateTaskCommand(WorkspaceId, "Poda", null));

        // Assert
        (await act.Should().ThrowAsync<TaskConflictException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ConflictTaskNameDuplicate);
        await _taskRepository.DidNotReceive().AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
        await _taskRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RechazarNombreInvalido_AntesDeConsultarDuplicados()
    {
        // Arrange — el 400 de validación va antes que el 409 de conflicto
        var sut = CreateSut();

        // Act
        var act = () => sut.HandleAsync(new CreateTaskCommand(WorkspaceId, "   ", null));

        // Assert
        (await act.Should().ThrowAsync<TaskValidationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredTaskName);
        await _taskRepository.DidNotReceive().ExistsWithNameAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }
}
