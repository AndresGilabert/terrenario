using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;

namespace Terrenario.Api.Tests.Seasons;

/// <summary>
/// Tests del repositorio de temporadas contra SQLite real: ejercitan la traducción a SQL (que los
/// mocks no ven) y, sobre todo, la invariante RN-022 materializada como índice único parcial en la
/// base de datos (MVP-201, CA-3).
/// </summary>
public sealed class SeasonRepositorySqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TerrenarioDbContext _db;

    public SeasonRepositorySqliteTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TerrenarioDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TerrenarioDbContext(options);
        _db.Database.EnsureCreated();
    }

    private async Task<Workspace> SeedWorkspaceAsync()
    {
        var user = User.Create("google-sub", "Andrés", "andres@ejemplo.com");
        _db.Users.Add(user);
        var workspace = Workspace.Create(user.Id, "Finca El Olivar");
        _db.Workspaces.Add(workspace);
        await _db.SaveChangesAsync();
        return workspace;
    }

    [Fact]
    public async Task FindActiveByWorkspaceAsync_Deberia_DevolverLaTemporadaActiva()
    {
        var workspace = await SeedWorkspaceAsync();
        var season = Season.Create(workspace.Id, "Campaña 2026", new DateOnly(2026, 1, 1), null);
        _db.Seasons.Add(season);
        await _db.SaveChangesAsync();

        var repository = new SeasonRepository(_db);

        var active = await repository.FindActiveByWorkspaceAsync(workspace.Id);

        active.Should().NotBeNull();
        active!.Id.Should().Be(season.Id);
        active.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deberia_ImpedirDosTemporadasActivas_EnElMismoWorkspace()
    {
        // Arrange — RN-022 / CA-3: el índice único parcial no debe permitir una segunda activa
        var workspace = await SeedWorkspaceAsync();
        _db.Seasons.Add(Season.Create(workspace.Id, "Campaña 2026", new DateOnly(2026, 1, 1), null));
        await _db.SaveChangesAsync();

        _db.Seasons.Add(Season.Create(workspace.Id, "Campaña 2027", new DateOnly(2027, 1, 1), null));

        // Act
        var act = async () => await _db.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
