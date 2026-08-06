using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Telemetry;

/// <summary>
/// MVP-602 — Las señales de uso y los contadores de los que salen los KPI de producto de la KB.
/// </summary>
public class UsageTelemetryServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 6, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(Ahora);
    private readonly RecordingLogger<UsageTelemetryService> _logger = new();
    private readonly TelemetryCounterAccumulator _counters;
    private readonly UsageTelemetryService _sut;

    public UsageTelemetryServiceTests()
    {
        _counters = new TelemetryCounterAccumulator(_clock);
        _sut = new UsageTelemetryService(_logger, _counters, _clock);
    }

    private static UsageEventContext Context() =>
        UsageEventContext.Create("session01", TelemetryDimensions.DeviceDesktop);

    private Dictionary<string, long> Counters() =>
        _counters.Drain().ToDictionary(c => c.Metric, c => c.Value);

    // ── CA-1 — Acceso al dashboard, medible por sesión ───────────────────────────

    [Fact]
    public void Deberia_ContarLaSesionQueLlegaAlAreaAutenticada_ComoDivisorDelKpi()
    {
        _sut.AppSessionStarted(Context());

        Counters()[TelemetryMetrics.AppSessionStarted].Should().Be(1);
    }

    [Fact]
    public void Deberia_SepararVisitasDeSesionesConUso()
    {
        // El KPI de la KB pregunta por **sesiones** que usan el panel. Sin esta separación, quien entra
        // ocho veces en una sesión pesaría como ocho sesiones y el porcentaje pasaría del 100 %.
        _sut.DashboardViewed(Context(), firstInSession: true);
        _sut.DashboardViewed(Context(), firstInSession: false);
        _sut.DashboardViewed(Context(), firstInSession: false);

        var contadores = Counters();
        contadores[TelemetryMetrics.DashboardViewed].Should().Be(3);
        contadores[TelemetryMetrics.DashboardSessionWithView].Should().Be(1);
    }

    // ── CA-2 — Recarga manual como señal separada ────────────────────────────────

    [Fact]
    public void Deberia_ContarLaRecargaManual_ApartaDeLaEntrada()
    {
        _sut.DashboardViewed(Context(), firstInSession: true);
        _sut.DashboardManualRefresh(Context());
        _sut.DashboardManualRefresh(Context());

        var contadores = Counters();
        contadores[TelemetryMetrics.DashboardManualRefresh].Should().Be(2);
        contadores[TelemetryMetrics.DashboardViewed].Should().Be(1);
    }

    // ── CA-3 — Cobertura de widgets ──────────────────────────────────────────────

    [Fact]
    public void Deberia_ContarComoCubierto_ElWidgetVacio()
    {
        // El KPI admite expresamente los estados vacío/incompleto: un Workspace sin cosechas todavía no
        // tiene el dashboard roto. Contarlo como fallo haría bajar la cobertura con cada alta nueva.
        _sut.DashboardWidgets(Context(), [
            new DashboardWidgetOutcome(DashboardWidgets.Summary, DashboardWidgets.StatusOk),
            new DashboardWidgetOutcome(DashboardWidgets.KgByPlot, DashboardWidgets.StatusEmpty),
        ]);

        var contadores = Counters();
        contadores[TelemetryMetrics.DashboardWidgetRendered].Should().Be(2);
        contadores.Should().NotContainKey(TelemetryMetrics.DashboardWidgetBlocked);
    }

    [Fact]
    public void Deberia_ContarComoBloqueado_SoloElWidgetConError()
    {
        _sut.DashboardWidgets(Context(), [
            new DashboardWidgetOutcome(DashboardWidgets.Summary, DashboardWidgets.StatusOk),
            new DashboardWidgetOutcome(DashboardWidgets.YieldEvolution, DashboardWidgets.StatusError),
        ]);

        var contadores = Counters();
        contadores[TelemetryMetrics.DashboardWidgetRendered].Should().Be(1);
        contadores[TelemetryMetrics.DashboardWidgetBlocked].Should().Be(1);
    }

    [Fact]
    public void Deberia_DesglosarPorWidgetYEstado_ParaSaberCualFalla()
    {
        _sut.DashboardWidgets(Context(), [
            new DashboardWidgetOutcome(DashboardWidgets.YieldEvolution, DashboardWidgets.StatusError),
        ]);

        Counters()[TelemetryMetrics.DashboardWidgetFor(
            DashboardWidgets.YieldEvolution, DashboardWidgets.StatusError)].Should().Be(1);
    }

    // ── Privacidad ───────────────────────────────────────────────────────────────

    [Fact]
    public void Deberia_EmitirSoloLasDimensionesDeclaradas_YNingunCampoMas()
    {
        _sut.AppSessionStarted(Context());

        // El endpoint es autenticado, así que el servidor **sabe** quién es: que no salga en la traza es
        // una decisión, no una casualidad. Este test la sostiene.
        _logger.LastDimensions().Should().BeEquivalentTo(
            "Event", "Timestamp", "SessionId", "Channel", "DeviceType");
    }

    [Fact]
    public void Deberia_RegistrarElDetalleDeLosWidgets_SinIdentificarANadie()
    {
        _sut.DashboardWidgets(Context(), [
            new DashboardWidgetOutcome(DashboardWidgets.Summary, DashboardWidgets.StatusOk),
        ]);

        _logger.Last()["Detail"].Should().Be("summary:ok");
        _logger.LastDimensions().Should().BeEquivalentTo(
            "Event", "Timestamp", "SessionId", "Channel", "DeviceType", "DetailKey", "Detail");
    }
}
