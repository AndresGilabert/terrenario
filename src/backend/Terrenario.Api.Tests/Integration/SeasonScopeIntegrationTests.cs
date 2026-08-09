using FluentAssertions;
using System.Text.Json;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-701 (`P-082`) — El <b>defecto de temporada</b> de RN-008 aplicado por igual a todas las
/// lecturas operativas, contra la API real.
///
/// El escenario reproduce el que destapó el punto: dos campañas con producción, y la pregunta
/// «¿cuánto llevo esta campaña?» hecha desde dos pantallas distintas. Antes de esta historia
/// <c>GET /harvests</c> respondía con el histórico entero y <c>GET /dashboard/summary</c> con la
/// campaña de trabajo, así que el listado de Cosechas rotulaba 5.460,5 kg donde la Visión General
/// rotulaba 4.460,5.
///
/// <b>La comparación numérica es el test</b>, no un detalle: el punto no se detectó leyendo código
/// —que parecía correcto en las dos pantallas— sino sumando kilos y viendo que no cuadraban.
/// </summary>
public sealed class SeasonScopeIntegrationTests : IAsyncLifetime
{
    private readonly TerrenarioApiFactory _factory = new();

    private ApiSession _session = null!;
    private Guid _currentSeasonId;
    private Guid _previousSeasonId;
    private Guid _plotId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _factory.Google.WithIdentity("codigo", "sub", "Andrés", "andres@ejemplo.test");
        _session = await ApiSession.LoginAsync(_factory, "codigo");
        await _session.CreateWorkspaceAsync("Finca El Olivar");

        _previousSeasonId = await CreateSeasonAsync("Campaña 2024/25", "2024-10-01", "2025-03-31");
        _currentSeasonId = await CreateSeasonAsync("Campaña 2025/26", "2025-10-01", "2026-03-31");

        // La de trabajo es la actual: es sobre la que se pregunta «cuánto llevo».
        await _session.PostJsonAsync($"/api/v1/seasons/{_currentSeasonId}/activate", new { });

        _plotId = (await _session.PostJsonAsync("/api/v1/plots", new { name = "La Hoya", ownership_type = "propia" }))
            .GetProperty("id").GetGuid();

        // 4.460,5 kg en la campaña de trabajo, repartidos en dos partidas…
        await CreateHarvestAsync("2025-12-05", _currentSeasonId, 3_460.5m);
        await CreateHarvestAsync("2025-12-18", _currentSeasonId, 1_000m);
        // …y 1.000 kg en la anterior, que es la partida que se colaba en el total.
        await CreateHarvestAsync("2024-12-10", _previousSeasonId, 1_000m);

        await _session.PostJsonAsync("/api/v1/purchases", new
        {
            purchase_date = "2025-11-02",
            product = "Abono foliar",
            season_id = _currentSeasonId,
            total_quantity = 200m,
            total_cost = 400m
        });
        await _session.PostJsonAsync("/api/v1/purchases", new
        {
            purchase_date = "2024-11-02",
            product = "Abono foliar",
            season_id = _previousSeasonId,
            total_quantity = 100m,
            total_cost = 150m
        });
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private async Task<Guid> CreateSeasonAsync(string name, string start, string end)
        => (await _session.PostJsonAsync("/api/v1/seasons", new { name, start_date = start, end_date = end }))
            .GetProperty("id").GetGuid();

    private Task<JsonElement> CreateHarvestAsync(string date, Guid seasonId, decimal kgs)
        => _session.PostJsonAsync("/api/v1/harvests", new
        {
            date,
            plot_id = _plotId,
            season_id = seasonId,
            product = "aceituna_olivar",
            kgs,
            destination = "aceite_para_venta"
        });

