using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Terrenario.Api.Controllers;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-603 — La superficie de operación contra la API real: la comprobación de salud que sondea la
/// plataforma y el endpoint de señales.
///
/// Aquí el arnés completo sí aporta: la sonda de salud consulta la <b>base de datos real</b>, que es
/// justo lo que la hace distinta de un «200 OK» cualquiera, y el endpoint de señales solo se puede
/// comprobar de verdad pasando por la tubería entera de la aplicación.
/// </summary>
public sealed class HealthAndOpsTests : IAsyncLifetime
{
    private readonly TerrenarioApiFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Health_Deberia_ResponderSano_Cuando_AlcanzaLaBaseDeDatos()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("healthy");
        body.GetProperty("database").GetString().Should().Be("healthy");
    }

    [Fact]
    public async Task Health_Deberia_SerAnonimo()
    {
        // La sonda de la plataforma no tiene sesión. Si exigiera token, la alerta `ServiceDown` no se
        // podría cablear a nada.
        var response = await _factory.CreateClient().GetAsync("/api/v1/health");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OpsSignals_Deberia_NoExistir_Cuando_NoHayLlaveConfigurada()
    {
        // El arnés no configura `Ops:ApiKey`, así que este es el estado por defecto de un despliegue
        // que no lo haya configurado: no se puede consultar, en vez de quedar abierto.
        var response = await _factory.CreateClient().GetAsync("/api/v1/ops/signals");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OpsSignals_Deberia_ExigirLaLlave_Cuando_EstaConfigurada()
    {
        await using var factory = new TerrenarioApiFactory();
        await factory.InitializeAsync();
        var client = factory.WithOpsKey("llave").CreateClient();

        (await client.GetAsync("/api/v1/ops/signals")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/ops/signals");
        request.Headers.Add(OpsController.ApiKeyHeader, "llave");

        var authorized = await client.SendAsync(request);
        authorized.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await authorized.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("slo", out _).Should().BeTrue();
        body.TryGetProperty("login_funnel_7d", out _).Should().BeTrue();
        body.TryGetProperty("product_usage_7d", out _).Should().BeTrue();
        body.TryGetProperty("business_7d", out _).Should().BeTrue();
        body.GetProperty("alerts").GetArrayLength().Should().Be(0, "la vigilancia está apagada en el arnés");
    }

    /// <summary>
    /// MVP-699 (`R-01`) — La serie diaria, que es lo que permite comparar una semana con la anterior y
    /// fijar el baseline que pide <c>kpis.md</c>.
    /// </summary>
    [Fact]
    public async Task OpsSignals_Deberia_DevolverLaSerieDiariaConElRangoPedido()
    {
        await using var factory = new TerrenarioApiFactory();
        await factory.InitializeAsync();
        var client = factory.WithOpsKey("llave").CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/ops/signals?days=10");
        request.Headers.Add(OpsController.ApiKeyHeader, "llave");

        var body = await (await client.SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("daily_days").GetInt32().Should().Be(10);
        var dias = body.GetProperty("daily");
        dias.GetArrayLength().Should().Be(10);

        // Las fechas llegan ordenadas y sin huecos: una serie con dias omitidos no se puede leer como
        // tendencia, que es justo para lo que existe.
        var fechas = dias.EnumerateArray()
            .Select(d => DateOnly.Parse(d.GetProperty("date").GetString()!))
            .ToArray();

        fechas.Should().BeInAscendingOrder();
        fechas[^1].Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        fechas.Zip(fechas.Skip(1)).Should().OnlyContain(par => par.Second == par.First.AddDays(1));
    }

    [Fact]
    public async Task OpsSignals_Deberia_UsarElRangoPorDefecto_Cuando_NoSePide()
    {
        await using var factory = new TerrenarioApiFactory();
        await factory.InitializeAsync();
        var client = factory.WithOpsKey("llave").CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/ops/signals");
        request.Headers.Add(OpsController.ApiKeyHeader, "llave");

        var body = await (await client.SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("daily_days").GetInt32().Should().Be(28);
    }

    /// <summary>
    /// MVP-706 (CA-5) — `dashboard.manual_refresh` deja de publicarse. El boton «Actualizar» era su
    /// unica fuente y el PO lo retiro, asi que seguir informandola daria siempre cero: se leeria como
    /// «nadie refresca» en vez de como «esto ya no se mide».
    /// </summary>
    [Fact]
    public async Task OpsSignals_NoDeberia_PublicarLaRecargaManual_Discontinuada_EnMvp706()
    {
        await using var factory = new TerrenarioApiFactory();
        await factory.InitializeAsync();
        var client = factory.WithOpsKey("llave").CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/ops/signals");
        request.Headers.Add(OpsController.ApiKeyHeader, "llave");

        var body = await (await client.SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>();

        var usage = body.GetProperty("product_usage_7d");
        usage.TryGetProperty("manual_refresh_per_session", out _).Should().BeFalse();
        // La cobertura de widgets sigue viva: lo que se retira es la recarga manual, no el bloque.
        usage.TryGetProperty("widget_coverage", out _).Should().BeTrue();

        foreach (var day in body.GetProperty("daily").EnumerateArray())
            day.TryGetProperty("manual_refresh", out _).Should().BeFalse();
    }
}
