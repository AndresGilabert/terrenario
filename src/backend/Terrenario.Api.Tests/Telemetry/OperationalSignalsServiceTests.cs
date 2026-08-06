using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Terrenario.Api.Application.Ops;
using Terrenario.Api.Infrastructure.Telemetry;
using Terrenario.Api.Infrastructure.Telemetry.Alerts;

namespace Terrenario.Api.Tests.Telemetry;

/// <summary>
/// MVP-603 (CA-3) — El informe de la revisión operativa. Lo que importa aquí es que los KPI salgan bien
/// y que **no se inventen** cuando no hay datos.
/// </summary>
public class OperationalSignalsServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 6, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(Ahora.UtcDateTime);

    private readonly FakeTimeProvider _clock = new(Ahora);
    private readonly ITelemetryCounterStore _store = Substitute.For<ITelemetryCounterStore>();
    private readonly AlertStateStore _alerts = new();

    private OperationalSignalsService CreateSut(Dictionary<string, long> counters)
    {
        _store.GetRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([.. counters.Select(c => new TelemetryCounter(Hoy, c.Key, c.Value))]);

        return new OperationalSignalsService(_store, new RollingWindowMetrics(_clock), _alerts, _clock);
    }

    [Fact]
    public async Task Deberia_CalcularLosKpiDelEmbudo()
    {
        var report = await CreateSut(new Dictionary<string, long>
        {
            [TelemetryMetrics.LoginScreenViewed] = 200,
            [TelemetryMetrics.LoginSuccess] = 180,
            [TelemetryMetrics.LoginAbandonment] = 20,
            [TelemetryMetrics.LoginSuccessDurationMsSum] = 3_600_000,
            [TelemetryMetrics.LoginSuccessTimedCount] = 180,
        }).BuildAsync(CancellationToken.None);

        report.LoginFunnel7d.Conversion.Should().Be(0.9);
        report.LoginFunnel7d.AbandonmentRate.Should().Be(0.1);
        report.LoginFunnel7d.AverageSuccessMs.Should().Be(20_000);
    }

    [Fact]
    public async Task Deberia_CalcularLosKpiDeUsoDelProducto()
    {
        var report = await CreateSut(new Dictionary<string, long>
        {
            [TelemetryMetrics.AppSessionStarted] = 50,
            [TelemetryMetrics.DashboardSessionWithView] = 45,
            [TelemetryMetrics.DashboardManualRefresh] = 90,
            [TelemetryMetrics.DashboardWidgetRendered] = 196,
            [TelemetryMetrics.DashboardWidgetBlocked] = 4,
        }).BuildAsync(CancellationToken.None);

        report.ProductUsage7d.DashboardUsage.Should().Be(0.9);
        report.ProductUsage7d.ManualRefreshPerSession.Should().Be(2);
        report.ProductUsage7d.WidgetCoverage.Should().Be(0.98);
    }

    [Fact]
    public async Task Deberia_DevolverElMonitoreoDeNegocioMinimoDeLaKb()
    {
        var report = await CreateSut(new Dictionary<string, long>
        {
            [TelemetryMetrics.LoginSuccess] = 30,
            [TelemetryMetrics.ApiCreated] = 120,
            [TelemetryMetrics.ApiRequests] = 1000,
            [TelemetryMetrics.ApiRequests4xx] = 25,
        }).BuildAsync(CancellationToken.None);

        report.Business7d.Logins.Should().Be(30);
        report.Business7d.RecordsCreated.Should().Be(120);
        report.Business7d.VisibleErrorRate.Should().Be(0.025);
    }

    [Fact]
    public async Task Deberia_DejarLosKpiEnNulo_Cuando_NoHayNadaSobreLoQueCalcular()
    {
        // Cero sería inventarse una respuesta: «ninguna sesión abrió el panel» y «no hubo sesiones» no
        // son lo mismo, y con cero la revisión leería un problema donde solo hay una semana sin tráfico.
        var report = await CreateSut([]).BuildAsync(CancellationToken.None);

        report.LoginFunnel7d.Conversion.Should().BeNull();
        report.ProductUsage7d.DashboardUsage.Should().BeNull();
        report.ProductUsage7d.WidgetCoverage.Should().BeNull();
        report.Slo.ErrorRate7d.Should().BeNull();
        report.Slo.LatencyP95Ms7d.Should().BeNull();
    }

    [Fact]
    public async Task Deberia_ExponerMinutosObservados_YNoUnUptimeInventado()
    {
        // Un proceso caído no se observa a sí mismo, así que esto mide degradación, no disponibilidad.
        // El nombre del campo lo dice para que nadie lo lea como uptime.
        var report = await CreateSut(new Dictionary<string, long>
        {
            [TelemetryMetrics.HealthProbeOk] = 43_190,
            [TelemetryMetrics.HealthProbeFailed] = 10,
        }).BuildAsync(CancellationToken.None);

        report.Slo.HealthyMinutes30d.Should().Be(43_190);
        report.Slo.DegradedMinutes30d.Should().Be(10);
    }

    [Fact]
    public async Task Deberia_IncluirElEstadoDeLasAlertas()
    {
        _alerts.Apply(new AlertVerdict(AlertNames.ServiceDown, AlertSeverity.Critical, true, "caída"), Ahora);

        var report = await CreateSut([]).BuildAsync(CancellationToken.None);

        report.Alerts.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                Name = AlertNames.ServiceDown,
                IsFiring = true,
                FiringSince = (DateTimeOffset?)Ahora,
            });
    }
}
