using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Telemetry;

/// <summary>
/// MKT-106 — Mismo contrato que `LoginFlowTimings`: en memoria, solo mientras dura el intento, con
/// caducidad por edad.
/// </summary>
public class LoginFlowEntriesTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero));
    private LoginFlowEntries CreateSut() => new(_clock);

    [Fact]
    public void Deberia_DevolverLaClasificacion_AlCerrarElIntento()
    {
        var sut = CreateSut();
        sut.Start("flow01", "landing.home");

        sut.Complete("flow01").Should().Be("landing.home");
    }

    [Fact]
    public void Deberia_DevolverNull_Cuando_NoSeConociaElIntento() =>
        CreateSut().Complete("desconocido").Should().BeNull();

    [Fact]
    public void Deberia_DevolverNull_Cuando_YaSeCompleto()
    {
        var sut = CreateSut();
        sut.Start("flow01", "landing.home");
        sut.Complete("flow01");

        sut.Complete("flow01").Should().BeNull();
    }

    [Fact]
    public void Deberia_DevolverNull_Cuando_SeDescarto()
    {
        var sut = CreateSut();
        sut.Start("flow01", "landing.home");
        sut.Discard("flow01");

        sut.Complete("flow01").Should().BeNull();
    }

    [Fact]
    public void Deberia_DevolverNull_Cuando_ElIntentoCaduco()
    {
        var sut = CreateSut();
        sut.Start("flow01", "landing.home");

        _clock.Advance(LoginFlowTimings.MaxAge + TimeSpan.FromMinutes(1));

        sut.Complete("flow01").Should().BeNull();
    }
}
