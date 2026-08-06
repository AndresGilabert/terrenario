using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Controllers;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Auth;

public class AuthControllerTelemetryTests
{
    private readonly ILoginTelemetry _telemetry = Substitute.For<ILoginTelemetry>();

    // Solo la acción de telemetría entra en juego: el resto de dependencias no se invoca.
    private AuthController CreateSut() =>
        new(null!, null!, null!, null!, _telemetry, NullLogger<AuthController>.Instance);

    private const string ValidFlowId = "0123456789abcdef";
    private const string ValidSessionId = "fedcba9876543210";

    [Fact]
    public void LoginTelemetry_Acepta_Y_EmiteScreenViewed_Cuando_EsValido()
    {
        var result = CreateSut().LoginTelemetry(
            new LoginTelemetryRequest(LoginFunnelEvents.ScreenViewed, ValidFlowId));

        result.Should().BeOfType<AcceptedResult>();
        _telemetry.Received(1).LoginScreenViewed(Arg.Is<LoginEventContext>(c => c.FlowId == ValidFlowId));
    }

    [Fact]
    public void LoginTelemetry_EmiteAbandono_Cuando_EsEventoDeAbandono()
    {
        CreateSut().LoginTelemetry(new LoginTelemetryRequest(LoginFunnelEvents.Abandonment, ValidFlowId));

        _telemetry.Received(1).LoginAbandoned(Arg.Is<LoginEventContext>(c => c.FlowId == ValidFlowId));
    }

    [Fact]
    public void LoginTelemetry_Rechaza_Cuando_ElEventoNoEsIngestablePorCliente()
    {
        // El éxito es autoritativo del servidor: el cliente no puede falsear la conversión.
        var result = CreateSut().LoginTelemetry(
            new LoginTelemetryRequest(LoginFunnelEvents.Success, ValidFlowId));

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<ApiErrorResponse>()
            .Which.Error.Code.Should().Be(ErrorCodes.ValidationRequired);
        _telemetry.DidNotReceive().LoginSuccess(Arg.Any<LoginEventContext>());
    }

    [Fact]
    public void LoginTelemetry_Rechaza_Cuando_ElFlowIdEsInvalido()
    {
        var result = CreateSut().LoginTelemetry(
            new LoginTelemetryRequest(LoginFunnelEvents.ScreenViewed, "flow con espacios"));

        result.Should().BeOfType<BadRequestObjectResult>();
        _telemetry.DidNotReceive().LoginScreenViewed(Arg.Any<LoginEventContext>());
    }

    [Fact]
    public void LoginTelemetry_Rechaza_Cuando_ElEventoEsDesconocido()
    {
        var result = CreateSut().LoginTelemetry(new LoginTelemetryRequest("login_teleport", ValidFlowId));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── MVP-601 — Dimensiones mínimas ────────────────────────────────────────────

    [Fact]
    public void LoginTelemetry_PropagaSesionYDispositivo_Cuando_ElClienteLosEnvia()
    {
        CreateSut().LoginTelemetry(new LoginTelemetryRequest(
            LoginFunnelEvents.ScreenViewed, ValidFlowId, ValidSessionId, TelemetryDimensions.DeviceMobile));

        _telemetry.Received(1).LoginScreenViewed(Arg.Is<LoginEventContext>(c =>
            c.FlowId == ValidFlowId &&
            c.SessionId == ValidSessionId &&
            c.DeviceType == TelemetryDimensions.DeviceMobile &&
            c.Channel == TelemetryDimensions.ChannelWeb));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("sesión con acentos", "telepuerto")]
    [InlineData("", "")]
    public void LoginTelemetry_DegradaADesconocido_SinDescartarElEvento_Cuando_LasDimensionesNoSirven(
        string? sessionId, string? deviceType)
    {
        // Perder la conversión entera por una dimensión secundaria sería peor medida, y además dejaría
        // al cliente decidir qué se cuenta con solo mandar basura.
        var result = CreateSut().LoginTelemetry(new LoginTelemetryRequest(
            LoginFunnelEvents.GoogleClicked, ValidFlowId, sessionId, deviceType));

        result.Should().BeOfType<AcceptedResult>();
        _telemetry.Received(1).LoginGoogleClicked(Arg.Is<LoginEventContext>(c =>
            c.SessionId == TelemetryDimensions.Unknown &&
            c.DeviceType == TelemetryDimensions.Unknown));
    }
}
