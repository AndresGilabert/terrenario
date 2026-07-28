using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Tasks;
using Terrenario.Api.Application.Tasks.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Tasks;

namespace Terrenario.Api.Tests.Tasks;

public class UpdateTaskHandlerTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private UpdateTaskHandler CreateSut() => new(_taskRepository);

    /// <summary>PATCH mínimo: solo cambia el estado de actividad (el nombre, ausente).</summary>
    private static UpdateTaskCommand ActiveOnly(Guid taskId, bool isActive) => new(
        WorkspaceId, taskId,
        FieldUpdate<string>.Absent,
        FieldUpdate<bool>.Set(isActive));

    [Fact]
    public async Task Deberia_DevolverNull_Cuando_LaTareaNoEstaEnElWorkspace()
    {
        // Arrange — aislamiento multi-tenant (CA-1): no se toca el catálogo de otro Workspace
        var taskId = Guid.NewGuid();
        _taskRepository.FindByIdAsync(WorkspaceId, taskId, Arg.Any<CancellationToken>())
            .Returns((TaskItem?)null);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(ActiveOnly(taskId, isActive: false));

        // Assert
        result.Should().BeNull();
        await _taskRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Inactivar_SinCambiarElNombre()
    {
        // Arrange — CA-3 + regresión del PATCH parcial: inactivar no toca los campos omitidos
        var task = TaskItem.Create(WorkspaceId, "Poda");
        _taskRepository.FindByIdAsync(WorkspaceId, task.Id, Arg.Any<CancellationToken>()).Returns(task);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(ActiveOnly(task.Id, isActive: false));

        // Assert
        result!.IsActive.Should().BeFalse();
        result.Name.Should().Be("Poda");
        await _taskRepository.DidNotReceive().ExistsWithNameAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        await _taskRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RenombrarLaTarea_Y_Persistir()
    {
        // Arrange
        var task = TaskItem.Create(WorkspaceId, "Poda");
        _taskRepository.FindByIdAsync(WorkspaceId, task.Id, Arg.Any<CancellationToken>()).Returns(task);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(new UpdateTaskCommand(
            WorkspaceId, task.Id,
            FieldUpdate<string>.Set("Poda en verde"),
            FieldUpdate<bool>.Absent));

        // Assert
        result!.Name.Should().Be("Poda en verde");
        result.IsActive.Should().BeTrue();
        // Se excluye la propia tarea al comprobar duplicados (renombrarse a sí misma no es conflicto).
        await _taskRepository.Received(1).ExistsWithNameAsync(
            WorkspaceId, "Poda en verde", task.Id, Arg.Any<CancellationToken>());
        await _taskRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RechazarRenombrarAUnNombreYaExistente_SinTocarLaTarea()
    {
        // Arrange
        var task = TaskItem.Create(WorkspaceId, "Poda");
        _taskRepository.FindByIdAsync(WorkspaceId, task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _taskRepository.ExistsWithNameAsync(WorkspaceId, "Abonado", task.Id, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        // Act
        var act = () => sut.HandleAsync(new UpdateTaskCommand(
            WorkspaceId, task.Id,
            FieldUpdate<string>.Set("Abonado"),
            FieldUpdate<bool>.Absent));

        // Assert
        (await act.Should().ThrowAsync<TaskConflictException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ConflictTaskNameDuplicate);
        task.Name.Should().Be("Poda");
        await _taskRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
