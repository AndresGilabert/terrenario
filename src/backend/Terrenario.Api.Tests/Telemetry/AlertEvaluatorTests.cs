using FluentAssertions;
using Terrenario.Api.Infrastructure.Telemetry;
using Terrenario.Api.Infrastructure.Telemetry.Alerts;

namespace Terrenario.Api.Tests.Telemetry;

/// <summary>
/// MVP-603 (CA-1/CA-2) — Las cinco alertas de la KB, evaluadas sobre una ventana.
///
/// Es una función pura, así que aquí se prueban los umbrales de verdad —no que «algo se dispara»—
/// incluidos los dos casos que separan una alerta útil de una que se acaba ignorando: el volumen
/// mínimo y el borde exacto del umbral.
/// </summary>
public class AlertEvaluatorTests
{
    private static AlertVerdict Evaluate(string name, Dictionary<string, long> window, int failedProbes = 0)
        => AlertEvaluator.Evaluate(window, failedProbes).Single(v => v.Name == name);

    private static Dictionary<string, long> Requests(long total, long errors5xx = 0) => new()
    {
        [TelemetryMetrics.ApiRequests] = total,
        [TelemetryMetrics.ApiRequests5xx] = errors5xx,
    };

    private static Dictionary<string, long> Logins(long screens, long success = 0, long abandonment = 0) => new()
    {
        [TelemetryMetrics.LoginScreenViewed] = screens,
        [TelemetryMetrics.LoginSuccess] = success,
        [TelemetryMetrics.LoginAbandonment] = abandonment,
    };

    // ── ServiceDown ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]   // «más de 1 minuto»: un solo fallo todavía no lo es
    [InlineData(2, true)]
    public void ServiceDown_SeDispara_TrasDosSondasFallidasSeguidas(int fallos, bool esperado)
        => Evaluate(AlertNames.ServiceDown, [], fallos).IsFiring.Should().Be(esperado);

    // ── HighErrorRate ────────────────────────────────────────────────────────────

    [Fact]
    public void HighErrorRate_NoSeDispara_SinVolumenSuficiente()
    {
        // Una madrugada con tres peticiones y un 500 daría un 33 % de error. Alertar ahí es alertar por
        // nada, y una alerta que salta sin motivo se acaba ignorando también cuando el motivo es real.
        var verdict = Evaluate(AlertNames.HighErrorRate, Requests(total: 3, errors5xx: 1));

        verdict.IsFiring.Should().BeFalse();
        verdict.Detail.Should().Contain("volumen");
    }

    [Fact]
    public void HighErrorRate_SeDispara_PorEncimaDelUnoPorCiento()
        => Evaluate(AlertNames.HighErrorRate, Requests(total: 1000, errors5xx: 11))
            .IsFiring.Should().BeTrue();

    [Fact]
    public void HighErrorRate_NoSeDispara_JustoEnElUmbral()
        // El umbral de la KB es «> 1 %», no «>= 1 %».
        => Evaluate(AlertNames.HighErrorRate, Requests(total: 1000, errors5xx: 10))
            .IsFiring.Should().BeFalse();

    [Fact]
    public void HighErrorRate_EsCritica_ComoDiceLaKb()
        => Evaluate(AlertNames.HighErrorRate, Requests(100)).Severity.Should().Be(AlertSeverity.Critical);

    // ── HighLatency ──────────────────────────────────────────────────────────────

    [Fact]
    public void HighLatency_SeDispara_Cuando_ElP95SuperaMedioSegundo()
    {
        // 100 muestras: 94 rápidas y 6 lentas dejan el percentil 95 en el cubo de 1000 ms.
        var window = Requests(100);
        window[TelemetryMetrics.LatencyBucket(50)] = 94;
        window[TelemetryMetrics.LatencyBucket(1000)] = 6;

        Evaluate(AlertNames.HighLatency, window).IsFiring.Should().BeTrue();
    }

    [Fact]
    public void HighLatency_NoSeDispara_Cuando_LaColaEsPequena()
    {
        // Con 4 lentas de 100, el percentil 95 sigue cayendo en el cubo rápido.
        var window = Requests(100);
        window[TelemetryMetrics.LatencyBucket(50)] = 96;
        window[TelemetryMetrics.LatencyBucket(1000)] = 4;

        Evaluate(AlertNames.HighLatency, window).IsFiring.Should().BeFalse();
    }

