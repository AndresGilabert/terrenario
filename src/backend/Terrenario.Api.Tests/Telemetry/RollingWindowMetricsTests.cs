using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Telemetry;

/// <summary>
/// MVP-603 — La ventana corta. Existe porque las alertas de la KB están definidas sobre 30 minutos y un
/// contador diario no puede responder a eso: a las 23:00 lleva acumuladas veintitrés horas y una caída
/// de media hora queda diluida.
/// </summary>
public class RollingWindowMetricsTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));
    private readonly RollingWindowMetrics _sut;

    public RollingWindowMetricsTests() => _sut = new RollingWindowMetrics(_clock);

    [Fact]
    public void Deberia_SumarLoOcurridoDentroDeLaVentana()
    {
        _sut.Add("api.requests", 3);
        _clock.Advance(TimeSpan.FromMinutes(5));
        _sut.Add("api.requests", 2);

        _sut.Snapshot(TimeSpan.FromMinutes(30))["api.requests"].Should().Be(5);
    }

    [Fact]
    public void Deberia_DejarFueraLoAnteriorALaVentana()
    {
        _sut.Add("api.requests", 100);
        _clock.Advance(TimeSpan.FromMinutes(31));
        _sut.Add("api.requests", 1);

        _sut.Snapshot(TimeSpan.FromMinutes(30))["api.requests"].Should().Be(1);
    }

    [Fact]
    public void Deberia_ConservarLoJustoEnElBordeDeLaVentana()
    {
        _sut.Add("api.requests", 7);
        _clock.Advance(TimeSpan.FromMinutes(29));

        _sut.Snapshot(TimeSpan.FromMinutes(30))["api.requests"].Should().Be(7);
    }

    [Fact]
    public void Deberia_OlvidarLoQueSaleDeLaRetencion()
    {
        _sut.Add("api.requests", 100);
        _clock.Advance(RollingWindowMetrics.Retention + TimeSpan.FromMinutes(5));
        _sut.Add("api.requests", 1);   // dispara la poda al cambiar de minuto

        _sut.Snapshot(RollingWindowMetrics.Retention * 2)["api.requests"].Should().Be(1);
    }

    [Fact]
    public void Deberia_DevolverVacio_Cuando_NoHaOcurridoNada()
        => _sut.Snapshot(TimeSpan.FromMinutes(30)).Should().BeEmpty();
}

public class CompositeTelemetryCountersTests
{
    [Fact]
    public void Deberia_LlevarCadaMedidaALasDosSalidas()
    {
        // La serie diaria es la que se conserva; la ventana corta es la que deciden las alertas. Si una
        // de las dos se quedara sin la medida, o se perdería el histórico o dejarían de saltar alertas.
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));
        var daily = new TelemetryCounterAccumulator(clock);
        var window = new RollingWindowMetrics(clock);

        new CompositeTelemetryCounters(daily, window).Add("api.requests", 4);

        daily.Drain().Single().Value.Should().Be(4);
        window.Snapshot(TimeSpan.FromMinutes(30))["api.requests"].Should().Be(4);
    }
}
