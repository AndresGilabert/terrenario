using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Terrenario.Api.Application.Feedback;

namespace Terrenario.Api.Tests.Feedback;

/// <summary>
/// MVP-711 (CA-6) — El límite anti-abuso. Lo que se prueba es que el cupo <b>se agota</b>, que
/// <b>se recupera</b> y que es <b>por cuenta</b>: sin las tres, el límite sería un adorno.
/// </summary>
public class FeedbackRateLimiterTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 8, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(Ahora);
    private readonly Guid _antonio = Guid.NewGuid();
    private readonly Guid _lucia = Guid.NewGuid();

    private FeedbackRateLimiter CreateSut() => new(_clock);

    [Fact]
    public void Deberia_DejarPasar_HastaAgotarElCupo()
    {
        var sut = CreateSut();

        for (var envio = 0; envio < FeedbackRateLimiter.MaxPerWindow; envio++)
        {
            sut.IsAllowed(_antonio, out _).Should().BeTrue();
            sut.Register(_antonio);
        }

        sut.IsAllowed(_antonio, out var retryAfter).Should().BeFalse();

        // Negarse sin decir cuánto hay que esperar deja a quien reporta probando a ciegas.
        retryAfter.Should().BePositive().And.BeLessThanOrEqualTo(FeedbackRateLimiter.Window);
    }

    [Fact]
    public void Deberia_LiberarCupo_Cuando_ElEnvioMasAntiguoSaleDeLaVentana()
    {
        var sut = CreateSut();

        for (var envio = 0; envio < FeedbackRateLimiter.MaxPerWindow; envio++) sut.Register(_antonio);
        sut.IsAllowed(_antonio, out _).Should().BeFalse();

        // Ventana deslizante: al cumplirse la hora del primero, vuelve a haber sitio.
        _clock.Advance(FeedbackRateLimiter.Window);

        sut.IsAllowed(_antonio, out _).Should().BeTrue();
    }

    [Fact]
    public void Deberia_ContarPorCuenta_YNoGlobalmente()
    {
        var sut = CreateSut();

        for (var envio = 0; envio < FeedbackRateLimiter.MaxPerWindow; envio++) sut.Register(_antonio);

        // Un límite global convertiría a cualquier usuario en capaz de callar a todos los demás.
        sut.IsAllowed(_antonio, out _).Should().BeFalse();
        sut.IsAllowed(_lucia, out _).Should().BeTrue();
    }

    [Fact]
    public void Deberia_NoRetenerCuentas_Cuando_SusEnviosCaducan()
    {
        // El diccionario es de proceso y vive lo que viva la instancia: si no se limpiara, tendría
        // una entrada por cada cuenta que reportó alguna vez. No se puede observar desde fuera, así
        // que lo que se afirma es lo observable: pasada la ventana, todo el mundo vuelve a cero.
        var sut = CreateSut();

        sut.Register(_antonio);
        _clock.Advance(FeedbackRateLimiter.Window + TimeSpan.FromMinutes(1));
        sut.Register(_lucia);

        sut.IsAllowed(_antonio, out _).Should().BeTrue();
        sut.IsAllowed(_lucia, out _).Should().BeTrue();
    }
}
