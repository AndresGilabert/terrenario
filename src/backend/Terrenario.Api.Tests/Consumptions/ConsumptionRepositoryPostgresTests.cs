using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Purchases;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Tests.Integration;

namespace Terrenario.Api.Tests.Consumptions;

/// <summary>
/// Tests del repositorio de consumos contra PostgreSQL real (MVP-304): traducción a SQL de la proyección,
/// del filtro de baja lógica, del orden por fecha de negocio (CA-4) y —lo más delicado— de las
/// **sumas por compra** que sostienen la guarda de sobre-imputación y el «imputado / total» del libro.
/// </summary>
public sealed class ConsumptionRepositoryPostgresTests : RepositoryTestBase
{
    private readonly Guid _userId = Guid.NewGuid();

    private sealed record Fixture(Workspace Workspace, Season Season, Plot Plot, Purchase Purchase);

    private async Task<Fixture> SeedAsync(string suffix)
    {
        var user = User.Create($"google-sub{suffix}", "Andrés", $"andres{suffix}@ejemplo.com");
        Db.Users.Add(user);
        var workspace = Workspace.Create(user.Id, $"Finca {suffix}");
        Db.Workspaces.Add(workspace);
        var season = Season.Create(
            workspace.Id, $"2026/2027 {suffix}", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28));
        Db.Seasons.Add(season);
        var plot = Plot.Create(workspace.Id, $"Olivar Alto {suffix}", "propia");
        Db.Plots.Add(plot);
        await Db.SaveChangesAsync();

        var purchase = Purchase.Create(
            workspace.Id, season.Id, new DateOnly(2026, 10, 1), "Abono NPK", 500m, 250m, _userId);
        Db.Purchases.Add(purchase);
        await Db.SaveChangesAsync();

