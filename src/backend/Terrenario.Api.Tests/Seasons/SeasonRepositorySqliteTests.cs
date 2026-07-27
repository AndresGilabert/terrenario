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
/// base de datos, incluido el <b>cambio de temporada activa</b> del maestro (MVP-203 HU-2), que debe
/// desbancar a la anterior sin violar el índice ni de forma transitoria.
///
/// Se usa un <see cref="TerrenarioDbContext"/> nuevo por operación (compartiendo la conexión en
/// memoria), reproduciendo el ámbito por petición de producción y evitando artefactos del identity-map.
/// </summary>
public sealed class SeasonRepositorySqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TerrenarioDbContext> _options;

    public SeasonRepositorySqliteTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<TerrenarioDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = NewDb();
        db.Database.EnsureCreated();
    }

    private TerrenarioDbContext NewDb() => new(_options);

    private async Task<Workspace> SeedWorkspaceAsync()
    {
        await using var db = NewDb();
        var user = User.Create("google-sub", "Andrés", "andres@ejemplo.com");
        db.Users.Add(user);
        var workspace = Workspace.Create(user.Id, "Finca El Olivar");
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();
        return workspace;
    }

    [Fact]
    public async Task FindActiveByWorkspaceAsync_Deberia_DevolverLaTemporadaActiva()
    {
        var workspace = await SeedWorkspaceAsync();
        var season = Season.Create(workspace.Id, "Campaña 2026", new DateOnly(2026, 1, 1), null);
        await using (var seed = NewDb())
        {
            seed.Seasons.Add(season);
            await seed.SaveChangesAsync();
        }

        await using var db = NewDb();
        var active = await new SeasonRepository(db).FindActiveByWorkspaceAsync(workspace.Id);

        active.Should().NotBeNull();
        active!.Id.Should().Be(season.Id);
        active.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deberia_ImpedirDosTemporadasActivas_EnElMismoWorkspace()
    {
        // RN-022 / CA-3: el índice único parcial no debe permitir insertar una segunda activa directa.
        var workspace = await SeedWorkspaceAsync();
        await using (var seed = NewDb())
        {
            seed.Seasons.Add(Season.Create(workspace.Id, "Campaña 2026", new DateOnly(2026, 1, 1), null));
            await seed.SaveChangesAsync();
        }

        await using var db = NewDb();
        db.Seasons.Add(Season.Create(workspace.Id, "Campaña 2027", new DateOnly(2027, 1, 1), null));

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ActivateExclusivelyAsync_Deberia_DesbancarLaActivaAnterior_SinViolarElIndice()
    {
        // RN-022 / MVP-203 HU-2: crear otra activa debe dejar UNA sola activa, sin excepción.
        var workspace = await SeedWorkspaceAsync();

        var first = Season.Create(workspace.Id, "Campaña 2026", new DateOnly(2026, 1, 1), null);
        await using (var db = NewDb())
            await new SeasonRepository(db).ActivateExclusivelyAsync(first, isNew: true);

        var second = Season.Create(workspace.Id, "Campaña 2027", new DateOnly(2027, 1, 1), null);
        await using (var db = NewDb())
        {
            var act = async () => await new SeasonRepository(db).ActivateExclusivelyAsync(second, isNew: true);
            await act.Should().NotThrowAsync();
        }

        await using var verify = NewDb();
        var actives = await verify.Seasons.Where(s => s.WorkspaceId == workspace.Id && s.IsActive).ToListAsync();
        actives.Should().ContainSingle().Which.Id.Should().Be(second.Id);
    }

    [Fact]
    public async Task ActivateExclusivelyAsync_Deberia_ReactivarUnaExistente_DesbancandoLaActual()
    {
        // Dos temporadas: la 2027 activa, la 2026 planificada; el usuario reactiva la 2026.
        var workspace = await SeedWorkspaceAsync();
        var older = Season.Create(workspace.Id, "Campaña 2026", new DateOnly(2026, 1, 1), null);
        var newer = Season.Create(workspace.Id, "Campaña 2027", new DateOnly(2027, 1, 1), null);

        await using (var db = NewDb())
            await new SeasonRepository(db).ActivateExclusivelyAsync(older, isNew: true);
        await using (var db = NewDb())
            await new SeasonRepository(db).ActivateExclusivelyAsync(newer, isNew: true); // older → planificada

        await using (var db = NewDb())
        {
            var repository = new SeasonRepository(db);
            var target = await repository.FindByIdAsync(workspace.Id, older.Id);
            target!.Activate();
            await repository.ActivateExclusivelyAsync(target, isNew: false);
        }

        await using var verify = NewDb();
        var actives = await verify.Seasons.Where(s => s.WorkspaceId == workspace.Id && s.IsActive).ToListAsync();
        actives.Should().ContainSingle().Which.Id.Should().Be(older.Id);
    }

    [Fact]
    public async Task ListByWorkspaceAsync_Deberia_DevolverLaActivaPrimero()
    {
        var workspace = await SeedWorkspaceAsync();

        var closed = Season.Create(workspace.Id, "Campaña 2024", new DateOnly(2024, 1, 1), null);
        closed.Close();
        await using (var seed = NewDb())
        {
            seed.Seasons.Add(closed);
            await seed.SaveChangesAsync();
        }

        var active = Season.Create(workspace.Id, "Campaña 2026", new DateOnly(2026, 1, 1), null);
        await using (var db = NewDb())
            await new SeasonRepository(db).ActivateExclusivelyAsync(active, isNew: true);

        await using var verify = NewDb();
        var list = await new SeasonRepository(verify).ListByWorkspaceAsync(workspace.Id);

        list.Should().HaveCount(2);
        list[0].Id.Should().Be(active.Id);
        list[0].IsActive.Should().BeTrue();
        list[1].Id.Should().Be(closed.Id);
        list[1].IsClosed.Should().BeTrue();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
