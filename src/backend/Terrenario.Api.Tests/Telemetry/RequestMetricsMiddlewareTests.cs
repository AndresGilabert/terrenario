using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Telemetry;

/// <summary>
/// MVP-603 (CA-1) — La materia prima de los tres SLO: cuántas peticiones, cuántas fallan y cuánto
/// tardan.
/// </summary>
public class RequestMetricsMiddlewareTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));
    private readonly TelemetryCounterAccumulator _counters;

    public RequestMetricsMiddlewareTests() => _counters = new TelemetryCounterAccumulator(_clock);

    private async Task<Dictionary<string, long>> InvokeAsync(
        string path, int statusCode = 200, string method = "GET")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;

        var middleware = new RequestMetricsMiddleware(ctx =>
        {
            ctx.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, _counters);

        return _counters.Drain().ToDictionary(c => c.Metric, c => c.Value);
    }

    [Fact]
    public async Task Deberia_ContarLaPeticionYSuLatencia()
    {
        var contadores = await InvokeAsync("/api/v1/plots");

        contadores[TelemetryMetrics.ApiRequests].Should().Be(1);
        contadores.Keys.Should().Contain(k => k.StartsWith("api.latency_ms.bucket."));
    }

    [Theory]
    [InlineData(200, null)]
    [InlineData(404, TelemetryMetrics.ApiRequests4xx)]
    [InlineData(500, TelemetryMetrics.ApiRequests5xx)]
    public async Task Deberia_SepararElErrorVisibleDelFalloDelServidor(int status, string? esperado)
    {
        // El SLO habla de 5xx; el `tasa_error_funcional_visible` de la revisión de negocio, de 4xx.
        // Mezclarlos haría que un formulario mal rellenado contase como caída del servicio.
        var contadores = await InvokeAsync("/api/v1/plots", status);

        if (esperado is null)
        {
            contadores.Should().NotContainKey(TelemetryMetrics.ApiRequests4xx);
            contadores.Should().NotContainKey(TelemetryMetrics.ApiRequests5xx);
        }
        else
        {
            contadores[esperado].Should().Be(1);
        }
    }

    [Fact]
    public async Task Deberia_IgnorarLoQueNoEsLaApi()
    {
        // Los ficheros del cliente los sirve el mismo proceso. Contarlos hundiría la latencia media y
        // metería en el divisor del SLO peticiones que no ejecutan nada del servidor.
        var contadores = await InvokeAsync("/legal/privacidad");

        contadores.Should().BeEmpty();
    }

    [Fact]
    public async Task Deberia_ContarElAlta_Cuando_UnPostDevuelve201()
    {
        var contadores = await InvokeAsync("/api/v1/harvests", 201, "POST");

        contadores[TelemetryMetrics.ApiCreated].Should().Be(1);
        contadores[TelemetryMetrics.CreatedFor("harvests")].Should().Be(1);
    }

    [Theory]
    [InlineData(200, "POST")]
    [InlineData(201, "GET")]
    public async Task Deberia_NoContarComoAlta_LoQueNoLoEs(int status, string method)
        => (await InvokeAsync("/api/v1/harvests", status, method))
            .Should().NotContainKey(TelemetryMetrics.ApiCreated);

    [Fact]
    public async Task Deberia_ContarComo5xx_LaExcepcionSinCapturar()
    {
        // Acaba en 500 para quien llama, así que tiene que contar como 500 en el SLO. Si solo se mirase
        // el código de respuesta ya escrito, este caso desaparecería del numerador.
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/plots";

        var middleware = new RequestMetricsMiddleware(_ => throw new InvalidOperationException("boom"));

        var act = async () => await middleware.InvokeAsync(context, _counters);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _counters.Drain().Should().Contain(c => c.Metric == TelemetryMetrics.ApiRequests5xx);
    }
}