    [Fact]
    public void HighLatency_EsUnAviso_YNoUnaCritica_ComoDiceLaKb()
        => Evaluate(AlertNames.HighLatency, Requests(100)).Severity.Should().Be(AlertSeverity.Warning);

    // ── Embudo de login ──────────────────────────────────────────────────────────

    [Fact]
    public void LoginAbandonmentSpike_SeDispara_PorEncimaDelVeinticincoPorCiento()
        => Evaluate(AlertNames.LoginAbandonmentSpike, Logins(screens: 100, abandonment: 26))
            .IsFiring.Should().BeTrue();

    [Fact]
    public void LoginAbandonmentSpike_NoSeDispara_JustoEnElUmbral()
        => Evaluate(AlertNames.LoginAbandonmentSpike, Logins(screens: 100, abandonment: 25))
            .IsFiring.Should().BeFalse();

    [Fact]
    public void LoginSuccessDrop_SeDispara_PorDebajoDelSetentaPorCiento()
        => Evaluate(AlertNames.LoginSuccessDrop, Logins(screens: 100, success: 69))
            .IsFiring.Should().BeTrue();

    [Fact]
    public void LoginSuccessDrop_NoSeDispara_JustoEnElUmbral()
        => Evaluate(AlertNames.LoginSuccessDrop, Logins(screens: 100, success: 70))
            .IsFiring.Should().BeFalse();

    [Theory]
    [InlineData(AlertNames.LoginAbandonmentSpike)]
    [InlineData(AlertNames.LoginSuccessDrop)]
    public void ElEmbudo_NoSeJuzga_SinPantallasSuficientes(string alerta)
    {
        // Con 9 pantallas y ninguna entrada, la conversión es 0 % y las dos alertas del embudo
        // saltarían. En un producto con pocos usuarios, eso sería todas las noches.
        var verdict = Evaluate(alerta, Logins(screens: 9, success: 0, abandonment: 9));

        verdict.IsFiring.Should().BeFalse();
        verdict.Detail.Should().Contain("volumen");
    }

    // ── P95 sobre el histograma ──────────────────────────────────────────────────

    [Fact]
    public void P95_EsNulo_SinMuestras()
        // Nulo y no cero: cero milisegundos sería una latencia excelente inventada.
        => AlertEvaluator.LatencyP95Ms(new Dictionary<string, long>()).Should().BeNull();

    [Fact]
    public void P95_DevuelveElCorteSuperiorDelCuboDondeCaeElPercentil()
    {
        // 100 muestras: la nonagésima quinta cae ya dentro del cubo de 200 ms (90 + 5 = 95), así que el
        // P95 es «por debajo de 200 ms». Las 5 lentas quedan por encima del percentil, que es justo lo
        // que un percentil sirve para ignorar.
        var window = new Dictionary<string, long>
        {
            [TelemetryMetrics.LatencyBucket(50)] = 90,
            [TelemetryMetrics.LatencyBucket(200)] = 5,
            [TelemetryMetrics.LatencyBucket(2000)] = 5,
        };

        AlertEvaluator.LatencyP95Ms(window).Should().Be(200);
    }

    [Fact]
    public void P95_SubeDeCubo_Cuando_LaColaLentaPesaMasDelCincoPorCiento()
    {
        var window = new Dictionary<string, long>
        {
            [TelemetryMetrics.LatencyBucket(50)] = 90,
            [TelemetryMetrics.LatencyBucket(2000)] = 10,
        };

        AlertEvaluator.LatencyP95Ms(window).Should().Be(2000);
    }

    [Fact]
    public void P95_SeLeeComoCota_YElUltimoCuboNoTieneTecho()
    {
        AlertEvaluator.FormatBucket(300).Should().Be("por debajo de 300 ms");
        AlertEvaluator.FormatBucket(int.MaxValue).Should().Be("por encima de 2000 ms");
    }

    [Fact]
    public void Evaluate_DevuelveLasCincoAlertasDeLaKb_Siempre()
        // También cuando no hay nada que contar: la revisión operativa debe poder ver que una alerta
        // existe y está tranquila, no que ha desaparecido.
        => AlertEvaluator.Evaluate(new Dictionary<string, long>(), 0)
            .Select(v => v.Name).Should().BeEquivalentTo(
                AlertNames.ServiceDown, AlertNames.HighErrorRate, AlertNames.HighLatency,
                AlertNames.LoginAbandonmentSpike, AlertNames.LoginSuccessDrop);
}
