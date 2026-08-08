using FluentAssertions;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Purchases;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Tests.Integration;

namespace Terrenario.Api.Tests.Materials;

/// <summary>
/// MVP-708 (<c>P-057</c>) — Tests del vocabulario de materiales contra PostgreSQL real (RN-031).
///
/// Vienen de <c>PurchaseRepositoryPostgresTests</c>, donde el vocabulario solo miraba compras. Lo que
/// se comprueba aquí es exactamente lo que ningún mock ve: que la <c>UNION ALL</c> entre los dos
/// libros se traduce a SQL, que agrupa y ordena sobre el conjunto unido y que sigue respetando el
/// aislamiento por Workspace y la baja lógica.
/// </summary>
public sealed class MaterialRepositoryPostgresTests : RepositoryTestBase
{
    private readonly Guid _userId = Guid.NewGuid();

    private sealed record Fixture(Workspace Workspace, Season Season, Plot Plot);

    private async Task<Fixture> SeedAsync(string suffix)
    {
        var user = User.Create($"google-sub{suffix}", "Andrés", $"andres{suffix}@ejemplo.com");
        Db.Users.Add(user);
        var workspace = Workspace.Create(user.Id, $"Finca El Olivar {suffix}");
        Db.Workspaces.Add(workspace);
        var season = Season.Create(
            workspace.Id, $"2026/2027 {suffix}", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28));
        Db.Seasons.Add(season);
        var plot = Plot.Create(workspace.Id, $"Olivar Alto {suffix}", "propia");
        Db.Plots.Add(plot);
        await Db.SaveChangesAsync();

