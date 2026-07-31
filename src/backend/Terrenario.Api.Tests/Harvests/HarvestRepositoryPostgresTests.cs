using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Harvests;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Tests.Integration;

namespace Terrenario.Api.Tests.Harvests;

/// <summary>
/// Tests del repositorio de cosechas contra PostgreSQL real (MVP-401): ejercitan la traducción a SQL de
/// la proyección con <c>JOIN</c> a terreno y temporada, del filtro de baja lógica, de los filtros del
/// listado y del orden por fecha de negocio. Los mocks no ven nada de esto (lección de P-014).
/// </summary>
public sealed class HarvestRepositoryPostgresTests : RepositoryTestBase
{
    private readonly Guid _userId;

    private sealed record Fixture(Workspace Workspace, Plot Plot, Season Season);

    private async Task<Fixture> SeedAsync(string suffix)
    {
        var user = User.Create($"google-sub{suffix}", "Andrés", $"andres{suffix}@ejemplo.com");
        Db.Users.Add(user);
        var workspace = Workspace.Create(user.Id, $"Finca El Olivar {suffix}");
        Db.Workspaces.Add(workspace);

        var plot = Plot.Create(workspace.Id, $"Olivar Alto {suffix}", "propia");
        var season = Season.Create(
            workspace.Id, $"2026/2027 {suffix}", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28));
        Db.Plots.Add(plot);
        Db.Seasons.Add(season);

        await Db.SaveChangesAsync();

