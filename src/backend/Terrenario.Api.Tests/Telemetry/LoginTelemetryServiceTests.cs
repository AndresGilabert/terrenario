using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Telemetry;

/// <summary>
/// MVP-601 — Los tres criterios de aceptación de la historia, sobre el emisor:
/// CA-1 (los cinco eventos), CA-2 (las dimensiones mínimas) y CA-3 (ninguna PII).
/// </summary>
public class LoginTelemetryServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 6, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(Ahora);
    private readonly RecordingLogger<LoginTelemetryService> _logger = new();
    private readonly TelemetryCounterAccumulator _counters;
    private readonly LoginTelemetryService _sut;

    public LoginTelemetryServiceTests()
    {
        _counters = new TelemetryCounterAccumulator(_clock);
        _sut = new LoginTelemetryService(_logger, _counters, new LoginFlowTimings(_clock), _clock);
    }

    private static LoginEventContext Context(string flowId = "flow01") =>
        LoginEventContext.Create(flowId, "session01", TelemetryDimensions.DeviceMobile);

    // ── CA-1 — Los cinco eventos del embudo ──────────────────────────────────────

    [Fact]
    public void Deberia_EmitirLosCincoEventosDelEmbudo_ConSusNombresCanonicos()
    {
        _sut.LoginScreenViewed(Context());
        _sut.LoginGoogleClicked(Context());
        _sut.LoginSuccess(Context());
        _sut.LoginError(Context(), ErrorCodes.AuthGoogleTokenInvalid);
        _sut.LoginAbandoned(Context());

        _logger.Entries.Select(e => e["Event"]).Should().Equal(
            LoginFunnelEvents.ScreenViewed,
            LoginFunnelEvents.GoogleClicked,
            LoginFunnelEvents.Success,
            LoginFunnelEvents.Error,
            LoginFunnelEvents.Abandonment);
    }

    // ── CA-2 — Dimensiones mínimas ───────────────────────────────────────────────

    [Fact]
    public void Deberia_AcompanarCadaEvento_DeLasDimensionesMinimasDeLaKb()
    {
        _sut.LoginGoogleClicked(Context());

        var evento = _logger.Last();
        evento["Timestamp"].Should().Be(Ahora.ToString("O"));
        evento["SessionId"].Should().Be("session01");
        evento["FlowId"].Should().Be("flow01");
        evento["Channel"].Should().Be(TelemetryDimensions.ChannelWeb);
        evento["DeviceType"].Should().Be(TelemetryDimensions.DeviceMobile);
    }

    [Fact]
    public void Deberia_IncluirElCodigoDeError_SoloCuandoAplica()
    {
        _sut.LoginError(Context(), ErrorCodes.AuthGoogleExchangeFailed);
        _logger.Last()["ErrorCode"].Should().Be(ErrorCodes.AuthGoogleExchangeFailed);

        _sut.LoginAbandoned(Context());
        _logger.Last().Should().NotContainKey("ErrorCode");
    }

    // ── CA-3 — Sin PII ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("screen")]
    [InlineData("error")]
    public void Deberia_EmitirSoloLasDimensionesDeclaradas_YNingunCampoMas(string caso)
    {
        if (caso == "screen") _sut.LoginScreenViewed(Context());
        else _sut.LoginError(Context(), ErrorCodes.AuthGoogleTokenInvalid);

        // La garantía de «sin PII en claro» es que el conjunto de campos sea **cerrado**: si algún día
        // alguien añade el email o el id de usuario a la traza, este test lo para.
        var esperadas = caso == "screen"
            ? new[] { "Event", "Timestamp", "SessionId", "FlowId", "Channel", "DeviceType" }
            : ["Event", "Timestamp", "SessionId", "FlowId", "Channel", "DeviceType", "ErrorCode"];

        _logger.LastDimensions().Should().BeEquivalentTo(esperadas);
    }

    // ── Contadores agregados ─────────────────────────────────────────────────────

    [Fact]
    public void Deberia_SumarUnContadorPorEvento()
    {
        _sut.LoginScreenViewed(Context("uno"));
        _sut.LoginScreenViewed(Context("dos"));
        _sut.LoginAbandoned(Context("dos"));

        var contadores = _counters.Drain().ToDictionary(c => c.Metric, c => c.Value);
        contadores[TelemetryMetrics.LoginScreenViewed].Should().Be(2);
        contadores[TelemetryMetrics.LoginAbandonment].Should().Be(1);
    }

    [Fact]
    public void Deberia_DesglosarElErrorPorCodigo_ParaPoderDistinguirloEnLaRevision()
    {
        _sut.LoginError(Context(), ErrorCodes.AuthGoogleTokenInvalid);

        var contadores = _counters.Drain().ToDictionary(c => c.Metric, c => c.Value);
        contadores[TelemetryMetrics.LoginError].Should().Be(1);
        contadores[TelemetryMetrics.LoginErrorFor(ErrorCodes.AuthGoogleTokenInvalid)].Should().Be(1);
    }

    [Fact]
    public void Deberia_MedirLaDuracionDelLogin_Cuando_ConoceElInicioDelIntento()
    {
        _sut.LoginScreenViewed(Context());
        _clock.Advance(TimeSpan.FromSeconds(12));
        _sut.LoginSuccess(Context());

        var contadores = _counters.Drain().ToDictionary(c => c.Metric, c => c.Value);
        contadores[TelemetryMetrics.LoginSuccess].Should().Be(1);
        contadores[TelemetryMetrics.LoginSuccessTimedCount].Should().Be(1);
        contadores[TelemetryMetrics.LoginSuccessDurationMsSum].Should().Be(12_000);
    }

    [Fact]
    public void Deberia_ContarElExitoSinDuracion_Cuando_NoConoceElInicio()
    {
        // Pasa tras un reinicio: el «pantalla vista» se registró en el proceso anterior. Contar ese
        // éxito con duración cero rebajaría la media y haría creer que el login es más rápido de lo
        // que es.
        _sut.LoginSuccess(Context());

        var contadores = _counters.Drain().ToDictionary(c => c.Metric, c => c.Value);
        contadores[TelemetryMetrics.LoginSuccess].Should().Be(1);
        contadores.Should().NotContainKey(TelemetryMetrics.LoginSuccessTimedCount);
        contadores.Should().NotContainKey(TelemetryMetrics.LoginSuccessDurationMsSum);
    }
}
