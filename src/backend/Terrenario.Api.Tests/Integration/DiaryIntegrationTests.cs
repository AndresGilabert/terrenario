using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-501, CA-2 — El <b>diario unificado</b> (`GET /api/v1/diary`) contra la API real. Es la vista
/// principal del MVP (RN-033) y la que `MVP-506` va a reescribir para paginar en SQL, así que esta
/// cobertura es la red de regresión de esa reescritura: fija lo que el endpoint debe seguir
/// respondiendo cuando la mezcla deje de hacerse en memoria.
/// </summary>
public sealed class DiaryIntegrationTests : IAsyncLifetime
{
    private readonly TerrenarioApiFactory _factory = new();

    private ApiSession _session = null!;
    private Guid _seasonId;
    private Guid _otherSeasonId;
    private Guid _hoyaId;
    private Guid _cerroId;
    private Guid _workerId;

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

        _otherSeasonId = (await _session.PostJsonAsync("/api/v1/seasons", new
        {
            name = "Campaña 2024/25",
            start_date = "2024-10-01",
            end_date = "2025-03-31"
        })).GetProperty("id").GetGuid();

        _hoyaId = (await _session.PostJsonAsync("/api/v1/plots", new { name = "La Hoya", ownership_type = "propia" }))
            .GetProperty("id").GetGuid();
        _cerroId = (await _session.PostJsonAsync("/api/v1/plots", new { name = "El Cerro", ownership_type = "propia" }))
            .GetProperty("id").GetGuid();
        _workerId = (await _session.PostJsonAsync("/api/v1/workers", new { name = "Antonio Ruiz" }))
            .GetProperty("id").GetGuid();

        await _session.PostJsonAsync("/api/v1/activities", new
        {
            date = "2025-11-12",
            plot_id = _hoyaId,
            season_id = _seasonId,
            worker_id = _workerId,
            task_text = "Poda",
            hours = 6m,
            manual_cost = 75m
        });

        await _session.PostJsonAsync("/api/v1/activities", new
        {
            date = "2025-11-20",
            plot_id = _cerroId,
            season_id = _seasonId,
            worker_id = _workerId,
            task_text = "Riego",
            hours = 2m,
            manual_cost = 20m
        });

        await _session.PostJsonAsync("/api/v1/harvests", new
        {
            date = "2025-12-05",
            plot_id = _hoyaId,
            season_id = _seasonId,
            product = "aceituna_olivar",
            kgs = 4200m,
            destination = "aceite_para_venta",
            liters = 840m
        });

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

    private async Task<(List<JsonElement> Entries, JsonElement Meta)> DiaryAsync(string query = "")
    {
        var body = await _session.GetJsonAsync($"/api/v1/diary{query}");
        return (body.GetProperty("data").EnumerateArray().ToList(), body.GetProperty("meta"));
    }