        return new Fixture(workspace, season, plot);
    }

    private Purchase NewPurchase(Fixture fixture, DateOnly date, string product)
        => Purchase.Create(fixture.Workspace.Id, fixture.Season.Id, date, product, 500m, 250m, _userId);

    private PurchaseConsumption NewConsumptionWithoutPurchase(
        Fixture fixture,
        DateOnly date,
        string product)
        => PurchaseConsumption.RegisterWithoutPurchase(
            fixture.Workspace.Id, fixture.Season.Id, fixture.Plot.Id, date, product, 20m, _userId);

    [Fact]
    public async Task ListSuggestionsAsync_Deberia_CombinarComprasYConsumos_Y_OrdenarPorFrecuencia()
    {
        // `P-057` — el consumo sin compra previa aporta vocabulario propio: es el único sitio donde se
        // escribe un material que nunca se compró.
        var fixture = await SeedAsync("-a");
        Db.Purchases.Add(NewPurchase(fixture, new DateOnly(2026, 10, 1), "Abono NPK"));
        Db.Purchases.Add(NewPurchase(fixture, new DateOnly(2026, 10, 2), "Abono NPK"));
        Db.PurchaseConsumptions.Add(
            NewConsumptionWithoutPurchase(fixture, new DateOnly(2026, 10, 3), "Abono NPK"));
        Db.PurchaseConsumptions.Add(
            NewConsumptionWithoutPurchase(fixture, new DateOnly(2026, 10, 4), "Cobre de la nave"));
        await Db.SaveChangesAsync();

        var repository = new MaterialRepository(Db);

        var suggestions = await repository.ListSuggestionsAsync(fixture.Workspace.Id, null, 20);

        suggestions.Select(s => s.Product).Should().ContainInOrder("Abono NPK", "Cobre de la nave");
        // Dos compras y un consumo del mismo nombre: el recuento se hace sobre el conjunto unido.
        suggestions[0].TimesUsed.Should().Be(3);
        suggestions[1].TimesUsed.Should().Be(1);
    }

    [Fact]
    public async Task ListSuggestionsAsync_NoDeberia_ContarLasImputaciones()
    {
        // Una imputación copia el material de su compra, así que no aporta vocabulario: contarla
        // ordenaría el vocabulario por «cuánto se repartió» en vez de por «cuánto se escribió».
        var fixture = await SeedAsync("-b");
        var purchase = NewPurchase(fixture, new DateOnly(2026, 10, 1), "Abono NPK");
        var otra = NewPurchase(fixture, new DateOnly(2026, 10, 2), "Gasóleo B");
        Db.Purchases.AddRange(purchase, otra);
        await Db.SaveChangesAsync();

        for (var day = 10; day < 14; day++)
            Db.PurchaseConsumptions.Add(PurchaseConsumption.ImputeFromPurchase(
                fixture.Workspace.Id, purchase.Id, fixture.Season.Id, purchase.Product,
                purchase.UnitPrice, fixture.Plot.Id, new DateOnly(2026, 10, day), 10m, _userId));
        await Db.SaveChangesAsync();

        var repository = new MaterialRepository(Db);

        var suggestions = await repository.ListSuggestionsAsync(fixture.Workspace.Id, null, 20);

        suggestions.Should().HaveCount(2);
        suggestions.Single(s => s.Product == "Abono NPK").TimesUsed.Should().Be(1);
        suggestions.Single(s => s.Product == "Gasóleo B").TimesUsed.Should().Be(1);
    }

    [Fact]
    public async Task ListSuggestionsAsync_Deberia_BuscarParcialmente_Aislar_Y_NoVerLoEliminado()
    {
        var mine = await SeedAsync("-c");
        var other = await SeedAsync("-d");
        Db.Purchases.Add(NewPurchase(mine, new DateOnly(2026, 10, 1), "Abono NPK"));

        var compraRetirada = NewPurchase(mine, new DateOnly(2026, 10, 2), "Producto retirado");
        compraRetirada.Delete(_userId);
        Db.Purchases.Add(compraRetirada);

        var consumoRetirado = NewConsumptionWithoutPurchase(
            mine, new DateOnly(2026, 10, 3), "Consumo retirado");
        consumoRetirado.Delete(_userId);
        Db.PurchaseConsumptions.Add(consumoRetirado);

        Db.Purchases.Add(NewPurchase(other, new DateOnly(2026, 10, 4), "Abono ajeno"));
        Db.PurchaseConsumptions.Add(
            NewConsumptionWithoutPurchase(other, new DateOnly(2026, 10, 5), "Consumo ajeno"));
        await Db.SaveChangesAsync();

        var repository = new MaterialRepository(Db);
        var workspaceId = mine.Workspace.Id;

        (await repository.ListSuggestionsAsync(workspaceId, "npk", 20))
            .Should().ContainSingle().Which.Product.Should().Be("Abono NPK");
        // Lo eliminado deja de sugerirse en los dos libros: si se retiró, no conviene proponerlo (RN-037).
        (await repository.ListSuggestionsAsync(workspaceId, "retirado", 20)).Should().BeEmpty();
        // El histórico de otro Workspace no se filtra dentro (aislamiento multi-tenant).
        (await repository.ListSuggestionsAsync(workspaceId, "ajeno", 20)).Should().BeEmpty();
    }

    [Fact]
    public async Task ListSuggestionsAsync_Deberia_RecortarSobreElConjuntoUnido()
    {
        // El motivo de unir en SQL en vez de juntar dos listas ya recortadas: con un tope por lista,
        // un material presente en los dos libros podría quedarse fuera aunque sumando fuese el más
        // usado. Aquí el tope es 1 y el ganador solo lo es al sumar.
        var fixture = await SeedAsync("-e");
        Db.Purchases.Add(NewPurchase(fixture, new DateOnly(2026, 10, 1), "Abono NPK"));
        Db.Purchases.Add(NewPurchase(fixture, new DateOnly(2026, 10, 2), "Gasóleo B"));
        Db.Purchases.Add(NewPurchase(fixture, new DateOnly(2026, 10, 3), "Gasóleo B"));
        Db.PurchaseConsumptions.Add(
            NewConsumptionWithoutPurchase(fixture, new DateOnly(2026, 10, 4), "Abono NPK"));
        Db.PurchaseConsumptions.Add(
            NewConsumptionWithoutPurchase(fixture, new DateOnly(2026, 10, 5), "Abono NPK"));
        await Db.SaveChangesAsync();

        var repository = new MaterialRepository(Db);

        var suggestions = await repository.ListSuggestionsAsync(fixture.Workspace.Id, null, 1);

        suggestions.Should().ContainSingle().Which.Product.Should().Be("Abono NPK");
        suggestions[0].TimesUsed.Should().Be(3);
    }
}
