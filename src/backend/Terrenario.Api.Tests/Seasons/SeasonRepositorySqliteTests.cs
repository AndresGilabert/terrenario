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
/// Tests del repositorio de temporadas contra SQLite real (MVP-203 · MVP-209): ejercitan la traducción
/// a SQL (que los mocks no ven) y, sobre todo, la <b>temporada de trabajo por usuario</b> —resuelta
/// desde <c>workspace_members.active_season_id</c> con su regla de defecto— y su <b>aislamiento entre
/// usuarios</b>.
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

    private sealed record Fixture(Workspace Workspace, Guid UserId);

    /// <summary>Workspace con su membresía de propietario, que es donde vive la temporada de trabajo.</summary>
    private async Task<Fixture> SeedWorkspaceAsync(string suffix = "")
    {
        await using var db = NewDb();
        var user = User.Create($"google-sub{suffix}", $"Andrés{suffix}", $"andres{suffix}@ejemplo.com");
        db.Users.Add(user);
        var workspace = Workspace.Create(user.Id, $"Finca El Olivar{suffix}");
        db.Workspaces.Add(workspace);
        db.WorkspaceMembers.Add(WorkspaceMember.CreateOwner(workspace.Id, user.Id));
        await db.SaveChangesAsync();
        return new Fixture(workspace, user.Id);
    }

    private async Task<Guid> AddMemberAsync(Guid workspaceId, string suffix)
    {
        await using var db = NewDb();
        var user = User.Create($"google-sub{suffix}", $"Otro{suffix}", $"otro{suffix}@ejemplo.com");
        db.Users.Add(user);
        db.WorkspaceMembers.Add(WorkspaceMember.CreateMember(workspaceId, user.Id));
        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<Season> AddSeasonAsync(Guid workspaceId, string name, DateOnly start, DateOnly? end, bool closed = false)
    {
        var season = Season.Create(workspaceId, name, start, end);
        if (closed) season.Close();
        await using var db = NewDb();
        db.Seasons.Add(season);
        await db.SaveChangesAsync();
        return season;
    }

    // ── Temporada de trabajo por usuario (MVP-209) ──────────────────────────

    [Fact]
    public async Task FindWorkingSeasonAsync_Deberia_DevolverLaFijadaEnLaMembresia()
    {
        var f = await SeedWorkspaceAsync();
        await AddSeasonAsync(f.Workspace.Id, "Campaña 2025", new DateOnly(2025, 1, 1), null);
        var elegida = await AddSeasonAsync(f.Workspace.Id, "Campaña 2026", new DateOnly(2026, 1, 1), null);

        await using (var db = NewDb())
            await new SeasonRepository(db).SetWorkingSeasonAsync(f.UserId, f.Workspace.Id, elegida.Id);

        await using var verify = NewDb();
        var working = await new SeasonRepository(verify).FindWorkingSeasonAsync(f.UserId, f.Workspace.Id);

        working!.Id.Should().Be(elegida.Id);
    }

    [Fact]
    public async Task FindWorkingSeasonAsync_Deberia_ResolverDefecto_SiNoHayFijada()
    {
        // Sin nada fijado en la membresía, la regla de defecto elige la abierta más reciente (iniciada).
        var f = await SeedWorkspaceAsync();
        await AddSeasonAsync(f.Workspace.Id, "Campaña 2023", new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));
        var reciente = await AddSeasonAsync(f.Workspace.Id, "Campaña 2025", new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));

        await using var db = NewDb();
        var working = await new SeasonRepository(db).FindWorkingSeasonAsync(f.UserId, f.Workspace.Id);

        working!.Id.Should().Be(reciente.Id);
    }

    [Fact]
    public async Task FindWorkingSeasonAsync_Deberia_DevolverNull_SiNoHayTemporadas()
    {
        var f = await SeedWorkspaceAsync();

        await using var db = NewDb();
        var working = await new SeasonRepository(db).FindWorkingSeasonAsync(f.UserId, f.Workspace.Id);

        working.Should().BeNull();
    }

    [Fact]
    public async Task SetWorkingSeasonAsync_Deberia_AfectarSoloAlUsuarioIndicado()
    {
        // CA-2 — fijar la de un usuario no cambia la de otro miembro del mismo Workspace.
        var f = await SeedWorkspaceAsync();
        var otro = await AddMemberAsync(f.Workspace.Id, "-2");
        var s2025 = await AddSeasonAsync(f.Workspace.Id, "Campaña 2025", new DateOnly(2025, 1, 1), null);
        var s2026 = await AddSeasonAsync(f.Workspace.Id, "Campaña 2026", new DateOnly(2026, 1, 1), null);

        await using (var db = NewDb())
        {
            var repository = new SeasonRepository(db);
            await repository.SetWorkingSeasonAsync(f.UserId, f.Workspace.Id, s2025.Id);
            await repository.SetWorkingSeasonAsync(otro, f.Workspace.Id, s2026.Id);
        }

        await using var verify = NewDb();
        var repo = new SeasonRepository(verify);
        (await repo.FindWorkingSeasonAsync(f.UserId, f.Workspace.Id))!.Id.Should().Be(s2025.Id);
        (await repo.FindWorkingSeasonAsync(otro, f.Workspace.Id))!.Id.Should().Be(s2026.Id);
    }

    [Fact]
    public async Task Borrar_LaTemporadaDeTrabajo_Deberia_DejarLaMembresiaEnDefecto()
    {
        // FK ON DELETE SET NULL: borrar la fijada no deja una referencia colgada; se cae al defecto.
        var f = await SeedWorkspaceAsync();
        var abierta = await AddSeasonAsync(f.Workspace.Id, "Campaña 2025", new DateOnly(2025, 1, 1), null);
        var aBorrar = await AddSeasonAsync(f.Workspace.Id, "Campaña 2020", new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31));

        await using (var db = NewDb())
            await new SeasonRepository(db).SetWorkingSeasonAsync(f.UserId, f.Workspace.Id, aBorrar.Id);

        await using (var db = NewDb())
        {
            var s = await db.Seasons.FirstAsync(x => x.Id == aBorrar.Id);
            db.Seasons.Remove(s);
            await db.SaveChangesAsync();
        }

        await using var verify = NewDb();
        var working = await new SeasonRepository(verify).FindWorkingSeasonAsync(f.UserId, f.Workspace.Id);
        working!.Id.Should().Be(abierta.Id);
    }

    [Fact]
    public async Task ListByWorkspaceAsync_Deberia_DevolverLasNoCerradasPrimero()
    {
        // MVP-209 — el orden ya no depende de «activa»: no cerradas arriba, por fecha descendente.
        var f = await SeedWorkspaceAsync();
        var cerrada = await AddSeasonAsync(f.Workspace.Id, "Campaña 2024", new DateOnly(2024, 1, 1), null, closed: true);
        var abierta = await AddSeasonAsync(f.Workspace.Id, "Campaña 2026", new DateOnly(2026, 1, 1), null);

        await using var verify = NewDb();
        var list = await new SeasonRepository(verify).ListByWorkspaceAsync(f.Workspace.Id);

        list.Should().HaveCount(2);
        list[0].Id.Should().Be(abierta.Id);
        list[0].IsClosed.Should().BeFalse();
        list[1].Id.Should().Be(cerrada.Id);
        list[1].IsClosed.Should().BeTrue();
    }

    // ── Nombre único por Workspace (MVP-207) ────────────────────────────────

    [Fact]
    public async Task ExistsWithNameAsync_Deberia_IgnorarMayusculas_Y_AcotarPorWorkspace()
    {
        var mine = await SeedWorkspaceAsync();
        var other = await SeedWorkspaceAsync("-otro");
        await AddSeasonAsync(mine.Workspace.Id, "2025/2026", new DateOnly(2025, 9, 1), null);

        await using var db = NewDb();
        var repository = new SeasonRepository(db);

        (await repository.ExistsWithNameAsync(mine.Workspace.Id, "2025/2026", null, default)).Should().BeTrue();
        (await repository.ExistsWithNameAsync(mine.Workspace.Id, "Campaña 2026", null, default)).Should().BeFalse();
        // El maestro de otro Workspace no genera conflicto (aislamiento multi-tenant).
        (await repository.ExistsWithNameAsync(other.Workspace.Id, "2025/2026", null, default)).Should().BeFalse();
    }

    [Fact]
    public async Task ExistsWithNameAsync_Deberia_ExcluirLaPropiaTemporada_Y_VerLasCerradas()
    {
        var f = await SeedWorkspaceAsync();
        var propia = await AddSeasonAsync(f.Workspace.Id, "Campaña 2026", new DateOnly(2026, 1, 1), null);
        await AddSeasonAsync(f.Workspace.Id, "Campaña 2025", new DateOnly(2025, 1, 1), null, closed: true);

        await using var db = NewDb();
        var repository = new SeasonRepository(db);

        // Cambiar solo las mayúsculas del propio nombre no es un conflicto consigo misma.
        (await repository.ExistsWithNameAsync(f.Workspace.Id, "CAMPAÑA 2026", propia.Id, default)).Should().BeFalse();
        // Cerrar una temporada no libera su nombre: la guarda cubre todo el maestro.
        (await repository.ExistsWithNameAsync(f.Workspace.Id, "campaña 2025", null, default)).Should().BeTrue();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
