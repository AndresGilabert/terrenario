using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Tasks;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Tests.Integration;

namespace Terrenario.Api.Tests.Tasks;

/// <summary>
/// Tests del repositorio del catálogo de tareas contra PostgreSQL real (MVP-205): ejercitan la
/// traducción a SQL del filtro por estado, del aislamiento por Workspace y de la comparación de
/// nombres insensible a mayúsculas, que los mocks no ven (lección de P-014).
/// </summary>
public sealed class TaskRepositoryPostgresTests : RepositoryTestBase
{

    private async Task<Workspace> SeedWorkspaceAsync(string suffix = "")
    {
        var user = User.Create($"google-sub{suffix}", "Andrés", $"andres{suffix}@ejemplo.com");
        Db.Users.Add(user);
        var workspace = Workspace.Create(user.Id, $"Finca El Olivar {suffix}");
        Db.Workspaces.Add(workspace);
        await Db.SaveChangesAsync();
        return workspace;
    }

    [Fact]
    public async Task ListByWorkspaceAsync_Deberia_AislarPorWorkspace()
    {
        // CA-1 — el catálogo de un Workspace no ve ni afecta al de otro
        var mine = await SeedWorkspaceAsync("-a");
        var other = await SeedWorkspaceAsync("-b");
        Db.Tasks.Add(TaskItem.Create(mine.Id, "Poda"));
        Db.Tasks.Add(TaskItem.Create(other.Id, "Vendimia ajena"));
        await Db.SaveChangesAsync();

        var repository = new TaskRepository(Db);

        var result = await repository.ListByWorkspaceAsync(mine.Id, null, default);

        result.Should().ContainSingle().Which.Name.Should().Be("Poda");
    }

    [Fact]
    public async Task ListByWorkspaceAsync_Deberia_ArrancarVacio()
    {
        // CA-2 — el catálogo nace vacío, sin semillas ni configuración externa
        var workspace = await SeedWorkspaceAsync("-vacio");

        var repository = new TaskRepository(Db);

        var result = await repository.ListByWorkspaceAsync(workspace.Id, null, default);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListByWorkspaceAsync_Deberia_FiltrarPorEstado_Y_OrdenarActivasPrimero()
    {
        var workspace = await SeedWorkspaceAsync("-c");
        var activa = TaskItem.Create(workspace.Id, "Recolección");
        var inactiva = TaskItem.Create(workspace.Id, "Abonado");
        inactiva.SetActive(false);
        Db.Tasks.AddRange(activa, inactiva);
        await Db.SaveChangesAsync();

        var repository = new TaskRepository(Db);

        var soloActivas = await repository.ListByWorkspaceAsync(workspace.Id, isActive: true, default);
        soloActivas.Should().ContainSingle().Which.Name.Should().Be("Recolección");

        // Orden: activas primero aunque alfabéticamente "Abonado" iría antes que "Recolección".
        var todas = await repository.ListByWorkspaceAsync(workspace.Id, null, default);
        todas.Select(t => t.Name).Should().ContainInOrder("Recolección", "Abonado");
    }

    [Fact]
    public async Task FindByIdAsync_Deberia_NoDevolverTareaDeOtroWorkspace()
    {
        var mine = await SeedWorkspaceAsync("-d");
        var other = await SeedWorkspaceAsync("-e");
        var ajena = TaskItem.Create(other.Id, "Poda");
        Db.Tasks.Add(ajena);
        await Db.SaveChangesAsync();

        var repository = new TaskRepository(Db);

        var found = await repository.FindByIdAsync(mine.Id, ajena.Id, default);

        found.Should().BeNull();
    }

    [Fact]
    public async Task ExistsWithNameAsync_Deberia_IgnorarMayusculas_Y_AcotarPorWorkspace()
    {
        var mine = await SeedWorkspaceAsync("-f");
        var other = await SeedWorkspaceAsync("-g");
        Db.Tasks.Add(TaskItem.Create(mine.Id, "Poda"));
        await Db.SaveChangesAsync();

        var repository = new TaskRepository(Db);

        (await repository.ExistsWithNameAsync(mine.Id, "Poda", null, default)).Should().BeTrue();
        (await repository.ExistsWithNameAsync(mine.Id, "poda", null, default)).Should().BeTrue();
        (await repository.ExistsWithNameAsync(mine.Id, "PODA", null, default)).Should().BeTrue();
        (await repository.ExistsWithNameAsync(mine.Id, "Abonado", null, default)).Should().BeFalse();
        // El catálogo de otro Workspace no genera conflicto (CA-1).
        (await repository.ExistsWithNameAsync(other.Id, "Poda", null, default)).Should().BeFalse();
    }

    [Fact]
    public async Task ExistsWithNameAsync_Deberia_ExcluirLaPropiaTarea()
    {
        // Renombrar una tarea conservando su nombre (o cambiando solo mayúsculas) no es un duplicado.
        var workspace = await SeedWorkspaceAsync("-h");
        var task = TaskItem.Create(workspace.Id, "Poda");
        Db.Tasks.Add(task);
        await Db.SaveChangesAsync();

        var repository = new TaskRepository(Db);

        (await repository.ExistsWithNameAsync(workspace.Id, "Poda", task.Id, default)).Should().BeFalse();
    }

    [Fact]
    public async Task ExistsWithNameAsync_Deberia_VerTareasInactivas()
    {
        // Una tarea inactivada sigue ocupando su nombre: reactivarla es mejor que duplicarla (CA-3).
        var workspace = await SeedWorkspaceAsync("-i");
        var inactiva = TaskItem.Create(workspace.Id, "Poda");
        inactiva.SetActive(false);
        Db.Tasks.Add(inactiva);
        await Db.SaveChangesAsync();

        var repository = new TaskRepository(Db);

        (await repository.ExistsWithNameAsync(workspace.Id, "poda", null, default)).Should().BeTrue();
    }

    [Fact]
    public async Task FindByNameAsync_Deberia_ResolverLaTareaExistente_IgnorandoMayusculas()
    {
        // MVP-302 — el guardado de una tarea libre necesita saber **cuál** es la tarea que ocupa el
        // nombre, no solo si está ocupado: es lo que permite reutilizarla en vez de crear una segunda.
        var mine = await SeedWorkspaceAsync("-j");
        var other = await SeedWorkspaceAsync("-k");
        var poda = TaskItem.Create(mine.Id, "Poda");
        Db.Tasks.Add(poda);
        await Db.SaveChangesAsync();

        var repository = new TaskRepository(Db);

        (await repository.FindByNameAsync(mine.Id, "PODA", default))!.Id.Should().Be(poda.Id);
        (await repository.FindByNameAsync(mine.Id, "Abonado", default)).Should().BeNull();
        // El catálogo de otro Workspace no interfiere (CA-3 de MVP-302).
        (await repository.FindByNameAsync(other.Id, "Poda", default)).Should().BeNull();
    }

    [Fact]
    public async Task FindByNameAsync_Deberia_VerLasInactivas()
    {
        // Siguen ocupando su nombre (MVP-205, CA-3): MVP-302 las reactiva en vez de duplicarlas.
        var workspace = await SeedWorkspaceAsync("-l");
        var inactiva = TaskItem.Create(workspace.Id, "Abonado");
        inactiva.SetActive(false);
        Db.Tasks.Add(inactiva);
        await Db.SaveChangesAsync();

        var repository = new TaskRepository(Db);

        var found = await repository.FindByNameAsync(workspace.Id, "abonado", default);
        found!.Id.Should().Be(inactiva.Id);
        found.IsActive.Should().BeFalse();
    }

}
