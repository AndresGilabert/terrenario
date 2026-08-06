using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Telemetry;

public class LoginFlowTimingsTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));
    private readonly LoginFlowTimings _sut;

    public LoginFlowTimingsTests() => _sut = new LoginFlowTimings(_clock);

    [Fact]
    public void Deberia_DevolverLoQueDuroElIntento()
    {
        _sut.Start("flow");
        _clock.Advance(TimeSpan.FromSeconds(30));

        _sut.Complete("flow").Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Deberia_DevolverNulo_Cuando_NoConoceElInicio()
        => _sut.Complete("flow-de-otro-proceso").Should().BeNull();

    [Fact]
    public void Deberia_DevolverNuloYNoCero_Cuando_ElIntentoLlevaDemasiadoAbierto()
    {
        // La pestaña olvidada media mañana no mide «lo que se tarda en entrar»: mide el olvido.
        _sut.Start("flow");
        _clock.Advance(LoginFlowTimings.MaxAge + TimeSpan.FromMinutes(1));

        _sut.Complete("flow").Should().BeNull();
    }

    [Fact]
    public void Deberia_NoReiniciarElReloj_Cuando_LaPantallaSeAnunciaDosVeces()
    {
        _sut.Start("flow");
        _clock.Advance(TimeSpan.FromSeconds(10));
        _sut.Start("flow");
        _clock.Advance(TimeSpan.FromSeconds(5));

        _sut.Complete("flow").Should().Be(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void Deberia_OlvidarElIntento_Cuando_SeDescarta()
    {
        _sut.Start("flow");
        _sut.Discard("flow");

        _sut.Complete("flow").Should().BeNull();
    }

    [Fact]
    public void Deberia_ConsumirElIntento_Cuando_SeCompleta()
    {
        _sut.Start("flow");

        _sut.Complete("flow").Should().NotBeNull();
        _sut.Complete("flow").Should().BeNull();
    }
}
