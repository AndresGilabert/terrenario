using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Tasks;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;

namespace Terrenario.Api.Tests.Tasks;

/// <summary>
/// Tests del repositorio del catálogo de tareas contra SQLite real (MVP-205): ejercitan la
/// traducción a SQL del filtro por estado, del aislamiento por Workspace y de la comparación de
/// nombres insensible a mayúsculas, que los mocks no ven (lección de P-014).
/// </summary>
public sealed class TaskRepositorySqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TerrenarioDbContext _db;

    public TaskRepositorySqliteTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TerrenarioDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TerrenarioDbContext(options);
        _db.Database.EnsureCreated();
    }

    private async Task<Workspace> SeedWorkspaceAsync(string suffix = "")
    {
        var user = User.Create($"google-sub{suffix}", "Andrés", $"andres{suffix}@ejemplo.com");
        _db.Users.Add(user);
        var workspace = Workspace.Create(user.Id, $"Finca El Olivar {suffix}");
        _db.Workspaces.Add(workspace);
        await _db.SaveChangesAsync();
        return workspace;
    }

    [Fact]
    public async Task ListByWorkspaceAsync_Deberia_AislarPorWorkspace()
    {
        // CA-1 — el catálogo de un Workspace no ve ni afecta al de otro
        var mine = await SeedWorkspaceAsync("-a");
        var other = await SeedWorkspaceAsync("-b");
        _db.Tasks.Add(TaskItem.Create(mine.Id, "Poda"));
        _db.Tasks.Add(TaskItem.Create(other.Id, "Vendimia ajena"));
        await _db.SaveChangesAsync();

        var repository = new TaskRepository(_db);

        var result = await repository.ListByWorkspaceAsync(mine.Id, null, default);

        result.Should().ContainSingle().Which.Name.Should().Be("Poda");
    }

    [Fact]
    public async Task ListByWorkspaceAsync_Deberia_ArrancarVacio()
    {
        // CA-2 — el catálogo nace vacío, sin semillas ni configuración externa
        var workspace = await SeedWorkspaceAsync("-vacio");

        var repository = new TaskRepository(_db);

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
        _db.Tasks.AddRange(activa, inactiva);
        await _db.SaveChangesAsync();

        var repository = new TaskRepository(_db);

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
        _db.Tasks.Add(ajena);
        await _db.SaveChangesAsync();

        var repository = new TaskRepository(_db);

        var found = await repository.FindByIdAsync(mine.Id, ajena.Id, default);

        found.Should().BeNull();
    }

    [Fact]
    public async Task ExistsWithNameAsync_Deberia_IgnorarMayusculas_Y_AcotarPorWorkspace()
    {
        var mine = await SeedWorkspaceAsync("-f");
        var other = await SeedWorkspaceAsync("-g");
        _db.Tasks.Add(TaskItem.Create(mine.Id, "Poda"));
        await _db.SaveChangesAsync();

        var repository = new TaskRepository(_db);

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
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        var repository = new TaskRepository(_db);

        (await repository.ExistsWithNameAsync(workspace.Id, "Poda", task.Id, default)).Should().BeFalse();
    }

    [Fact]
    public async Task ExistsWithNameAsync_Deberia_VerTareasInactivas()
    {
        // Una tarea inactivada sigue ocupando su nombre: reactivarla es mejor que duplicarla (CA-3).
        var workspace = await SeedWorkspaceAsync("-i");
        var inactiva = TaskItem.Create(workspace.Id, "Poda");
        inactiva.SetActive(false);
        _db.Tasks.Add(inactiva);
        await _db.SaveChangesAsync();

        var repository = new TaskRepository(_db);

        (await repository.ExistsWithNameAsync(workspace.Id, "poda", null, default)).Should().BeTrue();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
