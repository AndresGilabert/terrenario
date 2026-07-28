using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Terrenario.Api.Common.Http;

namespace Terrenario.Api.Tests.Http;

public class RequestIdMiddlewareTests
{
    private static RequestIdMiddleware CreateSut(RequestDelegate? next = null) =>
        new(next ?? (_ => Task.CompletedTask), NullLogger<RequestIdMiddleware>.Instance);

    [Fact]
    public async Task Deberia_GenerarRequestId_Cuando_NoLlegaEnLaPeticion()
    {
        var context = new DefaultHttpContext();

        await CreateSut().InvokeAsync(context);

        var requestId = context.Response.Headers[RequestIdMiddleware.HeaderName].ToString();
        requestId.Should().NotBeNullOrEmpty();
        context.TraceIdentifier.Should().Be(requestId);
    }

    [Fact]
    public async Task Deberia_PropagarElRequestIdEntrante_Cuando_EsValido()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = "req-abc_123";

        await CreateSut().InvokeAsync(context);

        context.Response.Headers[RequestIdMiddleware.HeaderName].ToString().Should().Be("req-abc_123");
    }

    [Fact]
    public async Task Deberia_GenerarUnoNuevo_Cuando_ElEntranteEsInvalido()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = "id con espacios y símbolos!";

        await CreateSut().InvokeAsync(context);

        var requestId = context.Response.Headers[RequestIdMiddleware.HeaderName].ToString();
        requestId.Should().NotBe("id con espacios y símbolos!");
        requestId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Deberia_LlamarAlSiguienteMiddleware()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;

        await CreateSut(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }).InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}
