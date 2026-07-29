using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Operations;
using Terrenario.Api.Domain.Purchases;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;

namespace Terrenario.Api.Tests.Purchases;

/// <summary>
/// Tests del repositorio de compras contra SQLite real (MVP-303): traducción a SQL de la proyección
/// con <c>JOIN</c> a temporadas, del filtro de baja lógica, de los filtros del listado, del orden y
/// —sobre todo— de la agrupación de las **sugerencias de producto** (RN-031), que es la consulta más
/// fácil de romper y la que ningún mock ve (lección de P-014).
/// </summary>
public sealed class PurchaseRepositorySqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TerrenarioDbContext _db;
    private readonly Guid _userId = Guid.NewGuid();

    public PurchaseRepositorySqliteTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TerrenarioDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TerrenarioDbContext(options);
        _db.Database.EnsureCreated();
    }

    private sealed record Fixture(Workspace Workspace, Season Season);

    private async Task<Fixture> SeedAsync(string suffix)
    {
        var user = User.Create($"google-sub{suffix}", "Andrés", $"andres{suffix}@ejemplo.com");
        _db.Users.Add(user);
        var workspace = Workspace.Create(user.Id, $"Finca El Olivar {suffix}");
        _db.Workspaces.Add(workspace);
        var season = Season.Create(
            workspace.Id, $"2026/2027 {suffix}", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28));
        _db.Seasons.Add(season);
        await _db.SaveChangesAsync();

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
        _db.Purchases.Add(NewPurchase(mine, new DateOnly(2026, 10, 5)));
        _db.Purchases.Add(NewPurchase(other, new DateOnly(2026, 10, 6)));
        await _db.SaveChangesAsync();

        var repository = new PurchaseRepository(_db);

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
        _db.Purchases.AddRange(viva, borrada);
        await _db.SaveChangesAsync();

        var repository = new PurchaseRepository(_db);

        (await repository.ListAsync(fixture.Workspace.Id, new PurchaseFilter()))
            .Should().ContainSingle().Which.Id.Should().Be(viva.Id);
        (await repository.FindByIdAsync(fixture.Workspace.Id, borrada.Id)).Should().BeNull();
        (await _db.Purchases.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task ListAsync_Deberia_OrdenarPorFechaDeCompraDescendente()
    {
        var fixture = await SeedAsync("-d");
        var antigua = NewPurchase(fixture, new DateOnly(2026, 10, 1));
        var reciente = NewPurchase(fixture, new DateOnly(2026, 10, 20));
        _db.Purchases.Add(reciente);
        await _db.SaveChangesAsync();
        _db.Purchases.Add(antigua);
        await _db.SaveChangesAsync();

        var repository = new PurchaseRepository(_db);

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
        _db.Seasons.Add(otraTemporada);
        await _db.SaveChangesAsync();

        _db.Purchases.Add(NewPurchase(fixture, new DateOnly(2026, 10, 5), "Abono NPK"));
        _db.Purchases.Add(NewPurchase(fixture, new DateOnly(2026, 11, 5), "Gasóleo B"));
        _db.Purchases.Add(Purchase.Create(
            fixture.Workspace.Id, otraTemporada.Id, new DateOnly(2025, 10, 5), "Abono NPK", 100m, 50m, _userId));
        await _db.SaveChangesAsync();

        var repository = new PurchaseRepository(_db);
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
        _db.Purchases.AddRange(dentro, fuera);
        await _db.SaveChangesAsync();

        var repository = new PurchaseRepository(_db);

        (await repository.GetViewAsync(fixture.Workspace.Id, dentro.Id))!.IsOutOfSeasonRange.Should().BeFalse();
        (await repository.GetViewAsync(fixture.Workspace.Id, fuera.Id))!.IsOutOfSeasonRange.Should().BeTrue();
    }

    [Fact]
    public async Task ListProductSuggestionsAsync_Deberia_AgruparPorProducto_Y_OrdenarPorFrecuencia()
    {
        // RN-031 (HU-2) — vocabulario aprendido del histórico, no un catálogo
        var fixture = await SeedAsync("-g");
        _db.Purchases.Add(NewPurchase(fixture, new DateOnly(2026, 10, 1), "Abono NPK"));
        _db.Purchases.Add(NewPurchase(fixture, new DateOnly(2026, 10, 2), "Abono NPK"));
        _db.Purchases.Add(NewPurchase(fixture, new DateOnly(2026, 10, 3), "Gasóleo B"));
        await _db.SaveChangesAsync();

        var repository = new PurchaseRepository(_db);

        var suggestions = await repository.ListProductSuggestionsAsync(fixture.Workspace.Id, null, 20);

        suggestions.Select(s => s.Product).Should().ContainInOrder("Abono NPK", "Gasóleo B");
        suggestions[0].TimesUsed.Should().Be(2);
        suggestions[1].TimesUsed.Should().Be(1);
    }

    [Fact]
    public async Task ListProductSuggestionsAsync_Deberia_BuscarParcialmente_Aislar_Y_NoVerLasEliminadas()
    {
        var mine = await SeedAsync("-h");
        var other = await SeedAsync("-i");
        _db.Purchases.Add(NewPurchase(mine, new DateOnly(2026, 10, 1), "Abono NPK"));
        var borrada = NewPurchase(mine, new DateOnly(2026, 10, 2), "Producto retirado");
        borrada.Delete(_userId);
        _db.Purchases.Add(borrada);
        _db.Purchases.Add(NewPurchase(other, new DateOnly(2026, 10, 3), "Abono ajeno"));
        await _db.SaveChangesAsync();

        var repository = new PurchaseRepository(_db);

        (await repository.ListProductSuggestionsAsync(mine.Workspace.Id, "npk", 20))
            .Should().ContainSingle().Which.Product.Should().Be("Abono NPK");
        // Lo eliminado deja de sugerirse: si se retiró, no conviene volver a proponerlo (RN-037).
        (await repository.ListProductSuggestionsAsync(mine.Workspace.Id, "retirado", 20))
            .Should().BeEmpty();
        // El histórico de otro Workspace no se filtra dentro (aislamiento multi-tenant).
        (await repository.ListProductSuggestionsAsync(mine.Workspace.Id, "ajeno", 20))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_Deberia_Traducir_LaColisionDeVersion_A_Conflicto()
    {
        // ADR-0005 — dos escrituras simultáneas no pueden acabar en un 500
        var fixture = await SeedAsync("-j");
        var purchase = NewPurchase(fixture, new DateOnly(2026, 10, 5));
        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync();

        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE purchases SET version = version + 1 WHERE id = {0}", purchase.Id);

        purchase.Update(fixture.Season.Id, new DateOnly(2026, 10, 5), "Abono NPK", 600m, 300m, _userId);

        var repository = new PurchaseRepository(_db);

        await repository.Invoking(r => r.SaveChangesAsync())
            .Should().ThrowAsync<ConcurrencyConflictException>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