    [Fact]
    public async Task Deberia_CoincidirConElDashboard_Cuando_SePidenLasCosechasSinFiltro()
    {
        var harvests = await _session.GetJsonAsync("/api/v1/harvests");
        var summary = await _session.GetJsonAsync("/api/v1/dashboard/summary");

        // El contraste exacto de `P-082`: las dos pantallas responden a la misma pregunta.
        harvests.GetProperty("meta").GetProperty("total_kg").GetDecimal()
            .Should().Be(summary.GetProperty("total_kg").GetDecimal());
        harvests.GetProperty("meta").GetProperty("total").GetInt32()
            .Should().Be(summary.GetProperty("harvests").GetInt32());

        harvests.GetProperty("meta").GetProperty("total_kg").GetDecimal().Should().Be(4_460.5m);
        harvests.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Deberia_PublicarElAmbitoAplicado_Cuando_NoSePideTemporada()
    {
        var harvests = await _session.GetJsonAsync("/api/v1/harvests");
        var scope = harvests.GetProperty("meta").GetProperty("scope");

        scope.GetProperty("all_seasons").GetBoolean().Should().BeFalse();
        scope.GetProperty("season").GetProperty("id").GetGuid().Should().Be(_currentSeasonId);
        // Sin el nombre, la pantalla no podría decir de qué campaña son las cifras que enseña.
        scope.GetProperty("season").GetProperty("name").GetString().Should().Be("Campaña 2025/26");
    }

    [Fact]
    public async Task Deberia_DevolverElHistoricoEntero_Cuando_SePideTodasLasTemporadas()
    {
        var harvests = await _session.GetJsonAsync("/api/v1/harvests?season_id=all");

        harvests.GetProperty("meta").GetProperty("total_kg").GetDecimal().Should().Be(5_460.5m);
        harvests.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(3);
        harvests.GetProperty("meta").GetProperty("scope").GetProperty("all_seasons").GetBoolean()
            .Should().BeTrue();
    }

    [Fact]
    public async Task Deberia_SeguirLaTemporadaDeTrabajo_Cuando_SeCambiaDeCampana()
    {
        await _session.PostJsonAsync($"/api/v1/seasons/{_previousSeasonId}/activate", new { });

        var harvests = await _session.GetJsonAsync("/api/v1/harvests");

        harvests.GetProperty("meta").GetProperty("total_kg").GetDecimal().Should().Be(1_000m);
        harvests.GetProperty("meta").GetProperty("scope").GetProperty("season").GetProperty("id")
            .GetGuid().Should().Be(_previousSeasonId);
    }

    [Fact]
    public async Task Deberia_AplicarElMismoAmbito_Cuando_SeLeenDiarioComprasYConsumos()
    {
        var diary = await _session.GetJsonAsync("/api/v1/diary");
        var purchases = await _session.GetJsonAsync("/api/v1/purchases");
        var consumptions = await _session.GetJsonAsync("/api/v1/consumptions");

        foreach (var meta in new[] { diary, purchases, consumptions }.Select(b => b.GetProperty("meta")))
        {
            meta.GetProperty("scope").GetProperty("season").GetProperty("id").GetGuid()
                .Should().Be(_currentSeasonId);
        }

        // El libro de compras deja fuera la de la campaña anterior, como el listado de cosechas.
        purchases.GetProperty("meta").GetProperty("total_cost").GetDecimal().Should().Be(400m);
        // Y el diario no mezcla campañas: 2 cosechas + 1 compra de la actual.
        diary.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task Deberia_CaerAlDefecto_Cuando_LaTemporadaPedidaNoEsDeEsteWorkspace()
    {
        // Desde MVP-705 el filtro viaja en la URL: al cambiar de Workspace puede quedar el de otro.
        // Responder con el histórico entero sería peor que aplicar el defecto y decir cuál se aplicó.
        var harvests = await _session.GetJsonAsync($"/api/v1/harvests?season_id={Guid.NewGuid()}");

        harvests.GetProperty("meta").GetProperty("scope").GetProperty("season").GetProperty("id")
            .GetGuid().Should().Be(_currentSeasonId);
        harvests.GetProperty("meta").GetProperty("total_kg").GetDecimal().Should().Be(4_460.5m);
    }
}
