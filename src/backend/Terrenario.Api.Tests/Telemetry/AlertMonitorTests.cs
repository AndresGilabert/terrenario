using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Telemetry;
using Terrenario.Api.Infrastructure.Telemetry.Alerts;

namespace Terrenario.Api.Tests.Telemetry;

/// <summary>
/// MVP-603 — La vigilancia completa: sonda de salud, evaluación y aviso solo en la transición.
///
/// La sonda se ejercita contra un <see cref="TerrenarioDbContext"/> **sin proveedor alcanzable**, que es
/// exactamente el fallo que `ServiceDown` debe detectar: el proceso vive y la base de datos no está.
/// </summary>
public class AlertMonitorTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 6, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(Ahora);
    private readonly IAlertNotifier _notifier = Substitute.For<IAlertNotifier>();
    private readonly AlertStateStore _states = new();
    private readonly RollingWindowMetrics _window;
    private readonly TelemetryCounterAccumulator _counters;

    public AlertMonitorTests()
    {
        _window = new RollingWindowMetrics(_clock);
        _counters = new TelemetryCounterAccumulator(_clock);
    }

    private AlertMonitor CreateSut()
    {
        // Cadena de conexión válida en forma pero apuntando a un puerto donde no hay nadie: el contexto
        // se construye y `CanConnectAsync` falla, que es el escenario a probar.
        var services = new ServiceCollection();
        services.AddScoped(_ => new TerrenarioDbContext(
            new DbContextOptionsBuilder<TerrenarioDbContext>()
                .UseNpgsql("Host=localhost;Port=1;Database=nadie;Username=nadie;Password=nadie;Timeout=1")
                .Options));
        services.AddScoped(sp => new HealthProbe(
            sp.GetRequiredService<TerrenarioDbContext>(), NullLogger<HealthProbe>.Instance));
        services.AddScoped(_ => _notifier);

        return new AlertMonitor(
            services.BuildServiceProvider(), _window, _counters, _states,
            Options.Create(new OpsOptions()), _clock, NullLogger<AlertMonitor>.Instance);
    }

    [Fact]
    public async Task Deberia_DispararServiceDown_TrasDosSondasFallidas_YNoAntes()
    {
        var sut = CreateSut();

        await sut.RunOnceAsync(CancellationToken.None);
        _states.Firing().Should().BeEmpty();

        await sut.RunOnceAsync(CancellationToken.None);
        _states.Firing().Should().ContainSingle().Which.Name.Should().Be(AlertNames.ServiceDown);
    }

    [Fact]
    public async Task Deberia_AvisarUnaSolaVez_Aunque_LaCaidaDure()
    {
        var sut = CreateSut();

        for (var pasada = 0; pasada < 5; pasada++)
            await sut.RunOnceAsync(CancellationToken.None);

        await _notifier.Received(1).NotifyFiringAsync(
            Arg.Is<AlertVerdict>(v => v.Name == AlertNames.ServiceDown), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_ContarLaAlertaDisparada_ParaPoderMirarAtras()
    {
        var sut = CreateSut();

        await sut.RunOnceAsync(CancellationToken.None);
        await sut.RunOnceAsync(CancellationToken.None);

        _counters.Drain().Should().Contain(c =>
            c.Metric == TelemetryMetrics.AlertFiredFor(AlertNames.ServiceDown) && c.Value == 1);
    }

    [Fact]
    public async Task Deberia_ContarLaSondaFallida_EnCadaPasada()
    {
        await CreateSut().RunOnceAsync(CancellationToken.None);

        _counters.Drain().Should().Contain(c =>
            c.Metric == TelemetryMetrics.HealthProbeFailed && c.Value == 1);
    }

    [Fact]
    public async Task Deberia_DispararLaAlertaDelEmbudo_Cuando_LaVentanaLoJustifica()
    {
        // La ventana la alimenta el mismo camino que en producción: contadores de MVP-601.
        for (var i = 0; i < 20; i++) _window.Add(TelemetryMetrics.LoginScreenViewed, 1);
        for (var i = 0; i < 10; i++) _window.Add(TelemetryMetrics.LoginAbandonment, 1);

        await CreateSut().RunOnceAsync(CancellationToken.None);

        _states.Firing().Select(a => a.Name).Should().Contain(AlertNames.LoginAbandonmentSpike);
    }
}
