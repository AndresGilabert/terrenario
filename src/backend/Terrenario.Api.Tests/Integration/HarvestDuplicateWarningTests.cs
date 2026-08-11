using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-805 (RN-044, <c>RU-24</c>) — El aviso de cosecha repetida, contra la API real.
///
/// El escenario es el que la revisión del MVP reprodujo en el navegador: con la partida del 20 de
/// octubre en un terreno ya registrada, volver a abrir el formulario con ese mismo terreno, esa misma
/// fecha y ese mismo producto **no producía ningún aviso**. No había lógica de duplicados en
/// producción; la única que existía era la unicidad de nombre de los maestros, que es otra cosa —una
/// guarda que bloquea, no un aviso—.
///
/// Lo que se fija aquí es la **comparación**, que es donde está la decisión de producto: qué cuenta
/// como duplicado y qué no.
/// </summary>
public sealed class HarvestDuplicateWarningTests : IAsyncLifetime
{
    private readonly TerrenarioApiFactory _factory = new();

    private ApiSession _session = null!;
    private Guid _seasonId;
    private Guid _otraSeasonId;
    private Guid _matorralId;
    private Guid _laViaId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _factory.Google.WithIdentity("codigo", "sub", "Andrés", "andres@ejemplo.test");
        _session = await ApiSession.LoginAsync(_factory, "codigo");
        await _session.CreateWorkspaceAsync("Finca El Olivar");

        _seasonId = await CreateSeasonAsync("Campaña 2025/26", "2025-10-01", "2026-03-31");
        _otraSeasonId = await CreateSeasonAsync("Campaña 2024/25", "2024-10-01", "2025-03-31");

        _matorralId = await CreatePlotAsync("Matorral");
        _laViaId = await CreatePlotAsync("La Via");

        // La partida que ya está: 20 de octubre, Matorral, aceituna de olivar.
        await CreateHarvestAsync("2025-10-20", _matorralId, _seasonId, 1_000m, "aceite_para_venta");
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private async Task<Guid> CreateSeasonAsync(string name, string start, string end)
        => (await _session.PostJsonAsync("/api/v1/seasons", new { name, start_date = start, end_date = end }))
            .GetProperty("id").GetGuid();

    private async Task<Guid> CreatePlotAsync(string name)
        => (await _session.PostJsonAsync("/api/v1/plots", new { name, ownership_type = "propia" }))
            .GetProperty("id").GetGuid();

    private async Task<Guid> CreateHarvestAsync(
        string date, Guid plotId, Guid seasonId, decimal kgs, string destination)
        => (await _session.PostJsonAsync("/api/v1/harvests", new
        {
            date,
            plot_id = plotId,
            season_id = seasonId,
            product = "aceituna_olivar",
            kgs,
            destination
        })).GetProperty("id").GetGuid();

    private Task<JsonElement> DuplicatesAsync(
        string date, Guid plotId, string product = "aceituna_olivar", Guid? excludeId = null)
    {
        var url = $"/api/v1/harvests/duplicates?plot_id={plotId}&date={date}&product={product}";
        if (excludeId is { } id) url += $"&exclude_id={id}";
        return _session.GetJsonAsync(url);
    }

    [Fact]
    public async Task Deberia_AvisarYNombrarLaPartida_Cuando_CoincidenTerrenoFechaYProducto()
    {
        // CA-1 — el aviso tiene que **nombrar** la partida existente: sin sus kilos y su destino, quien
        // lo lee no puede distinguir si es la misma que acaba de apuntar o una segunda de verdad.
        var response = await DuplicatesAsync("2025-10-20", _matorralId);

        var duplicado = response.GetProperty("data").EnumerateArray().Should().ContainSingle().Subject;
        duplicado.GetProperty("kgs").GetDecimal().Should().Be(1_000m);
        duplicado.GetProperty("destination").GetString().Should().Be("aceite_para_venta");
    }

    [Fact]
    public async Task NoDeberia_Avisar_Cuando_CambiaElTerrenoOLaFecha()
    {
        (await DuplicatesAsync("2025-10-20", _laViaId)).GetProperty("meta")
            .GetProperty("total").GetInt32().Should().Be(0);
        (await DuplicatesAsync("2025-10-21", _matorralId)).GetProperty("meta")
            .GetProperty("total").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Deberia_Avisar_AunqueLaCampanaSeaOtra()
    {
        // La temporada **no** entra en la comparación: dos apuntes del mismo día en el mismo terreno
        // son el duplicado que se busca aunque estén asociados a campañas distintas, que es de hecho un
        // síntoma más de que uno de los dos sobra.
        await CreateHarvestAsync("2024-10-20", _matorralId, _otraSeasonId, 500m, "venta_aceituna");

        var response = await DuplicatesAsync("2024-10-20", _matorralId);

        response.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task NoDeberia_AvisarDeSiMisma_Cuando_SeCorrigeLaPartida()
    {
        // CA-3 — corregir el destino de una cosecha no puede avisar de que esa cosecha ya existe.
        var propia = (await _session.GetJsonAsync("/api/v1/harvests?season_id=all"))
            .GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();

        var response = await DuplicatesAsync("2025-10-20", _matorralId, excludeId: propia);

        response.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task NoDeberia_Avisar_Cuando_LaPartidaIgualEstaEliminada()
    {
        // CA-4 — el borrado de RN-037 es lógico, así que la fila sigue en la tabla. Si el aviso la
        // contara, borrar una partida y volver a apuntarla avisaría de un duplicado que ya no existe.
        var existente = (await _session.GetJsonAsync("/api/v1/harvests?season_id=all"))
            .GetProperty("data").EnumerateArray().First();
        var id = existente.GetProperty("id").GetGuid();
        var version = existente.GetProperty("version").GetInt32();

        (await _session.DeleteAsync($"/api/v1/harvests/{id}", version)).EnsureSuccessStatusCode();

        var response = await DuplicatesAsync("2025-10-20", _matorralId);

        response.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Deberia_Rechazar_Cuando_FaltaAlgunoDeLosTresCampos()
    {
        // Sin los tres, la pregunta no se puede formular: responder «no hay duplicados» sería peor,
        // porque el formulario mostraría silencio donde en realidad no se ha comprobado nada.
        var sinFecha = await _session.GetAsync(
            $"/api/v1/harvests/duplicates?plot_id={_matorralId}&product=aceituna_olivar");
        var sinTerreno = await _session.GetAsync(
            "/api/v1/harvests/duplicates?date=2025-10-20&product=aceituna_olivar");

        sinFecha.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        sinTerreno.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NoDeberia_VerPartidasDeOtroWorkspace()
    {
        // RN-034 — el Workspace lo resuelve el servidor y nunca viaja como parámetro. Un aviso que
        // mirase fuera del Workspace activo filtraría la existencia de datos ajenos.
        await _session.CreateWorkspaceAsync("Otra finca");

        var response = await DuplicatesAsync("2025-10-20", _matorralId);

        response.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(0);
    }
}
