using FluentAssertions;
using Terrenario.Api.Infrastructure.Telemetry.Alerts;

namespace Terrenario.Api.Tests.Telemetry;

public class AlertStateStoreTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 6, 10, 0, 0, TimeSpan.Zero);

    private readonly AlertStateStore _sut = new();

    private static AlertVerdict Verdict(bool firing, string name = AlertNames.HighErrorRate) =>
        new(name, AlertSeverity.Critical, firing, "detalle");

    [Fact]
    public void Deberia_AvisarSoloEnLaTransicion_YNoEnCadaPasada()
    {
        // Una degradación de dos horas mandaría ciento veinte avisos idénticos, y el canal de
        // incidentes dejaría de leerse justo cuando hace falta.
        _sut.Apply(Verdict(true), Ahora).Should().NotBeNull();
        _sut.Apply(Verdict(true), Ahora.AddMinutes(1)).Should().BeNull();
        _sut.Apply(Verdict(true), Ahora.AddMinutes(2)).Should().BeNull();
    }

    [Fact]
    public void Deberia_AvisarTambienDeLaResolucion_ConLoQueDuro()
    {
        _sut.Apply(Verdict(true), Ahora);

        var transicion = _sut.Apply(Verdict(false), Ahora.AddMinutes(42));

        transicion.Should().NotBeNull();
        transicion!.Started.Should().BeFalse();
        transicion.Duration.Should().Be(TimeSpan.FromMinutes(42));
    }

    [Fact]
    public void Deberia_NoAvisar_Cuando_LaAlertaNuncaHabiaSaltado()
        => _sut.Apply(Verdict(false), Ahora).Should().BeNull();

    [Fact]
    public void Deberia_ConservarElInicio_MientrasSigueDisparada()
    {
        _sut.Apply(Verdict(true), Ahora);
        _sut.Apply(Verdict(true), Ahora.AddMinutes(10));

        _sut.Current().Single().FiringSince.Should().Be(Ahora);
    }

    [Fact]
    public void Deberia_ReiniciarElInicio_Cuando_VuelveASaltarTrasResolverse()
    {
        _sut.Apply(Verdict(true), Ahora);
        _sut.Apply(Verdict(false), Ahora.AddMinutes(5));
        _sut.Apply(Verdict(true), Ahora.AddMinutes(20));

        _sut.Current().Single().FiringSince.Should().Be(Ahora.AddMinutes(20));
    }

    [Fact]
    public void Deberia_PonerDelanteLoDisparado_ParaQueLaRevisionLoVeaPrimero()
    {
        _sut.Apply(Verdict(false, AlertNames.HighErrorRate), Ahora);
        _sut.Apply(Verdict(true, AlertNames.ServiceDown), Ahora);

        _sut.Current().First().Name.Should().Be(AlertNames.ServiceDown);
        _sut.Firing().Should().ContainSingle().Which.Name.Should().Be(AlertNames.ServiceDown);
    }
}
