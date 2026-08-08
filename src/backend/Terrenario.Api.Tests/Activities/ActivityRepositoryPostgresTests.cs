using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Activities;
using Terrenario.Api.Domain.Operations;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Tasks;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workers;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Tests.Integration;

namespace Terrenario.Api.Tests.Activities;

/// <summary>
/// Tests del repositorio de actividades contra PostgreSQL real (MVP-301): ejercitan la traducción a SQL
/// de la proyección con <c>JOIN</c> a tres maestros más el <c>LEFT JOIN</c> al catálogo de tareas,
/// del filtro de baja lógica, de los filtros del listado y del orden por fecha de negocio. Los mocks
/// no ven nada de esto (lección de P-014).
/// </summary>
public sealed class ActivityRepositoryPostgresTests : RepositoryTestBase
{
    private readonly Guid _userId = Guid.NewGuid();

    private sealed record Fixture(Workspace Workspace, Plot Plot, Season Season, Worker Worker);

    private async Task<Fixture> SeedAsync(string suffix)
    {
        var user = User.Create($"google-sub{suffix}", "Andrés", $"andres{suffix}@ejemplo.com");
        Db.Users.Add(user);
        var workspace = Workspace.Create(user.Id, $"Finca El Olivar {suffix}");
        Db.Workspaces.Add(workspace);

        var plot = Plot.Create(workspace.Id, $"Olivar Alto {suffix}", "propia");
        var season = Season.Create(
            workspace.Id, $"2026/2027 {suffix}", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28));
        var worker = Worker.Create(workspace.Id, $"Antonio {suffix}");
        Db.Plots.Add(plot);
        Db.Seasons.Add(season);
        Db.Workers.Add(worker);

        await Db.SaveChangesAsync();

