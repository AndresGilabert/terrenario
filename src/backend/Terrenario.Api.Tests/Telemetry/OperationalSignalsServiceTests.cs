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
        => CreateSutWith([.. counters.Select(c => new TelemetryCounter(Hoy, c.Key, c.Value))]);

    private OperationalSignalsService CreateSutWith(TelemetryCounter[] counters)
    {
        _store.GetRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(counters);

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
        }).BuildAsync(null, CancellationToken.None);

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
            // MVP-706 — `dashboard.manual_refresh` sigue sembrado a proposito: el informe ya no debe
            // publicarlo aunque la tabla conserve su historico.
            [TelemetryMetrics.DashboardManualRefresh] = 90,
            [TelemetryMetrics.DashboardWidgetRendered] = 196,
            [TelemetryMetrics.DashboardWidgetBlocked] = 4,
        }).BuildAsync(null, CancellationToken.None);

        report.ProductUsage7d.DashboardUsage.Should().Be(0.9);
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
        }).BuildAsync(null, CancellationToken.None);

        report.Business7d.Logins.Should().Be(30);
        report.Business7d.RecordsCreated.Should().Be(120);
        report.Business7d.VisibleErrorRate.Should().Be(0.025);
    }

    [Fact]
    public async Task Deberia_DejarLosKpiEnNulo_Cuando_NoHayNadaSobreLoQueCalcular()
    {
        // Cero sería inventarse una respuesta: «ninguna sesión abrió el panel» y «no hubo sesiones» no
        // son lo mismo, y con cero la revisión leería un problema donde solo hay una semana sin tráfico.
        var report = await CreateSut([]).BuildAsync(null, CancellationToken.None);

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
        }).BuildAsync(null, CancellationToken.None);

        report.Slo.HealthyMinutes30d.Should().Be(43_190);
        report.Slo.DegradedMinutes30d.Should().Be(10);
    }

    // ── MVP-699 (`R-01`) — Serie diaria ──────────────────────────────────────────

    [Fact]
    public async Task Deberia_DevolverCuatroSemanasDeSerie_PorDefecto()
    {
        var report = await CreateSut([]).BuildAsync(null, CancellationToken.None);

        report.Daily.Should().HaveCount(OperationalSignalsService.DefaultDailyDays);
        report.Daily[^1].Date.Should().Be(Hoy, "el último día de la serie es hoy");
        report.Daily[0].Date.Should().Be(Hoy.AddDays(-(OperationalSignalsService.DefaultDailyDays - 1)));
    }

    [Fact]
    public async Task Deberia_PermitirComparadaSemanaContraSemana()
    {
        // Es el caso que motiva el hallazgo: sin serie, «82 % esta semana» no dice si mejora o empeora.
        var ayer = Hoy.AddDays(-1);
        var report = await CreateSutWith([
            new TelemetryCounter(ayer, TelemetryMetrics.LoginScreenViewed, 10),
            new TelemetryCounter(ayer, TelemetryMetrics.LoginSuccess, 9),
            new TelemetryCounter(Hoy, TelemetryMetrics.LoginScreenViewed, 10),
            new TelemetryCounter(Hoy, TelemetryMetrics.LoginSuccess, 5),
        ]).BuildAsync(14, CancellationToken.None);

        report.Daily.Should().HaveCount(14);
        report.Daily.Single(d => d.Date == ayer).LoginConversion.Should().Be(0.9);
        report.Daily.Single(d => d.Date == Hoy).LoginConversion.Should().Be(0.5);
    }

    [Fact]
    public async Task Deberia_EmitirLosDiasSinDatos_ConCeroYNulo()
    {
        // Omitirlos escondería el hueco. Un día sin tráfico no es un día con la conversión al 0 %, y
        // `healthy_minutes = 0` es justo lo que delata que ese día no había nadie observando.
        var report = await CreateSut([]).BuildAsync(3, CancellationToken.None);

        report.Daily.Should().HaveCount(3);
        report.Daily.Should().OnlyContain(d =>
            d.LoginScreenViewed == 0 && d.LoginConversion == null && d.HealthyMinutes == 0);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(100_000, OperationalSignalsService.MaxDailyDays)]
    public async Task Deberia_AcotarElRangoPedido(int pedidos, int esperados)
        // Pedir más que la retención solo devolvería días vacíos, y pedir cero no es una pregunta.
        => (await CreateSut([]).BuildAsync(pedidos, CancellationToken.None))
            .Daily.Should().HaveCount(esperados);

    [Fact]
    public async Task Deberia_NoMoverLasVentanasDeLosSlo_AlPedirOtroRango()
    {
        // Las ventanas de 7 y 30 días son parte de la definición del SLO: si el parámetro las moviera,
        // preguntar por 90 días cambiaría el objetivo contra el que se compara.
        var haceDiezDias = Hoy.AddDays(-10);
        var report = await CreateSutWith([
            new TelemetryCounter(haceDiezDias, TelemetryMetrics.ApiRequests, 1000),
            new TelemetryCounter(haceDiezDias, TelemetryMetrics.ApiRequests5xx, 500),
            new TelemetryCounter(Hoy, TelemetryMetrics.ApiRequests, 1000),
        ]).BuildAsync(90, CancellationToken.None);

        // El desastre de hace diez días entra en la serie, pero **no** en el SLO de 7 días.
        report.Slo.ErrorRate7d.Should().Be(0);
        report.Daily.Single(d => d.Date == haceDiezDias).ErrorRate.Should().Be(0.5);
    }

    [Fact]
    public async Task Deberia_LeerLaBaseUnaSolaVez_ParaTodoElInforme()
    {
        await CreateSut([]).BuildAsync(90, CancellationToken.None);

        await _store.Received(1).GetRangeAsync(
            Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_PublicarLoExcluidoDelSlo_ParaQueElRecorteSeVea()
    {
        // Excluir tráfico del divisor sin decirlo sería recortarlo a escondidas, y la revisión leería
        // «no hubo ese tráfico» donde en realidad hubo y no se contó.
        var report = await CreateSut(new Dictionary<string, long>
        {
            [TelemetryMetrics.ApiInternalRequests] = 1440,
            [TelemetryMetrics.ApiInternalRequests5xx] = 3,
        }).BuildAsync(null, CancellationToken.None);

        report.Slo.InternalRequests7d.Should().Be(1440);
        report.Slo.InternalErrors7d.Should().Be(3);
    }

    [Fact]
    public async Task Deberia_IncluirElEstadoDeLasAlertas()
    {
        _alerts.Apply(new AlertVerdict(AlertNames.ServiceDown, AlertSeverity.Critical, true, "caída"), Ahora);

        var report = await CreateSut([]).BuildAsync(null, CancellationToken.None);

        report.Alerts.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                Name = AlertNames.ServiceDown,
                IsFiring = true,
                FiringSince = (DateTimeOffset?)Ahora,
            });
    }
}
