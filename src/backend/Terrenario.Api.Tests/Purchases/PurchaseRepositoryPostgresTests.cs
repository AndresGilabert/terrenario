using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Operations;
using Terrenario.Api.Domain.Purchases;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Tests.Integration;

namespace Terrenario.Api.Tests.Purchases;

/// <summary>
/// Tests del repositorio de compras contra PostgreSQL real (MVP-303): traducción a SQL de la proyección
/// con <c>JOIN</c> a temporadas, del filtro de baja lógica, de los filtros del listado, del orden y
/// —sobre todo— de la agrupación de las **sugerencias de producto** (RN-031), que es la consulta más
/// fácil de romper y la que ningún mock ve (lección de P-014).
/// </summary>
public sealed class PurchaseRepositoryPostgresTests : RepositoryTestBase
{
    private readonly Guid _userId = Guid.NewGuid();

    private sealed record Fixture(Workspace Workspace, Season Season);

    private async Task<Fixture> SeedAsync(string suffix)
    {
        var user = User.Create($"google-sub{suffix}", "Andrés", $"andres{suffix}@ejemplo.com");
        Db.Users.Add(user);
        var workspace = Workspace.Create(user.Id, $"Finca El Olivar {suffix}");
        Db.Workspaces.Add(workspace);
        var season = Season.Create(
            workspace.Id, $"2026/2027 {suffix}", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28));
        Db.Seasons.Add(season);
        await Db.SaveChangesAsync();