        return new Fixture(workspace, plot, season, worker);
    }

    private Activity NewActivity(
        Fixture fixture,
        DateOnly date,
        Guid? taskId = null,
        string? taskText = "Poda")
        => Activity.Create(
            fixture.Workspace.Id, fixture.Plot.Id, fixture.Season.Id, fixture.Worker.Id,
            date, 4m, taskId, taskText, 70m, null, _userId);

    [Fact]
    public async Task ListAsync_Deberia_ResolverNombresDeLosMaestros()
    {
        var fixture = await SeedAsync("-a");
        var task = TaskItem.Create(fixture.Workspace.Id, "Poda de mantenimiento");
        Db.Tasks.Add(task);
        Db.Activities.Add(NewActivity(fixture, new DateOnly(2026, 10, 5), task.Id, null));
        await Db.SaveChangesAsync();

        var repository = new ActivityRepository(Db);

        var result = await repository.ListAsync(fixture.Workspace.Id, new ActivityFilter());

        var view = result.Should().ContainSingle().Which;
        view.PlotName.Should().Be("Olivar Alto -a");
        view.WorkerName.Should().Be("Antonio -a");
        view.SeasonName.Should().Be("2026/2027 -a");
        view.TaskName.Should().Be("Poda de mantenimiento");
        view.Task.Should().Be("Poda de mantenimiento");
    }

    [Fact]
    public async Task ListAsync_Deberia_ResolverTareaLibre_ConLeftJoin()
    {
        // RN-025 — una actividad con tarea en texto libre no tiene fila en el catálogo: el LEFT JOIN
        // es lo que impide que desaparezca del diario.
        var fixture = await SeedAsync("-b");
        Db.Activities.Add(NewActivity(fixture, new DateOnly(2026, 10, 5), null, "Riego de emergencia"));
        await Db.SaveChangesAsync();

        var repository = new ActivityRepository(Db);

        var view = (await repository.ListAsync(fixture.Workspace.Id, new ActivityFilter()))
            .Should().ContainSingle().Which;
        view.TaskName.Should().BeNull();
        view.TaskText.Should().Be("Riego de emergencia");
        view.Task.Should().Be("Riego de emergencia");
    }

    [Fact]
    public async Task ListAsync_Deberia_AislarPorWorkspace()
    {
        var mine = await SeedAsync("-c");
        var other = await SeedAsync("-d");
        Db.Activities.Add(NewActivity(mine, new DateOnly(2026, 10, 5)));
        Db.Activities.Add(NewActivity(other, new DateOnly(2026, 10, 6)));
        await Db.SaveChangesAsync();

        var repository = new ActivityRepository(Db);

        (await repository.ListAsync(mine.Workspace.Id, new ActivityFilter()))
            .Should().ContainSingle().Which.PlotName.Should().Be("Olivar Alto -c");
    }

    [Fact]
    public async Task ListAsync_Deberia_ExcluirLasEliminadasLogicamente()
    {
        // RN-037 — lo eliminado deja de aparecer en el diario y en los listados, sin borrarse
        var fixture = await SeedAsync("-e");
        var viva = NewActivity(fixture, new DateOnly(2026, 10, 5));
        var borrada = NewActivity(fixture, new DateOnly(2026, 10, 6));
        borrada.Delete(_userId);
        Db.Activities.AddRange(viva, borrada);
        await Db.SaveChangesAsync();

        var repository = new ActivityRepository(Db);

        (await repository.ListAsync(fixture.Workspace.Id, new ActivityFilter()))
            .Should().ContainSingle().Which.Id.Should().Be(viva.Id);
        (await repository.FindByIdAsync(fixture.Workspace.Id, borrada.Id)).Should().BeNull();
        // …pero la fila sigue en base de datos.
        (await Db.Activities.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task ListAsync_Deberia_OrdenarPorFechaDeNegocioDescendente()
    {
        // RN-033 — el diario ordena por fecha de negocio, no por fecha de captura: la más antigua se
        // captura la última y aun así queda debajo.
        var fixture = await SeedAsync("-f");
        var antigua = NewActivity(fixture, new DateOnly(2026, 10, 1));
        var reciente = NewActivity(fixture, new DateOnly(2026, 10, 20));
        Db.Activities.Add(reciente);
        await Db.SaveChangesAsync();
        Db.Activities.Add(antigua);
        await Db.SaveChangesAsync();

        var repository = new ActivityRepository(Db);

        (await repository.ListAsync(fixture.Workspace.Id, new ActivityFilter()))
            .Select(v => v.Id).Should().ContainInOrder(reciente.Id, antigua.Id);
    }

    [Fact]
    public async Task ListAsync_Deberia_FiltrarPorRangoDeFechas_Terreno_Temporada_Y_Responsable()
    {
        var fixture = await SeedAsync("-g");
        var otroTerreno = Plot.Create(fixture.Workspace.Id, "Olivar Bajo -g", "cedida");
        Db.Plots.Add(otroTerreno);
        await Db.SaveChangesAsync();

        var enRango = NewActivity(fixture, new DateOnly(2026, 10, 10));
        var fueraDeRango = NewActivity(fixture, new DateOnly(2026, 12, 1));
        var enOtroTerreno = Activity.Create(
            fixture.Workspace.Id, otroTerreno.Id, fixture.Season.Id, fixture.Worker.Id,
            new DateOnly(2026, 10, 11), 2m, null, "Riego", 10m, null, _userId);
        Db.Activities.AddRange(enRango, fueraDeRango, enOtroTerreno);
        await Db.SaveChangesAsync();

        var repository = new ActivityRepository(Db);
        var workspaceId = fixture.Workspace.Id;

        (await repository.ListAsync(workspaceId, new ActivityFilter(
                From: new DateOnly(2026, 10, 1), To: new DateOnly(2026, 10, 31))))
            .Should().HaveCount(2);

        (await repository.ListAsync(workspaceId, new ActivityFilter(PlotId: otroTerreno.Id)))
            .Should().ContainSingle().Which.Id.Should().Be(enOtroTerreno.Id);

        (await repository.ListAsync(workspaceId, new ActivityFilter(SeasonId: fixture.Season.Id)))
            .Should().HaveCount(3);

        (await repository.ListAsync(workspaceId, new ActivityFilter(WorkerId: fixture.Worker.Id)))
            .Should().HaveCount(3);
    }

    [Fact]
    public async Task GetViewAsync_Deberia_SenalarLaFechaFueraDelRangoDeLaTemporada()
    {
        // RN-023 — aviso, nunca bloqueo: la actividad se guarda igual y el diario la marca
        var fixture = await SeedAsync("-h");
        var dentro = NewActivity(fixture, new DateOnly(2026, 10, 5));
        var fuera = NewActivity(fixture, new DateOnly(2026, 8, 15));
        Db.Activities.AddRange(dentro, fuera);
        await Db.SaveChangesAsync();

        var repository = new ActivityRepository(Db);

        (await repository.GetViewAsync(fixture.Workspace.Id, dentro.Id))!
            .IsOutOfSeasonRange.Should().BeFalse();
        (await repository.GetViewAsync(fixture.Workspace.Id, fuera.Id))!
            .IsOutOfSeasonRange.Should().BeTrue();
    }

    [Fact]
    public async Task FindByIdAsync_Deberia_NoDevolverActividadDeOtroWorkspace()
    {
        var mine = await SeedAsync("-i");
        var other = await SeedAsync("-j");
        var ajena = NewActivity(other, new DateOnly(2026, 10, 5));
        Db.Activities.Add(ajena);
        await Db.SaveChangesAsync();

        var repository = new ActivityRepository(Db);

        (await repository.FindByIdAsync(mine.Workspace.Id, ajena.Id)).Should().BeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_Deberia_Traducir_LaColisionDeVersion_A_Conflicto()
    {
        // ADR-0005 — dos escrituras simultáneas partiendo de la misma versión: la segunda no puede
        // acabar en un 500. El token de concurrencia de EF es la última línea de defensa.
        var fixture = await SeedAsync("-k");
        var activity = NewActivity(fixture, new DateOnly(2026, 10, 5));
        Db.Activities.Add(activity);
        await Db.SaveChangesAsync();

        // Otra sesión sube la versión por debajo, sin pasar por el contexto en curso.
        await Db.Database.ExecuteSqlRawAsync(
            "UPDATE activities SET version = version + 1 WHERE id = {0}", activity.Id);

        activity.Update(
            fixture.Plot.Id, fixture.Season.Id, fixture.Worker.Id,
            new DateOnly(2026, 10, 5), 5m, null, "Poda", 70m, null, _userId);

        var repository = new ActivityRepository(Db);

        await repository.Invoking(r => r.SaveChangesAsync())
            .Should().ThrowAsync<ConcurrencyConflictException>();
    }

}
