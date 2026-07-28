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

    private async Task<User> SeedUserAsync(string suffix, string displayName)
    {
        var user = User.Create($"google-sub{suffix}", displayName, $"cuenta{suffix}@ejemplo.com");
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
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

    [Fact]
    public async Task ExistsWithNameAsync_Deberia_IgnorarMayusculas_Y_AcotarPorWorkspace()
    {
        // MVP-207 (CA-2) — mismo criterio que el índice único ux_workers_workspace_name.
        var mine = await SeedWorkspaceAsync("-dup-a");
        var other = await SeedWorkspaceAsync("-dup-b");
        _db.Workers.Add(Worker.Create(mine.Id, "Juan Pérez"));
        await _db.SaveChangesAsync();

        var repository = new WorkerRepository(_db);

        (await repository.ExistsWithNameAsync(mine.Id, "Juan Pérez", null, default)).Should().BeTrue();
        (await repository.ExistsWithNameAsync(mine.Id, "juan pérez", null, default)).Should().BeTrue();
        (await repository.ExistsWithNameAsync(mine.Id, "JUAN PÉREZ", null, default)).Should().BeTrue();
        (await repository.ExistsWithNameAsync(mine.Id, "Antonio", null, default)).Should().BeFalse();
        // El maestro de otro Workspace no genera conflicto (aislamiento multi-tenant).
        (await repository.ExistsWithNameAsync(other.Id, "Juan Pérez", null, default)).Should().BeFalse();
    }

    [Fact]
    public async Task ExistsWithNameAsync_Deberia_ExcluirElPropioTrabajador_Y_VerLosInactivos()
    {
        var workspace = await SeedWorkspaceAsync("-dup-c");
        var propio = Worker.Create(workspace.Id, "Juan Pérez");
        var inactivo = Worker.Create(workspace.Id, "Antonio");
        inactivo.SetActive(false);
        _db.Workers.AddRange(propio, inactivo);
        await _db.SaveChangesAsync();

        var repository = new WorkerRepository(_db);

        // Cambiar solo las mayúsculas del propio nombre no es un conflicto consigo mismo.
        (await repository.ExistsWithNameAsync(workspace.Id, "JUAN PÉREZ", propio.Id, default)).Should().BeFalse();
        // Un trabajador inactivo sigue ocupando su nombre: se reactiva, no se duplica.
        (await repository.ExistsWithNameAsync(workspace.Id, "antonio", null, default)).Should().BeTrue();
    }

    // ── MVP-208 · el maestro es la unión miembro + cuadrilla ───────────────────────────────────

    [Fact]
    public async Task ExistsWithNameAsync_Deberia_VerTambienALosMiembros()
    {
        // CA-3 (hallazgo R-16) — antes la guarda era por tabla y el miembro no estaba en ella, así que
        // dar de alta cuadrilla con su mismo nombre respondía 201 y dejaba dos personas indistinguibles.
        var workspace = await SeedWorkspaceAsync("-union");
        var cuenta = await SeedUserAsync("-union-m", "Andrés Gilabert");
        _db.Workers.Add(Worker.CreateForMember(workspace.Id, cuenta.Id, "Andrés Gilabert"));
        await _db.SaveChangesAsync();

        var repository = new WorkerRepository(_db);

        (await repository.ExistsWithNameAsync(workspace.Id, "andrés gilabert", null, default))
            .Should().BeTrue();
    }

    [Fact]
    public async Task FindByUserAccountAsync_Deberia_AcotarPorWorkspace()
    {
        var mine = await SeedWorkspaceAsync("-acc-a");
        var other = await SeedWorkspaceAsync("-acc-b");
        var cuenta = await SeedUserAsync("-acc", "Bruno");
        _db.Workers.Add(Worker.CreateForMember(mine.Id, cuenta.Id, "Bruno"));
        await _db.SaveChangesAsync();

        var repository = new WorkerRepository(_db);

        (await repository.FindByUserAccountAsync(mine.Id, cuenta.Id, default)).Should().NotBeNull();
        (await repository.FindByUserAccountAsync(other.Id, cuenta.Id, default)).Should().BeNull();
    }

    [Fact]
    public async Task ListByUserAccountAsync_Deberia_DevolverTodasSusFilas()
    {
        // La resincronización del nombre de Google (RN-036) afecta a todos sus Workspaces a la vez.
        var uno = await SeedWorkspaceAsync("-sync-a");
        var dos = await SeedWorkspaceAsync("-sync-b");
        var cuenta = await SeedUserAsync("-sync", "Clara");
        _db.Workers.Add(Worker.CreateForMember(uno.Id, cuenta.Id, "Clara"));
        _db.Workers.Add(Worker.CreateForMember(dos.Id, cuenta.Id, "Clara"));
        _db.Workers.Add(Worker.Create(uno.Id, "Cuadrilla sin cuenta"));
        await _db.SaveChangesAsync();

        var repository = new WorkerRepository(_db);

        var filas = await repository.ListByUserAccountAsync(cuenta.Id, default);

        filas.Should().HaveCount(2);
        filas.Should().OnlyContain(w => w.UserAccountId == cuenta.Id);
    }

    [Fact]
    public async Task FindByNameAsync_Deberia_DevolverAlOcupante_ParaSaberSiEsMiembroOCuadrilla()
    {
        // El desempate necesita saber **quién** ocupa el nombre: la fila que se renombra es la de
        // cuadrilla, nunca la del miembro.
        var workspace = await SeedWorkspaceAsync("-ocupante");
        var cuadrilla = Worker.Create(workspace.Id, "Andrés Gilabert");
        _db.Workers.Add(cuadrilla);
        await _db.SaveChangesAsync();

        var repository = new WorkerRepository(_db);

        var ocupante = await repository.FindByNameAsync(workspace.Id, "ANDRÉS GILABERT", null, default);

        ocupante.Should().NotBeNull();
        ocupante!.Id.Should().Be(cuadrilla.Id);
        ocupante.HasAccount.Should().BeFalse();
    }

    [Fact]
    public async Task ElIndiceUnico_Deberia_ImpedirDosFilasParaLaMismaCuenta()
    {
        // CA-1 — `user_account_id` es una identidad, no una etiqueta: ux_workers_workspace_user_account.
        var workspace = await SeedWorkspaceAsync("-idx");
        var cuenta = await SeedUserAsync("-idx-cuenta", "Diego");
        _db.Workers.Add(Worker.CreateForMember(workspace.Id, cuenta.Id, "Diego"));
        _db.Workers.Add(Worker.CreateForMember(workspace.Id, cuenta.Id, "Diego bis"));

        var act = async () => await _db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
