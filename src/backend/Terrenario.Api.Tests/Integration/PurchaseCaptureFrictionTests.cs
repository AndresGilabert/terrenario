using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-708 — Los dos roces de captura del libro de compras, contra la API real.
///
/// Se comprueban aquí y no con mocks porque los dos son de <b>lectura sobre dos entidades a la vez</b>:
/// el vocabulario de materiales une compras y consumos (<c>P-057</c>) y el aviso de fecha anterior
/// necesita la compra que paga el consumo (<c>P-058</c>). Un doble de repositorio los daría por buenos
/// sin ejecutar el SQL que de verdad puede fallar.
///
/// El escenario de <c>P-058</c> es literalmente el que lo destapó: imputar el <c>2020-01-01</c> una
/// compra del <c>2026-07-31</c>. Lo que este test fija es que <b>sigue respondiendo 201</b> —la
/// captura retroactiva es legítima (RN-032) y RN-043 avisa sin bloquear— y que la respuesta lleva la
/// señal.
/// </summary>
public sealed class PurchaseCaptureFrictionTests : IAsyncLifetime
{
    private readonly TerrenarioApiFactory _factory = new();

    private ApiSession _session = null!;
    private Guid _seasonId;
    private Guid _plotId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _factory.Google.WithIdentity("codigo", "sub", "Andrés", "andres@ejemplo.test");
        _session = await ApiSession.LoginAsync(_factory, "codigo");
        await _session.CreateWorkspaceAsync("Finca El Olivar");

        _seasonId = (await _session.PostJsonAsync("/api/v1/seasons", new
        {
            name = "Campaña 2026/27",
            start_date = "2026-07-01",
            end_date = "2027-03-31"
        })).GetProperty("id").GetGuid();

        _plotId = (await _session.PostJsonAsync("/api/v1/plots", new
        {
            name = "La Hoya",
            ownership_type = "propia"
        })).GetProperty("id").GetGuid();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// CA-1 (`P-057`) — El vocabulario que alimenta el campo de material sale de los <b>dos</b> libros.
    /// «Cobre de la nave» nunca se compró: solo existe porque alguien registró un consumo sin compra
    /// previa, y hasta esta historia no se le sugería a nadie.
    /// </summary>
    [Fact]
    public async Task ElVocabularioDeMateriales_Deberia_AprenderDeComprasYDeConsumos()
    {
        await _session.PostJsonAsync("/api/v1/purchases", new
        {
            purchase_date = "2026-07-31",
            product = "Abono NPK",
            season_id = _seasonId,
            total_quantity = 200m,
            total_cost = 400m
        });

        await _session.PostJsonAsync("/api/v1/consumptions", new
        {
            date = "2026-08-02",
            plot_id = _plotId,
            season_id = _seasonId,
            product = "Cobre de la nave",
            quantity = 5m
        });

        var vocabulary = await _session.GetJsonAsync("/api/v1/purchases/products");
        var products = vocabulary.GetProperty("data").EnumerateArray()
            .Select(item => item.GetProperty("product").GetString())
            .ToList();

        products.Should().BeEquivalentTo(["Abono NPK", "Cobre de la nave"]);

        // La búsqueda parcial también ve el material que solo existe en los consumos.
        var searched = await _session.GetJsonAsync("/api/v1/purchases/products?search=nave");
        searched.GetProperty("data").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("product").GetString().Should().Be("Cobre de la nave");
    }

    /// <summary>
    /// CA-2 (`P-058`) — Imputar con fecha anterior a la compra <b>sigue respondiendo 201</b>, y la
    /// respuesta trae la señal con la que la UI avisa sin impedir (RN-043).
    /// </summary>
    [Fact]
    public async Task ImputarConFechaAnteriorALaCompra_Deberia_Responder201_ConElAviso()
    {
        var purchaseId = (await _session.PostJsonAsync("/api/v1/purchases", new
        {
            purchase_date = "2026-07-31",
            product = "Abono NPK",
            season_id = _seasonId,
            total_quantity = 200m,
            total_cost = 400m
        })).GetProperty("id").GetGuid();

        var response = await _session.PostAsync($"/api/v1/purchases/{purchaseId}/consumptions", new
        {
            date = "2020-01-01",
            plot_id = _plotId,
            quantity = 50m
        });

        // Lo que el punto pedía conservar: no se bloquea.
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var consumption = await response.Content.ReadFromJsonAsync<JsonElement>();
        consumption.GetProperty("is_before_purchase_date").GetBoolean().Should().BeTrue();
        consumption.GetProperty("purchase_date").GetString().Should().Be("2026-07-31");
        // El coste se calcula igual: el aviso no cambia nada del registro.
        consumption.GetProperty("proportional_cost").GetDecimal().Should().Be(100m);

        // CA-3 — la fila del listado lo arrastra, que es de donde la etiqueta lo lee.
        var list = await _session.GetJsonAsync("/api/v1/consumptions?season_id=all");
        list.GetProperty("data").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("is_before_purchase_date").GetBoolean().Should().BeTrue();

        // …y el diario, que es la vista principal (RN-033), lo señala igual.
        var diary = await _session.GetJsonAsync("/api/v1/diary?season_id=all");
        var consumptionEntry = diary.GetProperty("data").EnumerateArray()
            .Single(entry => entry.GetProperty("type").GetString() == "consumo");
        consumptionEntry.GetProperty("is_before_purchase_date").GetBoolean().Should().BeTrue();
    }

    /// <summary>
    /// El aviso no se dispara donde no aplica: una imputación posterior a su compra y un consumo sin
    /// compra previa —que no tiene fecha contra la que comparar— quedan limpios.
    /// </summary>
    [Fact]
    public async Task ElAviso_NoDeberia_Aparecer_Cuando_LaFechaEsPosterior_Ni_SinCompra()
    {
        var purchaseId = (await _session.PostJsonAsync("/api/v1/purchases", new
        {
            purchase_date = "2026-07-31",
            product = "Abono NPK",
            season_id = _seasonId,
            total_quantity = 200m,
            total_cost = 400m
        })).GetProperty("id").GetGuid();

        var imputation = await _session.PostJsonAsync($"/api/v1/purchases/{purchaseId}/consumptions", new
        {
            date = "2026-08-05",
            plot_id = _plotId,
            quantity = 50m
        });
        imputation.GetProperty("is_before_purchase_date").GetBoolean().Should().BeFalse();

        var withoutPurchase = await _session.PostJsonAsync("/api/v1/consumptions", new
        {
            date = "2020-01-01",
            plot_id = _plotId,
            season_id = _seasonId,
            product = "Cobre de la nave",
            quantity = 5m
        });
        withoutPurchase.GetProperty("is_before_purchase_date").GetBoolean().Should().BeFalse();
        withoutPurchase.GetProperty("purchase_date").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