        return new Fixture(workspace, season);
    }

    private Purchase NewPurchase(
        Fixture fixture,
        DateOnly date,
        string product = "Abono NPK",
        decimal quantity = 500m,
        decimal cost = 250m)
        => Purchase.Create(fixture.Workspace.Id, fixture.Season.Id, date, product, quantity, cost, _userId);

    [Fact]
    public async Task ListAsync_Deberia_ResolverLaTemporada_Y_AislarPorWorkspace()
    {
        var mine = await SeedAsync("-a");
        var other = await SeedAsync("-b");
        Db.Purchases.Add(NewPurchase(mine, new DateOnly(2026, 10, 5)));
        Db.Purchases.Add(NewPurchase(other, new DateOnly(2026, 10, 6)));
        await Db.SaveChangesAsync();

        var repository = new PurchaseRepository(Db);

        var view = (await repository.ListAsync(mine.Workspace.Id, new PurchaseFilter()))
            .Should().ContainSingle().Which;
        view.SeasonName.Should().Be("2026/2027 -a");
        view.UnitPrice.Should().Be(0.5m);
    }

    [Fact]
    public async Task ListAsync_Deberia_ExcluirLasEliminadasLogicamente()
    {
        // RN-037 — desaparece del libro, pero la fila sigue en base de datos
        var fixture = await SeedAsync("-c");
        var viva = NewPurchase(fixture, new DateOnly(2026, 10, 5));
        var borrada = NewPurchase(fixture, new DateOnly(2026, 10, 6));
        borrada.Delete(_userId);
        Db.Purchases.AddRange(viva, borrada);
        await Db.SaveChangesAsync();

        var repository = new PurchaseRepository(Db);

        (await repository.ListAsync(fixture.Workspace.Id, new PurchaseFilter()))
            .Should().ContainSingle().Which.Id.Should().Be(viva.Id);
        (await repository.FindByIdAsync(fixture.Workspace.Id, borrada.Id)).Should().BeNull();
        (await Db.Purchases.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task ListAsync_Deberia_OrdenarPorFechaDeCompraDescendente()
    {
        var fixture = await SeedAsync("-d");
        var antigua = NewPurchase(fixture, new DateOnly(2026, 10, 1));
        var reciente = NewPurchase(fixture, new DateOnly(2026, 10, 20));
        Db.Purchases.Add(reciente);
        await Db.SaveChangesAsync();
        Db.Purchases.Add(antigua);
        await Db.SaveChangesAsync();

        var repository = new PurchaseRepository(Db);

        (await repository.ListAsync(fixture.Workspace.Id, new PurchaseFilter()))
            .Select(v => v.Id).Should().ContainInOrder(reciente.Id, antigua.Id);
    }

    [Fact]
    public async Task ListAsync_Deberia_FiltrarPorProductoParcial_Temporada_Y_Fechas()
    {
        var fixture = await SeedAsync("-e");
        // Una temporada nace activa y RN-022 solo admite una activa por Workspace (indice unico
        // parcial), asi que la anterior se cierra para poder convivir con la vigente.
        var otraTemporada = Season.Create(
            fixture.Workspace.Id, "2025/2026 -e", new DateOnly(2025, 9, 1), new DateOnly(2026, 2, 28));
        otraTemporada.Close();
        Db.Seasons.Add(otraTemporada);
        await Db.SaveChangesAsync();

        Db.Purchases.Add(NewPurchase(fixture, new DateOnly(2026, 10, 5), "Abono NPK"));
        Db.Purchases.Add(NewPurchase(fixture, new DateOnly(2026, 11, 5), "Gasóleo B"));
        Db.Purchases.Add(Purchase.Create(
            fixture.Workspace.Id, otraTemporada.Id, new DateOnly(2025, 10, 5), "Abono NPK", 100m, 50m, _userId));
        await Db.SaveChangesAsync();

        var repository = new PurchaseRepository(Db);
        var workspaceId = fixture.Workspace.Id;

        // Búsqueda parcial e insensible a mayúsculas: el producto es texto libre (RN-031)
        (await repository.ListAsync(workspaceId, new PurchaseFilter(Product: "abono")))
            .Should().HaveCount(2);
        (await repository.ListAsync(workspaceId, new PurchaseFilter(SeasonId: otraTemporada.Id)))
            .Should().ContainSingle();
        (await repository.ListAsync(workspaceId, new PurchaseFilter(
                From: new DateOnly(2026, 1, 1), To: new DateOnly(2026, 12, 31))))
            .Should().HaveCount(2);
    }

    [Fact]
    public async Task GetViewAsync_Deberia_SenalarLaFechaFueraDelRangoDeLaTemporada()
    {
        // RN-023 — mismo aviso no bloqueante que en la actividad
        var fixture = await SeedAsync("-f");
        var dentro = NewPurchase(fixture, new DateOnly(2026, 10, 5));
        var fuera = NewPurchase(fixture, new DateOnly(2026, 8, 15));
        Db.Purchases.AddRange(dentro, fuera);
        await Db.SaveChangesAsync();

        var repository = new PurchaseRepository(Db);

        (await repository.GetViewAsync(fixture.Workspace.Id, dentro.Id))!.IsOutOfSeasonRange.Should().BeFalse();
        (await repository.GetViewAsync(fixture.Workspace.Id, fuera.Id))!.IsOutOfSeasonRange.Should().BeTrue();
    }

    // MVP-708 (`P-057`) — Las sugerencias de material dejaron de colgar de este repositorio: se
    // aprenden de compras **y** de consumos sin compra previa. Sus tests viven ahora en
    // `Materials/MaterialRepositoryPostgresTests`.

    [Fact]
    public async Task SaveChangesAsync_Deberia_Traducir_LaColisionDeVersion_A_Conflicto()
    {
        // ADR-0005 — dos escrituras simultáneas no pueden acabar en un 500
        var fixture = await SeedAsync("-j");
        var purchase = NewPurchase(fixture, new DateOnly(2026, 10, 5));
        Db.Purchases.Add(purchase);
        await Db.SaveChangesAsync();

        await Db.Database.ExecuteSqlRawAsync(
            "UPDATE purchases SET version = version + 1 WHERE id = {0}", purchase.Id);

        purchase.Update(fixture.Season.Id, new DateOnly(2026, 10, 5), "Abono NPK", 600m, 300m, _userId);

        var repository = new PurchaseRepository(Db);

        await repository.Invoking(r => r.SaveChangesAsync())
            .Should().ThrowAsync<ConcurrencyConflictException>();
    }

}
