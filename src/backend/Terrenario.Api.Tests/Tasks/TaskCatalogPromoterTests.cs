using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Tasks;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Tasks;

namespace Terrenario.Api.Tests.Tasks;

/// <summary>
/// MVP-302 — Guardado de una tarea libre en el catálogo del Workspace. Lo que se comprueba aquí es
/// que la historia **reutiliza** la guarda de duplicados de MVP-205 en vez de construir otra: el
/// nombre ya ocupado no produce un 409, se resuelve reutilizando la tarea existente.
/// </summary>
public class TaskCatalogPromoterTests
{
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private TaskCatalogPromoter CreateSut() => new(_tasks);

    [Fact]
    public async Task Deberia_CrearLaTarea_SiElNombreEstaLibre()
    {
        // CA-2 — la tarea queda disponible en el catálogo del Workspace activo
        TaskItem? added = null;
        await _tasks.AddAsync(Arg.Do<TaskItem>(t => added = t), Arg.Any<CancellationToken>());

        var (task, outcome) = await CreateSut().ResolveOrCreateAsync(WorkspaceId, "Poda de mantenimiento");

        outcome.Should().Be(TaskCatalogOutcome.Created);
        task.Name.Should().Be("Poda de mantenimiento");
        task.IsActive.Should().BeTrue();
        task.WorkspaceId.Should().Be(WorkspaceId);
        added.Should().BeSameAs(task);
    }

    [Fact]
    public async Task Deberia_BuscarConElNombreYaNormalizado()
    {
        // La búsqueda usa el mismo texto que se persistiría: si no, «  Poda  » no encontraría «Poda»
        await CreateSut().ResolveOrCreateAsync(WorkspaceId, "  Poda  ");

        await _tasks.Received(1).FindByNameAsync(WorkspaceId, "Poda", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_ReutilizarLaTareaExistente_SinCrearUnaSegunda()
    {
        // CA-1 de MVP-302 y R-14 de MVP-299: la guarda de MVP-205 se reutiliza, no se reconstruye.
        // Un nombre ya ocupado no puede acabar en 409: el usuario no tendría nada que arreglar.
        // La comparación insensible a mayúsculas vive en el repositorio (mismo criterio que el índice
        // único de MVP-205), así que el mock responde a cualquier grafía del mismo nombre.
        var existing = TaskItem.Create(WorkspaceId, "Poda");
        _tasks.FindByNameAsync(
                WorkspaceId,
                Arg.Is<string>(n => string.Equals(n, "Poda", StringComparison.OrdinalIgnoreCase)),
                Arg.Any<CancellationToken>())
            .Returns(existing);

        var (task, outcome) = await CreateSut().ResolveOrCreateAsync(WorkspaceId, "poda");

        outcome.Should().Be(TaskCatalogOutcome.Reused);
        task.Should().BeSameAs(existing);
        await _tasks.DidNotReceive().AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_ReactivarLaTareaInactivada_EnVezDeDuplicarla()
    {
        // MVP-205 (CA-3) lo dejó fijado: las inactivas siguen ocupando su nombre y «se reactivan, no
        // se duplican». Volver a escribir esa labor es la señal de que se quiere disponible otra vez.
        var existing = TaskItem.Create(WorkspaceId, "Abonado");
        existing.SetActive(false);
        _tasks.FindByNameAsync(WorkspaceId, "Abonado", Arg.Any<CancellationToken>()).Returns(existing);

        var (task, outcome) = await CreateSut().ResolveOrCreateAsync(WorkspaceId, "Abonado");

        outcome.Should().Be(TaskCatalogOutcome.Reactivated);
        task.IsActive.Should().BeTrue();
        await _tasks.DidNotReceive().AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RechazarNombreVacio_ConElCodigoDelCatalogo()
    {
        // La validación es la del catálogo (MVP-205), no una nueva: mismo código y mismo mensaje
        var act = () => CreateSut().ResolveOrCreateAsync(WorkspaceId, "   ");

        (await act.Should().ThrowAsync<TaskValidationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredTaskName);
    }

    [Fact]
    public async Task NoDeberia_Persistir()
    {
        // La tarea entra en la misma unidad de trabajo que la actividad: o se guardan las dos o
        // ninguna (CA-3). Persistir aquí rompería esa atomicidad.
        await CreateSut().ResolveOrCreateAsync(WorkspaceId, "Riego");

        await _tasks.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
