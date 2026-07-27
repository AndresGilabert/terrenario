using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;

namespace Terrenario.Api.Tests.Plots;

/// <summary>
/// Tests del repositorio de terrenos contra SQLite real (MVP-202): ejercitan la traducción a SQL de
/// los filtros de listado y del aislamiento por Workspace, que los mocks no ven (lección de P-014).
/// </summary>
public sealed class PlotRepositorySqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TerrenarioDbContext _db;

    public PlotRepositorySqliteTests()
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
        _db.Plots.Add(Plot.Create(mine.Id, "La Hoya", PlotOwnershipTypes.Propia));
        _db.Plots.Add(Plot.Create(other.Id, "Ajena", PlotOwnershipTypes.Cedida));
        await _db.SaveChangesAsync();

        var repository = new PlotRepository(_db);

        var result = await repository.ListByWorkspaceAsync(mine.Id, null, null, default);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("La Hoya");
    }

    [Fact]
    public async Task ListByWorkspaceAsync_Deberia_FiltrarPorEstadoYBusqueda()
    {
        var workspace = await SeedWorkspaceAsync("-c");
        var activo = Plot.Create(workspace.Id, "Olivar Alto", PlotOwnershipTypes.Propia, location: "Sector Norte");
        var inactivo = Plot.Create(workspace.Id, "Olivar Bajo", PlotOwnershipTypes.Propia);
        inactivo.SetActive(false);
        _db.Plots.AddRange(activo, inactivo);
        await _db.SaveChangesAsync();

        var repository = new PlotRepository(_db);

        // Filtro por estado
        var soloActivos = await repository.ListByWorkspaceAsync(workspace.Id, null, isActive: true, default);
        soloActivos.Should().ContainSingle().Which.Name.Should().Be("Olivar Alto");

        // Búsqueda por texto (nombre/alias/ubicación), insensible a mayúsculas
        var porUbicacion = await repository.ListByWorkspaceAsync(workspace.Id, "norte", null, default);
        porUbicacion.Should().ContainSingle().Which.Name.Should().Be("Olivar Alto");

        // Orden: activos primero
        var todos = await repository.ListByWorkspaceAsync(workspace.Id, null, null, default);
        todos.Select(p => p.Name).Should().ContainInOrder("Olivar Alto", "Olivar Bajo");
    }

    [Fact]
    public async Task FindByIdAsync_Deberia_NoDevolverTerrenoDeOtroWorkspace()
    {
        var mine = await SeedWorkspaceAsync("-d");
        var other = await SeedWorkspaceAsync("-e");
        var ajeno = Plot.Create(other.Id, "Ajena", PlotOwnershipTypes.Cedida);
        _db.Plots.Add(ajeno);
        await _db.SaveChangesAsync();

        var repository = new PlotRepository(_db);

        var found = await repository.FindByIdAsync(mine.Id, ajeno.Id, default);

        found.Should().BeNull();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
