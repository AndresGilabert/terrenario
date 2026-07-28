using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Terrenario.Api.Common.Http;

namespace Terrenario.Api.Tests.Http;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Deberia_AnadirLosHeadersDeSeguridad_AObligatorios()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;
        var middleware = new SecurityHeadersMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers["Content-Security-Policy"].ToString().Should().Be("default-src 'self'");
        context.Response.Headers["Strict-Transport-Security"].ToString()
            .Should().Be("max-age=31536000; includeSubDomains");
    }

    [Fact]
    public async Task Deberia_NoDuplicarHeaders_Cuando_YaEstabanPresentes()
    {
        var context = new DefaultHttpContext();
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Frame-Options"].Should().ContainSingle().Which.Should().Be("DENY");
    }
}
