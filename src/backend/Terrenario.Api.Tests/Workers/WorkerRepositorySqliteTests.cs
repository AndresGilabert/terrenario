using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workers;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;

namespace Terrenario.Api.Tests.Workers;

/// <summary>
/// Tests del repositorio de trabajadores contra SQLite real (MVP-204): ejercitan la traducción a SQL
/// del filtro por estado y del aislamiento por Workspace, que los mocks no ven (lección de P-014).
/// </summary>
public sealed class WorkerRepositorySqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TerrenarioDbContext _db;

    public WorkerRepositorySqliteTests()
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
        var mine = await SeedWorkspaceAsync("-a");
        var other = await SeedWorkspaceAsync("-b");
        _db.Workers.Add(Worker.Create(mine.Id, "Antonio"));
        _db.Workers.Add(Worker.Create(other.Id, "Ajeno"));
        await _db.SaveChangesAsync();

        var repository = new WorkerRepository(_db);

        var result = await repository.ListByWorkspaceAsync(mine.Id, null, default);

        result.Should().ContainSingle().Which.Name.Should().Be("Antonio");
    }

    [Fact]
    public async Task ListByWorkspaceAsync_Deberia_FiltrarPorEstado_Y_OrdenarActivosPrimero()
    {
        var workspace = await SeedWorkspaceAsync("-c");
        var activo = Worker.Create(workspace.Id, "Beatriz");
        var inactivo = Worker.Create(workspace.Id, "Alfredo");
        inactivo.SetActive(false);
        _db.Workers.AddRange(activo, inactivo);
        await _db.SaveChangesAsync();

        var repository = new WorkerRepository(_db);

        var soloActivos = await repository.ListByWorkspaceAsync(workspace.Id, isActive: true, default);
        soloActivos.Should().ContainSingle().Which.Name.Should().Be("Beatriz");

        // Orden: activos primero aunque alfabéticamente "Alfredo" iría antes que "Beatriz".
        var todos = await repository.ListByWorkspaceAsync(workspace.Id, null, default);
        todos.Select(w => w.Name).Should().ContainInOrder("Beatriz", "Alfredo");
    }

    [Fact]
    public async Task FindByIdAsync_Deberia_NoDevolverTrabajadorDeOtroWorkspace()
    {
        var mine = await SeedWorkspaceAsync("-d");
        var other = await SeedWorkspaceAsync("-e");
        var ajeno = Worker.Create(other.Id, "Ajeno");
        _db.Workers.Add(ajeno);
        await _db.SaveChangesAsync();

        var repository = new WorkerRepository(_db);

        var found = await repository.FindByIdAsync(mine.Id, ajeno.Id, default);

        found.Should().BeNull();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
