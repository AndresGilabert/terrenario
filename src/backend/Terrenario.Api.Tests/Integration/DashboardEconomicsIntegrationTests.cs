using FluentAssertions;
using System.Net.Http.Json;
using System.Text.Json;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-707 (CA-4, CA-5) — La lectura económica de la campaña contra la API real.
///
/// El contraste que importa es el mismo que destapó `P-082` y `R-01`: **las cifras del panel y las de
/// la cabecera del diario tienen que coincidir**. Aquí se comprueba sumando de verdad, no leyendo el
/// código, porque el gasto tiene una regla que ya se equivocó una vez —las imputaciones reparten
/// dinero que la compra ya aportó, así que sumarlas contaría el mismo gasto dos veces—.
/// </summary>
public sealed class DashboardEconomicsIntegrationTests : IAsyncLifetime
{
    private readonly TerrenarioApiFactory _factory = new();

    private ApiSession _session = null!;
    private Guid _seasonId;
    private Guid _plotId;
    private Guid _otherPlotId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _factory.Google.WithIdentity("codigo", "sub", "Andrés", "andres@ejemplo.test");
        _session = await ApiSession.LoginAsync(_factory, "codigo");
        await _session.CreateWorkspaceAsync("Finca El Olivar");

        _seasonId = (await _session.PostJsonAsync("/api/v1/seasons", new
        {
            name = "Campaña 2025/26",
            start_date = "2025-10-01",
            end_date = "2026-03-31"
        })).GetProperty("id").GetGuid();

        _plotId = (await _session.PostJsonAsync("/api/v1/plots", new { name = "La Hoya", ownership_type = "propia" }))
            .GetProperty("id").GetGuid();
        _otherPlotId = (await _session.PostJsonAsync("/api/v1/plots", new { name = "El Cerro", ownership_type = "propia" }))
            .GetProperty("id").GetGuid();

        // Gasto: una compra de 400 € (del Workspace, no de un terreno).
        await _session.PostJsonAsync("/api/v1/purchases", new
        {
            purchase_date = "2025-11-02",
            product = "Abono foliar",
            season_id = _seasonId,
            total_quantity = 200m,
            total_cost = 400m
        });
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private Task CreateHarvestAsync(Guid plotId, decimal kgs, decimal? unitPrice, string date = "2025-12-05")
        => _session.PostJsonAsync("/api/v1/harvests", new
        {
            date,
            plot_id = plotId,
            season_id = _seasonId,
            product = "aceituna_olivar",
            kgs,
            destination = "venta_aceituna",
            unit_price = unitPrice
        });

    [Fact]
    public async Task Deberia_DecirSinDato_Cuando_NingunaPartidaTienePrecio()
    {
        // CA-5 — «0 €» afirmaría que la campaña no ha ingresado nada; lo cierto es que no se sabe.
        await CreateHarvestAsync(_plotId, 1_000m, unitPrice: null);

        var economics = await _session.GetJsonAsync("/api/v1/dashboard/economics");

        economics.GetProperty("income").ValueKind.Should().Be(JsonValueKind.Null);
        economics.GetProperty("harvests").GetInt32().Should().Be(1);
        economics.GetProperty("harvests_with_price").GetInt32().Should().Be(0);
        // El gasto sí es cero-conocido: la compra existe y vale 400 €.
        economics.GetProperty("expense").GetDecimal().Should().Be(400m);
    }

    [Fact]
    public async Task Deberia_SumarSoloLasPartidasConPrecio()
    {
        await CreateHarvestAsync(_plotId, 1_000m, unitPrice: 0.60m);   // 600 €
        await CreateHarvestAsync(_plotId, 500m, unitPrice: 0.40m);     // 200 €
        await CreateHarvestAsync(_plotId, 2_000m, unitPrice: null);    // sin dato

        var economics = await _session.GetJsonAsync("/api/v1/dashboard/economics");

        economics.GetProperty("income").GetDecimal().Should().Be(800m);
        economics.GetProperty("harvests").GetInt32().Should().Be(3);
        // Sobre cuántas se suma: sin esto, 800 € parecería el ingreso de las tres partidas.
        economics.GetProperty("harvests_with_price").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Deberia_CoincidirConLaCabeceraDelDiario()
    {
        // CA-4 — el contraste numérico entre las dos pantallas, hecho de verdad.
        await CreateHarvestAsync(_plotId, 1_000m, unitPrice: 0.60m);
        await CreateHarvestAsync(_otherPlotId, 500m, unitPrice: 0.40m);

        var economics = await _session.GetJsonAsync("/api/v1/dashboard/economics");
        var diary = await _session.GetJsonAsync("/api/v1/diary");
        var diaryMeta = diary.GetProperty("meta");

        economics.GetProperty("income").GetDecimal()
            .Should().Be(diaryMeta.GetProperty("total_income").GetDecimal());
        economics.GetProperty("expense").GetDecimal()
            .Should().Be(diaryMeta.GetProperty("total_cost").GetDecimal());
    }

    [Fact]
    public async Task Deberia_DejarFueraLaCompra_Cuando_SeAcotaPorTerrenos()
    {
        // Una compra es del Workspace, no de un terreno: acotar por terrenos la deja fuera **por
        // definición**, igual que en el diario. Si el panel la incluyera, las dos pantallas volverían
        // a discrepar en cuanto alguien tocara el filtro de terrenos.
        await CreateHarvestAsync(_plotId, 1_000m, unitPrice: 0.60m);

        var acotado = await _session.GetJsonAsync($"/api/v1/dashboard/economics?plot_ids={_plotId}");
        var sinFiltro = await _session.GetJsonAsync("/api/v1/dashboard/economics");

        acotado.GetProperty("expense").GetDecimal().Should().Be(0m);
        sinFiltro.GetProperty("expense").GetDecimal().Should().Be(400m);
        // El ingreso sí es del terreno: la cosecha sí tiene terreno.
        acotado.GetProperty("income").GetDecimal().Should().Be(600m);
    }

    [Fact]
    public async Task Deberia_RecalcularElImporte_Cuando_SeCorrigenLosKilos()
    {
        // CA-3 — el importe es derivado: corregir los kilos lo mueve, sin tocar el precio.
        var harvest = await _session.PostJsonAsync("/api/v1/harvests", new
        {
            date = "2025-12-05",
            plot_id = _plotId,
            season_id = _seasonId,
            product = "aceituna_olivar",
            kgs = 1_000m,
            destination = "venta_aceituna",
            unit_price = 0.50m
        });
        harvest.GetProperty("amount").GetDecimal().Should().Be(500m);

        var response = await _session.PatchAsync(
            $"/api/v1/harvests/{harvest.GetProperty("id").GetGuid()}",
            new { kgs = 1_800m },
            ifMatch: harvest.GetProperty("version").GetInt32());

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<JsonElement>();

        updated.GetProperty("unit_price").GetDecimal().Should().Be(0.50m);
        updated.GetProperty("amount").GetDecimal().Should().Be(900m);
    }
}