        return new Fixture(workspace, season, plot, purchase);
    }

    private PurchaseConsumption Imputed(Fixture fixture, DateOnly date, decimal quantity = 100m)
        => PurchaseConsumption.ImputeFromPurchase(
            fixture.Workspace.Id, fixture.Purchase.Id, fixture.Season.Id, fixture.Purchase.Product,
            fixture.Purchase.UnitPrice, fixture.Plot.Id, date, quantity, _userId);

    private PurchaseConsumption WithoutPurchase(Fixture fixture, DateOnly date, decimal quantity = 20m)
        => PurchaseConsumption.RegisterWithoutPurchase(
            fixture.Workspace.Id, fixture.Season.Id, fixture.Plot.Id, date, "Abono de la nave",
            quantity, _userId);

    [Fact]
    public async Task ListAsync_Deberia_ResolverTerrenoYTemporada_Y_DistinguirElConsumoSinCompra()
    {
        // CA-2/CA-4 — el consumo sin compra se lee igual que una imputación, con `has_purchase: false`
        var fixture = await SeedAsync("-a");
        Db.PurchaseConsumptions.Add(Imputed(fixture, new DateOnly(2026, 10, 12)));
        Db.PurchaseConsumptions.Add(WithoutPurchase(fixture, new DateOnly(2026, 10, 13)));
        await Db.SaveChangesAsync();

        var repository = new ConsumptionRepository(Db);

        var views = await repository.ListAsync(fixture.Workspace.Id, new ConsumptionFilter());

        views.Should().HaveCount(2);
        views.Should().OnlyContain(v => v.PlotName == "Olivar Alto -a" && v.SeasonName == "2026/2027 -a");
        var sinCompra = views.Single(v => !v.HasPurchase);
        sinCompra.ProportionalCost.Should().Be(0m);
        sinCompra.Product.Should().Be("Abono de la nave");
        views.Single(v => v.HasPurchase).ProportionalCost.Should().Be(50m);
    }

    [Fact]
    public async Task ListAsync_Deberia_OrdenarPorFechaDeNegocio_NoDeCaptura()
    {
        // CA-4 — un consumo capturado después pero de fecha anterior queda debajo
        var fixture = await SeedAsync("-b");
        var reciente = WithoutPurchase(fixture, new DateOnly(2026, 10, 20));
        Db.PurchaseConsumptions.Add(reciente);
        await Db.SaveChangesAsync();
        var antiguo = WithoutPurchase(fixture, new DateOnly(2026, 10, 1));
        Db.PurchaseConsumptions.Add(antiguo);
        await Db.SaveChangesAsync();

        var repository = new ConsumptionRepository(Db);

        (await repository.ListAsync(fixture.Workspace.Id, new ConsumptionFilter()))
            .Select(v => v.Id).Should().ContainInOrder(reciente.Id, antiguo.Id);
    }

    [Fact]
    public async Task ListAsync_Deberia_AislarPorWorkspace_Y_ExcluirLosEliminados()
    {
        var mine = await SeedAsync("-c");
        var other = await SeedAsync("-d");
        var vivo = WithoutPurchase(mine, new DateOnly(2026, 10, 12));
        var borrado = WithoutPurchase(mine, new DateOnly(2026, 10, 13));
        borrado.Delete(_userId);
        Db.PurchaseConsumptions.AddRange(vivo, borrado, WithoutPurchase(other, new DateOnly(2026, 10, 14)));
        await Db.SaveChangesAsync();

        var repository = new ConsumptionRepository(Db);

        (await repository.ListAsync(mine.Workspace.Id, new ConsumptionFilter()))
            .Should().ContainSingle().Which.Id.Should().Be(vivo.Id);
        (await repository.FindByIdAsync(mine.Workspace.Id, borrado.Id)).Should().BeNull();
        (await Db.PurchaseConsumptions.CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task ListAsync_Deberia_FiltrarPorFechas_Terreno_Temporada_Y_Compra()
    {
        var fixture = await SeedAsync("-e");
        var otroTerreno = Plot.Create(fixture.Workspace.Id, "Olivar Bajo -e", "cedida");
        Db.Plots.Add(otroTerreno);
        await Db.SaveChangesAsync();

        Db.PurchaseConsumptions.Add(Imputed(fixture, new DateOnly(2026, 10, 12)));
        Db.PurchaseConsumptions.Add(WithoutPurchase(fixture, new DateOnly(2026, 12, 1)));
        Db.PurchaseConsumptions.Add(PurchaseConsumption.RegisterWithoutPurchase(
            fixture.Workspace.Id, fixture.Season.Id, otroTerreno.Id, new DateOnly(2026, 10, 13),
            "Otro material", 5m, _userId));
        await Db.SaveChangesAsync();

        var repository = new ConsumptionRepository(Db);
        var workspaceId = fixture.Workspace.Id;

        (await repository.ListAsync(workspaceId, new ConsumptionFilter(
                From: new DateOnly(2026, 10, 1), To: new DateOnly(2026, 10, 31))))
            .Should().HaveCount(2);
        (await repository.ListAsync(workspaceId, new ConsumptionFilter(PlotId: otroTerreno.Id)))
            .Should().ContainSingle();
        (await repository.ListAsync(workspaceId, new ConsumptionFilter(SeasonId: fixture.Season.Id)))
            .Should().HaveCount(3);
        (await repository.ListAsync(workspaceId, new ConsumptionFilter(PurchaseId: fixture.Purchase.Id)))
            .Should().ContainSingle();
    }

    [Fact]
    public async Task SumImputedQuantityAsync_Deberia_SumarSoloLoVivo_Y_PoderExcluirUnaFila()
    {
        // Es la consulta que sostiene la guarda de sobre-imputación (CA-1)
        var fixture = await SeedAsync("-f");
        var primera = Imputed(fixture, new DateOnly(2026, 10, 12), 100m);
        var segunda = Imputed(fixture, new DateOnly(2026, 10, 13), 150m);
        var retirada = Imputed(fixture, new DateOnly(2026, 10, 14), 200m);
        retirada.Delete(_userId);
        // Un consumo sin compra no cuenta para el reparto de ninguna compra.
        Db.PurchaseConsumptions.AddRange(
            primera, segunda, retirada, WithoutPurchase(fixture, new DateOnly(2026, 10, 15), 999m));
        await Db.SaveChangesAsync();

        var repository = new ConsumptionRepository(Db);
        var workspaceId = fixture.Workspace.Id;

        (await repository.SumImputedQuantityAsync(workspaceId, fixture.Purchase.Id)).Should().Be(250m);
        (await repository.SumImputedQuantityAsync(workspaceId, fixture.Purchase.Id, segunda.Id))
            .Should().Be(100m);
    }

    [Fact]
    public async Task SumImputedQuantityAsync_Deberia_Devolver0_SinImputaciones()
    {
        // `SUM` sobre conjunto vacío es NULL en SQL: si no se colapsa, la guarda reventaría
        var fixture = await SeedAsync("-g");

        var repository = new ConsumptionRepository(Db);

        (await repository.SumImputedQuantityAsync(fixture.Workspace.Id, fixture.Purchase.Id))
            .Should().Be(0m);
    }

    [Fact]
    public async Task SumImputedQuantityByPurchaseAsync_Deberia_AgruparPorCompra()
    {
        var fixture = await SeedAsync("-h");
        var otraCompra = Purchase.Create(
            fixture.Workspace.Id, fixture.Season.Id, new DateOnly(2026, 10, 2), "Gasóleo", 100m, 145m, _userId);
        Db.Purchases.Add(otraCompra);
        await Db.SaveChangesAsync();

        Db.PurchaseConsumptions.Add(Imputed(fixture, new DateOnly(2026, 10, 12), 100m));
        Db.PurchaseConsumptions.Add(Imputed(fixture, new DateOnly(2026, 10, 13), 50m));
        Db.PurchaseConsumptions.Add(PurchaseConsumption.ImputeFromPurchase(
            fixture.Workspace.Id, otraCompra.Id, fixture.Season.Id, otraCompra.Product,
            otraCompra.UnitPrice, fixture.Plot.Id, new DateOnly(2026, 10, 14), 30m, _userId));
        await Db.SaveChangesAsync();

        var repository = new ConsumptionRepository(Db);

        var totals = await repository.SumImputedQuantityByPurchaseAsync(
            fixture.Workspace.Id, new[] { fixture.Purchase.Id, otraCompra.Id });

        totals[fixture.Purchase.Id].Should().Be(150m);
        totals[otraCompra.Id].Should().Be(30m);
    }

    [Fact]
    public async Task CountLiveByPurchaseAsync_Deberia_IgnorarLasRetiradas()
    {
        // Es la guarda que impide dar de baja una compra con imputaciones vivas
        var fixture = await SeedAsync("-i");
        var viva = Imputed(fixture, new DateOnly(2026, 10, 12));
        var retirada = Imputed(fixture, new DateOnly(2026, 10, 13));
        retirada.Delete(_userId);
        Db.PurchaseConsumptions.AddRange(viva, retirada);
        await Db.SaveChangesAsync();

        var repository = new ConsumptionRepository(Db);

        (await repository.CountLiveByPurchaseAsync(fixture.Workspace.Id, fixture.Purchase.Id))
            .Should().Be(1);
    }

    [Fact]
    public async Task ElConsumoSinCompra_NoDeberia_GanarCoste_SiLuegoApareceUnaCompra()
    {
        // CA-3 — registrar una compra posterior no recalcula lo ya guardado (RN-032)
        var fixture = await SeedAsync("-j");
        var consumo = WithoutPurchase(fixture, new DateOnly(2026, 10, 12));
        Db.PurchaseConsumptions.Add(consumo);
        await Db.SaveChangesAsync();

        // Aparece después una compra del mismo material.
        Db.Purchases.Add(Purchase.Create(
            fixture.Workspace.Id, fixture.Season.Id, new DateOnly(2026, 10, 20),
            "Abono de la nave", 100m, 90m, _userId));
        await Db.SaveChangesAsync();

        var repository = new ConsumptionRepository(Db);

        var view = (await repository.GetViewAsync(fixture.Workspace.Id, consumo.Id))!;
        view.HasPurchase.Should().BeFalse();
        view.ProportionalCost.Should().Be(0m);
        view.UnitPrice.Should().Be(0m);
    }

}
