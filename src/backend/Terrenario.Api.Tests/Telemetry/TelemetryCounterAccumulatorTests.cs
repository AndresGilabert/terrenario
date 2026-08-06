using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Telemetry;

public class TelemetryCounterAccumulatorTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 6, 23, 30, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(Ahora);
    private readonly TelemetryCounterAccumulator _sut;

    public TelemetryCounterAccumulatorTests() => _sut = new TelemetryCounterAccumulator(_clock);

    [Fact]
    public void Deberia_AcumularPorMetrica()
    {
        _sut.Add("login.success");
        _sut.Add("login.success", 4);
        _sut.Add("login.abandonment");

        _sut.Drain().Should().BeEquivalentTo(new[]
        {
            new TelemetryCounter(DateOnly.FromDateTime(Ahora.UtcDateTime), "login.success", 5),
            new TelemetryCounter(DateOnly.FromDateTime(Ahora.UtcDateTime), "login.abandonment", 1),
        });
    }

    [Fact]
    public void Deberia_SepararPorDia_Cuando_LaVentanaCruzaLaMedianoche()
    {
        // Lo acumulado antes de medianoche es del día 6, aunque se vuelque el 7. Fechar por el momento
        // del volcado desplazaría medio día de embudo al día siguiente.
        _sut.Add("login.success");
        _clock.Advance(TimeSpan.FromHours(1));
        _sut.Add("login.success");

        _sut.Drain().Should().BeEquivalentTo(new[]
        {
            new TelemetryCounter(new DateOnly(2026, 8, 6), "login.success", 1),
            new TelemetryCounter(new DateOnly(2026, 8, 7), "login.success", 1),
        });
    }

    [Fact]
    public void Deberia_VaciarseAlDrenar()
    {
        _sut.Add("login.success");

        _sut.Drain().Should().HaveCount(1);
        _sut.Drain().Should().BeEmpty();
    }

    [Fact]
    public void Deberia_SumarLoDevueltoALoLlegadoEntretanto_Cuando_SeRestaura()
    {
        _sut.Add("login.success", 3);
        var drenado = _sut.Drain();

        _sut.Add("login.success", 2);   // llega mientras la escritura estaba en curso
        _sut.Restore(drenado);          // …y la escritura falló

        _sut.Drain().Single().Value.Should().Be(5);
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("login.success", 0)]
    public void Deberia_IgnorarLaSuma_Cuando_NoAportaNada(string metric, long delta)
    {
        _sut.Add(metric, delta);

        _sut.Drain().Should().BeEmpty();
    }
}