        return new Fixture(workspace, plot, season);
    }

    private Harvest NewHarvest(
        Fixture fixture,
        DateOnly date,
        decimal kgs = 1200m,
        string destination = "aceite_para_venta",
        Guid? plotId = null)
        => Harvest.Create(
            fixture.Workspace.Id, plotId ?? fixture.Plot.Id, fixture.Season.Id, date,
            "aceituna_olivar", kgs, destination, 18.5m, null, _userId);

    [Fact]
    public async Task ListAsync_Deberia_ResolverNombresDeLosMaestros()
    {
        var fixture = await SeedAsync("-a");
        Db.Harvests.Add(NewHarvest(fixture, new DateOnly(2026, 10, 20)));
        await Db.SaveChangesAsync();

        var view = (await new HarvestRepository(Db).ListAsync(fixture.Workspace.Id, new HarvestFilter()))
            .Should().ContainSingle().Which;

        view.PlotName.Should().Be("Olivar Alto -a");
        view.SeasonName.Should().Be("2026/2027 -a");
        view.Kgs.Should().Be(1200m);
        view.Yield.Should().Be(18.5m);
        view.Liters.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_NoDeberia_DevolverLasEliminadas()
    {
        // RN-037 — el filtro de baja lógica vive en el puerto, así que listado, diario y dashboard lo
        // heredan sin repetirlo
        var fixture = await SeedAsync("-b");
        var viva = NewHarvest(fixture, new DateOnly(2026, 10, 20));
        var borrada = NewHarvest(fixture, new DateOnly(2026, 10, 21));
        borrada.Delete(_userId);
        Db.Harvests.AddRange(viva, borrada);
        await Db.SaveChangesAsync();

        var result = await new HarvestRepository(Db).ListAsync(fixture.Workspace.Id, new HarvestFilter());

        result.Should().ContainSingle().Which.Id.Should().Be(viva.Id);
    }

    [Fact]
    public async Task ListAsync_Deberia_OrdenarPorFechaDeNegocioDescendente()
    {
        // RN-033 — la fecha que ordena es la de la cosecha, no la de captura
        var fixture = await SeedAsync("-c");
        Db.Harvests.Add(NewHarvest(fixture, new DateOnly(2026, 10, 1)));
        Db.Harvests.Add(NewHarvest(fixture, new DateOnly(2026, 11, 5)));
        Db.Harvests.Add(NewHarvest(fixture, new DateOnly(2026, 10, 20)));
        await Db.SaveChangesAsync();

        var result = await new HarvestRepository(Db).ListAsync(fixture.Workspace.Id, new HarvestFilter());

        result.Select(v => v.Date).Should().ContainInOrder(
            new DateOnly(2026, 11, 5), new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 1));
    }

    [Fact]
    public async Task ListAsync_Deberia_FiltrarPorDestino_ConComparacionExacta()
    {
        // RN-012 — el destino es catálogo cerrado: comparación exacta, no parcial como el material
        // libre de las compras (RN-031)
        var fixture = await SeedAsync("-d");
        Db.Harvests.Add(NewHarvest(fixture, new DateOnly(2026, 10, 20), destination: "aceite_para_venta"));
        Db.Harvests.Add(NewHarvest(fixture, new DateOnly(2026, 10, 21), destination: "desconocido"));
        await Db.SaveChangesAsync();

        var result = await new HarvestRepository(Db).ListAsync(
            fixture.Workspace.Id, new HarvestFilter(Destination: "desconocido"));

        result.Should().ContainSingle().Which.Destination.Should().Be("desconocido");
    }

    [Fact]
    public async Task ListAsync_Deberia_FiltrarPorRangoDeFechas_Y_PorTerreno()
    {
        var fixture = await SeedAsync("-e");
        var otroTerreno = Plot.Create(fixture.Workspace.Id, "Olivar Bajo -e", "propia");
        Db.Plots.Add(otroTerreno);
        Db.Harvests.Add(NewHarvest(fixture, new DateOnly(2026, 10, 20)));
        Db.Harvests.Add(NewHarvest(fixture, new DateOnly(2026, 12, 20)));
        Db.Harvests.Add(NewHarvest(fixture, new DateOnly(2026, 10, 22), plotId: otroTerreno.Id));
        await Db.SaveChangesAsync();

        var repository = new HarvestRepository(Db);

        var porFecha = await repository.ListAsync(
            fixture.Workspace.Id,
            new HarvestFilter(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)));
        porFecha.Should().HaveCount(2);

        var porTerreno = await repository.ListAsync(
            fixture.Workspace.Id, new HarvestFilter(PlotId: otroTerreno.Id));
        porTerreno.Should().ContainSingle().Which.PlotName.Should().Be("Olivar Bajo -e");
    }

    [Fact]
    public async Task ListAsync_NoDeberia_DevolverCosechasDeOtroWorkspace()
    {
        // Aislamiento multi-tenant: la consulta siempre acota por Workspace
        var propio = await SeedAsync("-f");
        var ajeno = await SeedAsync("-g");
        Db.Harvests.Add(NewHarvest(propio, new DateOnly(2026, 10, 20)));
        Db.Harvests.Add(NewHarvest(ajeno, new DateOnly(2026, 10, 21)));
        await Db.SaveChangesAsync();

        var result = await new HarvestRepository(Db).ListAsync(propio.Workspace.Id, new HarvestFilter());

        result.Should().ContainSingle().Which.WorkspaceId.Should().Be(propio.Workspace.Id);
    }

    [Fact]
    public async Task GetViewAsync_Deberia_MarcarLaFechaFueraDeRango()
    {
        // RN-023 — el aviso se calcula en lectura, para que valga también si la temporada se edita
        // después de registrar la cosecha
        var fixture = await SeedAsync("-h");
        Db.Harvests.Add(NewHarvest(fixture, new DateOnly(2026, 10, 20)));
        var fuera = NewHarvest(fixture, new DateOnly(2028, 5, 1));
        Db.Harvests.Add(fuera);
        await Db.SaveChangesAsync();

        var repository = new HarvestRepository(Db);

        (await repository.GetViewAsync(fixture.Workspace.Id, fuera.Id))!
            .IsOutOfSeasonRange.Should().BeTrue();
        (await repository.ListAsync(fixture.Workspace.Id, new HarvestFilter()))
            .Single(v => v.Date == new DateOnly(2026, 10, 20))
            .IsOutOfSeasonRange.Should().BeFalse();
    }

    [Fact]
    public async Task FindByIdAsync_NoDeberia_DevolverUnaCosechaEliminada()
    {
        var fixture = await SeedAsync("-i");
        var harvest = NewHarvest(fixture, new DateOnly(2026, 10, 20));
        harvest.Delete(_userId);
        Db.Harvests.Add(harvest);
        await Db.SaveChangesAsync();

        var found = await new HarvestRepository(Db).FindByIdAsync(fixture.Workspace.Id, harvest.Id);

        found.Should().BeNull();
    }

}
