using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using Terrenario.Api.Application.Ops;
using Terrenario.Api.Controllers;
using Terrenario.Api.Infrastructure.Telemetry;
using Terrenario.Api.Infrastructure.Telemetry.Alerts;

namespace Terrenario.Api.Tests.Telemetry;

/// <summary>
/// MVP-603 (CA-3) — El acceso a las señales operativas. Lo que se prueba aquí es la puerta: quién puede
/// mirar y qué pasa cuando no está configurada.
/// </summary>
public class OpsControllerTests
{
    private const string Key = "llave-de-operacion";

    private static readonly DateTimeOffset Ahora = new(2026, 8, 6, 10, 0, 0, TimeSpan.Zero);

    private readonly ITelemetryCounterStore _store = Substitute.For<ITelemetryCounterStore>();

    private OpsController CreateSut(string? apiKey, string? providedKey)
    {
        _store.GetRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var clock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(Ahora);
        var signals = new OperationalSignalsService(
            _store, new RollingWindowMetrics(clock), new AlertStateStore(), clock);

        var controller = new OpsController(
            signals, Options.Create(new OpsOptions { ApiKey = apiKey ?? string.Empty }))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        if (providedKey is not null)
            controller.Request.Headers[OpsController.ApiKeyHeader] = providedKey;

        return controller;
    }

    [Fact]
    public async Task Deberia_NoExistir_Cuando_NoHayLlaveConfigurada()
    {
        // 404 y no 401: si alguna vez se despliega sin configurarlo, el fallo debe ser que no se puede
        // consultar, no que lo pueda consultar cualquiera.
        var result = await CreateSut(apiKey: null, providedKey: Key).Signals(null, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("llave-equivocada")]
    [InlineData("llave-de-operacion-mas-larga")]
    public async Task Deberia_RechazarSinLaLlaveCorrecta(string? provided)
    {
        var result = await CreateSut(Key, provided).Signals(null, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Deberia_DevolverLasSenales_ConLaLlaveCorrecta()
    {
        var result = await CreateSut(Key, Key).Signals(null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }
}
