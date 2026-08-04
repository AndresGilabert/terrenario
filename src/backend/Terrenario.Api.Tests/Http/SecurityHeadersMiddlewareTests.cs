using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Terrenario.Api.Common.Http;

namespace Terrenario.Api.Tests.Http;

public class SecurityHeadersMiddlewareTests
{
    private const string PoliticaSpa =
        "default-src 'self'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'";

    private static SecurityHeadersMiddleware Middleware(
        RequestDelegate next, string? politicaSpa = PoliticaSpa)
        => new(next, new SpaContentSecurityPolicy(politicaSpa));

    private static DefaultHttpContext Contexto(string ruta)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = ruta;
        return context;
    }

    [Fact]
    public async Task Deberia_AnadirLosHeadersDeSeguridad_AObligatorios()
    {
        var context = Contexto("/api/v1/diary");
        var nextCalled = false;
        var middleware = Middleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers["Content-Security-Policy"].ToString().Should().Be("default-src 'self'");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
        context.Response.Headers["Strict-Transport-Security"].ToString()
            .Should().Be("max-age=31536000; includeSubDomains");
    }

    [Fact]
    public async Task Deberia_NoDuplicarHeaders_Cuando_YaEstabanPresentes()
    {
        var context = Contexto("/api/v1/diary");
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        var middleware = Middleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Frame-Options"].Should().ContainSingle().Which.Should().Be("DENY");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/app/diario")]
    [InlineData("/legal/privacidad")]
    [InlineData("/assets/index-abc123.js")]
    public async Task Deberia_ServirLaPoliticaDelCliente_FueraDeLaApi(string ruta)
    {
        // Servirle al documento del SPA la política de la API lo rompería: el navegador aplica la
        // **intersección** de la cabecera y el `meta`, así que la más estricta gana y los estilos
        // calculados del dashboard dejarían de pintarse.
        var context = Contexto(ruta);

        await Middleware(_ => Task.CompletedTask).InvokeAsync(context);

        context.Response.Headers["Content-Security-Policy"].ToString().Should().Be(PoliticaSpa);
    }

    [Theory]
    [InlineData("/api/v1/diary")]
    [InlineData("/api/v1/auth/refresh")]
    public async Task Deberia_MantenerLaPoliticaCerrada_EnLaApi(string ruta)
    {
        // Las respuestas de la API son JSON: no hay contexto de ejecución, así que la más cerrada
        // posible no cuesta nada y no debe relajarse por compartir origen con el cliente.
        var context = Contexto(ruta);

        await Middleware(_ => Task.CompletedTask).InvokeAsync(context);

        context.Response.Headers["Content-Security-Policy"].ToString().Should().Be("default-src 'self'");
    }

    [Fact]
    public async Task Deberia_CaerALaPoliticaCerrada_Cuando_NoHayClienteDesplegado()
    {
        // API sin cliente —o ejecución local sin haber compilado el front—: ante la duda, la más
        // restrictiva. Lo contrario sería servir un documento sin política.
        var context = Contexto("/");

        await Middleware(_ => Task.CompletedTask, politicaSpa: null).InvokeAsync(context);

        context.Response.Headers["Content-Security-Policy"].ToString().Should().Be("default-src 'self'");
    }
}
