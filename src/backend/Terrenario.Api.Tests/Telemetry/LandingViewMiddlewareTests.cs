using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Telemetry;

/// <summary>MKT-106 (CA-1) — Cuenta la visita antes de que `UseStaticFiles` la sirva, sin tocarla.</summary>
public class LandingViewMiddlewareTests : IDisposable
{
    private readonly string _webRoot = Directory.CreateTempSubdirectory("terrenario-landing-mw-").FullName;
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero));
    private readonly TelemetryCounterAccumulator _counters;

    public LandingViewMiddlewareTests()
    {
        _counters = new TelemetryCounterAccumulator(_clock);
        File.WriteAllText(Path.Combine(_webRoot, "home.html"), "<html></html>");

        Directory.CreateDirectory(Path.Combine(_webRoot, "funcionalidades", "gestion-terrenos"));
        File.WriteAllText(
            Path.Combine(_webRoot, "funcionalidades", "gestion-terrenos", "index.html"), "<html></html>");
    }

    public void Dispose() => Directory.Delete(_webRoot, recursive: true);

    private async Task<(Dictionary<string, long> Contadores, bool LlamoAlSiguiente)> InvokeAsync(
        string path, string method = "GET")
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.WebRootPath.Returns(_webRoot);

        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;

        var llamoAlSiguiente = false;
        var middleware = new LandingViewMiddleware(_ =>
        {
            llamoAlSiguiente = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, _counters, env);

        return (_counters.Drain().ToDictionary(c => c.Metric, c => c.Value), llamoAlSiguiente);
    }

    [Fact]
    public async Task Deberia_ContarLaHome()
    {
        var (contadores, siguiente) = await InvokeAsync("/");

        contadores[TelemetryMetrics.LandingViewFor("home")].Should().Be(1);
        siguiente.Should().BeTrue();
    }

    [Fact]
    public async Task Deberia_ContarUnaFuncionalidadExistente()
    {
        var (contadores, _) = await InvokeAsync("/funcionalidades/gestion-terrenos");

        contadores[TelemetryMetrics.LandingViewFor("funcionalidades.gestion-terrenos")].Should().Be(1);
    }

    [Fact]
    public async Task Deberia_NoContarNada_Cuando_LaRutaNoEsUnaLandingReal()
    {
        var (contadores, siguiente) = await InvokeAsync("/funcionalidades/no-existe");

        contadores.Should().BeEmpty();
        siguiente.Should().BeTrue();
    }

    [Fact]
    public async Task Deberia_NoContarNada_Cuando_NoEsUnGet()
    {
        var (contadores, _) = await InvokeAsync("/", "POST");

        contadores.Should().BeEmpty();
    }

    [Fact]
    public async Task Deberia_SiempreLlamarAlSiguiente_ParaQueUseStaticFilesSigaSirviendo()
    {
        var (_, siguiente) = await InvokeAsync("/api/v1/plots");

        siguiente.Should().BeTrue();
    }
}