    [Fact]
    public async Task Deberia_DevolverLosCuatroTiposOrdenadosPorFechaDeNegocio_Cuando_NoHayFiltros()
    {
        var (entries, meta) = await DiaryAsync();

        entries.Should().HaveCount(4);
        entries.Select(e => e.GetProperty("date").GetString()).Should().BeInDescendingOrder();
        meta.GetProperty("total").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task Deberia_DejarFueraLasCompras_Cuando_SeFiltraPorTerreno()
    {
        // Una compra es del Workspace, no de un terreno: filtrar por terreno la excluye por
        // definición. Es la regla que el cliente explica al usuario, y aquí se fija en servidor.
        var (entries, _) = await DiaryAsync($"?plot_id={_hoyaId}");

        entries.Select(e => e.GetProperty("type").GetString()).Should().NotContain("compra");
        entries.Should().OnlyContain(e => e.GetProperty("plot_id").GetGuid() == _hoyaId);
    }

    [Fact]
    public async Task Deberia_DevolverSoloEseTipo_Cuando_SeFiltraPorTipo()
    {
        var (entries, _) = await DiaryAsync("?type=cosecha");

        entries.Should().ContainSingle()
            .Which.GetProperty("type").GetString().Should().Be("cosecha");
    }

    [Fact]
    public async Task Deberia_QuedarseVacio_Cuando_SeFiltraPorUnaTemporadaSinRegistros()
    {
        var (entries, meta) = await DiaryAsync($"?season_id={_otherSeasonId}");

        entries.Should().BeEmpty();
        meta.GetProperty("total").GetInt32().Should().Be(0);
        meta.GetProperty("total_cost").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task Deberia_AcotarPorFechaDeNegocio_Cuando_SePasaUnRango()
    {
        var (entries, _) = await DiaryAsync("?from=2025-11-15&to=2025-12-31");

        entries.Should().HaveCount(2);
        entries.Select(e => e.GetProperty("date").GetString())
            .Should().BeEquivalentTo(["2025-12-05", "2025-11-20"]);
    }

    [Fact]
    public async Task Deberia_RechazarElFiltro_Cuando_LaFechaNoTieneElFormatoDelContrato()
    {
        var response = await _session.GetAsync("/api/v1/diary?from=12-11-2025");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deberia_SumarSoloElGastoReal_Cuando_HayImputaciones()
    {
        var purchases = await _session.GetJsonAsync("/api/v1/purchases");
        var purchaseId = purchases.GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();

        await _session.PostJsonAsync($"/api/v1/purchases/{purchaseId}/consumptions", new
        {
            date = "2025-11-08",
            plot_id = _hoyaId,
            quantity = 50m
        });

        var (_, meta) = await DiaryAsync();

        // R-01 (MVP-399) — labor 75 + labor 20 + compra 400 = 495. La imputación (100 €) reparte
        // dinero que la compra ya aportó: se publica aparte, no se suma.
        meta.GetProperty("total_cost").GetDecimal().Should().Be(495m);
        meta.GetProperty("imputed_cost").GetDecimal().Should().Be(100m);
    }

    // ── MVP-506: paginación, búsqueda y responsable en el contrato HTTP ────────

    [Fact]
    public async Task Deberia_PublicarLaPosicionEnLaColeccion_Cuando_SePagina()
    {
        var (entries, meta) = await DiaryAsync("?page=1&limit=2");

        entries.Should().HaveCount(2);
        meta.GetProperty("page").GetInt32().Should().Be(1);
        meta.GetProperty("limit").GetInt32().Should().Be(2);
        // `total` es el del diario completo, no el de la página: es lo que permite saber cuántas
        // páginas hay y lo que sostiene la cabecera.
        meta.GetProperty("total").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task Deberia_DevolverElResto_Cuando_SePideLaSegundaPagina()
    {
        var (primera, _) = await DiaryAsync("?page=1&limit=3");
        var (segunda, _) = await DiaryAsync("?page=2&limit=3");

        segunda.Should().ContainSingle();
        var ids = primera.Concat(segunda).Select(e => e.GetProperty("id").GetGuid()).ToList();
        ids.Should().OnlyHaveUniqueItems().And.HaveCount(4);
    }

    [Fact]
    public async Task Deberia_ResponderConValoresPorDefecto_Cuando_NoSePidePagina()
    {
        var (_, meta) = await DiaryAsync();

        meta.GetProperty("page").GetInt32().Should().Be(1);
        meta.GetProperty("limit").GetInt32().Should().Be(20);
    }

    [Fact]
    public async Task Deberia_AcotarElTamanoDePagina_Cuando_SePideDemasiado()
    {
        // Pedir de más no es un error del cliente, pero servirlo sí sería un problema del servidor.
        var (_, meta) = await DiaryAsync("?limit=5000");

        meta.GetProperty("limit").GetInt32().Should().Be(100);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?limit=0")]
    [InlineData("?page=-3")]
    public async Task Deberia_RechazarLaPaginacion_Cuando_NoEsUnEnteroPositivo(string query)
    {
        var response = await _session.GetAsync($"/api/v1/diary{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deberia_BuscarEnServidor_Cuando_LlegaElParametroSearch()
    {
        var (entries, meta) = await DiaryAsync("?search=riego");

        entries.Should().ContainSingle().Which.GetProperty("title").GetString().Should().Be("Riego");
        // La cabecera resume lo encontrado, no el diario entero.
        meta.GetProperty("total").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Deberia_FiltrarPorResponsable_Cuando_LlegaWorkerId()
    {
        var (entries, meta) = await DiaryAsync($"?worker_id={_workerId}");

        // `P-056` — «qué hizo Antonio esta semana» ya se puede responder desde la vista principal.
        entries.Should().HaveCount(2);
        entries.Should().OnlyContain(e => e.GetProperty("type").GetString() == "actividad");
        meta.GetProperty("total").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Deberia_CombinarBusquedaYPaginacion_Cuando_LleganJuntas()
    {
        var (entries, meta) = await DiaryAsync("?search=o&page=1&limit=1");

        entries.Should().ContainSingle();
        // El total es el de la búsqueda completa, no el de la página: sin eso el cliente no sabría
        // que hay más resultados fuera de lo que ve.
        meta.GetProperty("total").GetInt32().Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task Deberia_ContarLosConsumosSinCompra_Cuando_SeRegistraUnoSuelto()
    {
        await _session.PostJsonAsync("/api/v1/consumptions", new
        {
            date = "2025-11-25",
            plot_id = _cerroId,
            season_id = _seasonId,
            product = "Cal",
            quantity = 10m
        });

        var (_, meta) = await DiaryAsync();

        // RN-032 — su coste es desconocido, no cero: el impacto en la calidad del dato queda visible.
        meta.GetProperty("consumptions_without_purchase").GetInt32().Should().Be(1);
    }
}
