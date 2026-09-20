using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Terrenario.Api.Common.Http;

namespace Terrenario.Api.Tests.Http;

/// <summary>
/// PLT-101 — Los dominios comprados solo para no perderlos no tienen contenido propio: quien llega
/// por ellos se redirige, permanentemente, al dominio canónico, con la misma ruta y query.
/// </summary>
public class AlternateDomainRedirectMiddlewareTests
{
    private static readonly string[] AlternateHosts =
        ["terrenario.com", "www.terrenario.com", "terrenario.es", "www.terrenario.es"];

    private async Task<(int StatusCode, string? Location, bool LlamoAlSiguiente)> InvokeAsync(
        string host, string path = "/", string query = "", string[]? alternateHosts = null)
    {
        var options = Options.Create(new DomainRedirectOptions
        {
            CanonicalHost = "app.terrenario.com",
            AlternateHosts = alternateHosts ?? AlternateHosts
        });

        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(query);

        var llamoAlSiguiente = false;
        var middleware = new AlternateDomainRedirectMiddleware(_ =>
        {
            llamoAlSiguiente = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, options);

        return (
            context.Response.StatusCode,
            context.Response.Headers.Location.FirstOrDefault(),
            llamoAlSiguiente);
    }

    [Theory]
    [InlineData("terrenario.com")]
    [InlineData("www.terrenario.com")]
    [InlineData("terrenario.es")]
    [InlineData("www.terrenario.es")]
    [InlineData("TERRENARIO.COM")]
    public async Task Deberia_Redirigir301AlDominioCanonico_ManteniendoRutaYQuery(string host)
    {
        var (statusCode, location, siguiente) = await InvokeAsync(host, "/funcionalidades/gestion-terrenos", "?utm_source=x");

        statusCode.Should().Be(StatusCodes.Status301MovedPermanently);
        location.Should().Be("https://app.terrenario.com/funcionalidades/gestion-terrenos?utm_source=x");
        siguiente.Should().BeFalse();
    }

    [Fact]
    public async Task Deberia_IgnorarElPuerto_AlComprobarElHost()
    {
        var (statusCode, location, _) = await InvokeAsync("terrenario.com:443", "/");

        statusCode.Should().Be(StatusCodes.Status301MovedPermanently);
        location.Should().Be("https://app.terrenario.com/");
    }

    [Fact]
    public async Task Deberia_SeguirElPipeline_Cuando_ElHostEsElCanonico()
    {
        var (_, _, siguiente) = await InvokeAsync("app.terrenario.com");

        siguiente.Should().BeTrue();
    }

    [Fact]
    public async Task Deberia_SeguirElPipeline_Cuando_NoHayHostsConfigurados()
    {
        var (_, _, siguiente) = await InvokeAsync("terrenario.com", alternateHosts: []);

        siguiente.Should().BeTrue();
    }
}
