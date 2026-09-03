using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Terrenario.Api.Common.Http;

namespace Terrenario.Api.Tests.Http;

/// <summary>
/// MKT-102 (riesgo pendiente, detectado en uso real) — `/funcionalidades/gestion-terrenos` (sin
/// barra final, la forma que declara el propio `canonical`) daba 404 porque `UseDefaultFiles` solo
/// resuelve `index.html` cuando la URL termina en `/`.
/// </summary>
public class PrettyUrlIndexMiddlewareTests : IDisposable
{
    private readonly string _webRoot = Directory.CreateTempSubdirectory("terrenario-pretty-url-").FullName;

    public PrettyUrlIndexMiddlewareTests()
    {
        Directory.CreateDirectory(Path.Combine(_webRoot, "funcionalidades", "gestion-terrenos"));
        File.WriteAllText(
            Path.Combine(_webRoot, "funcionalidades", "gestion-terrenos", "index.html"), "<html>landing</html>");
    }

    public void Dispose() => Directory.Delete(_webRoot, recursive: true);

    private async Task<(int StatusCode, string? ContentType, bool LlamoAlSiguiente)> InvokeAsync(
        string path, string method = "GET", string? webRoot = null)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.WebRootPath.Returns(webRoot ?? _webRoot);

        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();

        var llamoAlSiguiente = false;
        var middleware = new PrettyUrlIndexMiddleware(_ =>
        {
            llamoAlSiguiente = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, env);

        return (context.Response.StatusCode, context.Response.ContentType, llamoAlSiguiente);
    }

    [Fact]
    public async Task Deberia_ServirElIndex_Cuando_LaRutaNoLlevaBarraFinal()
    {
        var (statusCode, contentType, siguiente) = await InvokeAsync("/funcionalidades/gestion-terrenos");

        statusCode.Should().Be(StatusCodes.Status200OK);
        contentType.Should().Be("text/html; charset=utf-8");
        siguiente.Should().BeFalse();
    }

    [Fact]
    public async Task Deberia_SeguirElPipeline_Cuando_LaRutaYaLlevaBarraFinal()
    {
        // `UseDefaultFiles` ya sabe resolver este caso; no hace falta duplicar el trabajo.
        var (_, _, siguiente) = await InvokeAsync("/funcionalidades/gestion-terrenos/");

        siguiente.Should().BeTrue();
    }

    [Fact]
    public async Task Deberia_SeguirElPipeline_Cuando_NoExisteLaLanding()
    {
        var (_, _, siguiente) = await InvokeAsync("/funcionalidades/no-existe");

        siguiente.Should().BeTrue();
    }

    [Fact]
    public async Task Deberia_SeguirElPipeline_Cuando_EsUnaRutaDeApi()
    {
        var (_, _, siguiente) = await InvokeAsync("/api/v1/plots");

        siguiente.Should().BeTrue();
    }

    [Fact]
    public async Task Deberia_SeguirElPipeline_Cuando_NoEsUnGet()
    {
        var (_, _, siguiente) = await InvokeAsync("/funcionalidades/gestion-terrenos", "POST");

        siguiente.Should().BeTrue();
    }

    [Fact]
    public async Task Deberia_SeguirElPipeline_Cuando_NoHayWwwroot()
    {
        var (_, _, siguiente) = await InvokeAsync("/funcionalidades/gestion-terrenos", webRoot: "");

        siguiente.Should().BeTrue();
    }

    [Fact]
    public async Task Deberia_IgnorarIntentosDeSalirDeWwwroot()
    {
        var (_, _, siguiente) = await InvokeAsync("/funcionalidades/../../../windows/win.ini");

        siguiente.Should().BeTrue();
    }
}
