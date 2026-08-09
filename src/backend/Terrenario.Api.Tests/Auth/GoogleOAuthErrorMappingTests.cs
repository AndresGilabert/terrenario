using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Infrastructure.Auth;
using Terrenario.Api.Infrastructure.Telemetry;
using Terrenario.Api.Infrastructure.Telemetry.Alerts;

namespace Terrenario.Api.Tests.Auth;

/// <summary>
/// MVP-713 (`P-079`, CA-5) — Regresión del mapeo de códigos de OAuth 2.0 (RFC 6749 §5.2).
///
/// El vocabulario es cerrado y lo emite un tercero, así que la tabla no se deduce del código que la
/// usa: si alguien reclasificara `invalid_grant` como fallo del servidor, la única forma de enterarse
/// antes de que volviera a saltar una alerta crítica en producción es este test.
///
/// Cubre además CA-3 y CA-4: que la clasificación se traduzca en un contador 4xx —no 5xx— y que el
/// escenario exacto que disparó la alerta en `MVP-699` ya no la dispare.
/// </summary>
public class GoogleOAuthErrorMappingTests
{
    private static readonly DateTimeOffset Momento = new(2026, 8, 8, 10, 0, 0, TimeSpan.Zero);
    private const string CallbackPath = "/api/v1/auth/google/callback";

    // ── CA-5 · El vocabulario cerrado ────────────────────────────────────────────

    [Theory]
    // De cliente: recargar la pantalla de vuelta de Google basta para provocarlo.
    [InlineData(GoogleOAuthErrors.InvalidGrant, ErrorCodes.AuthGoogleCodeInvalid, StatusCodes.Status401Unauthorized)]
    [InlineData(GoogleOAuthErrors.InvalidRequest, ErrorCodes.AuthGoogleRequestInvalid, StatusCodes.Status400BadRequest)]
    // De servidor: configuración nuestra. Que el usuario no pueda hacer nada al respecto es
    // precisamente el motivo por el que tiene que seguir contando como fallo propio.
    [InlineData(GoogleOAuthErrors.InvalidClient, ErrorCodes.AuthGoogleExchangeFailed, StatusCodes.Status500InternalServerError)]
    [InlineData(GoogleOAuthErrors.UnauthorizedClient, ErrorCodes.AuthGoogleExchangeFailed, StatusCodes.Status500InternalServerError)]
    public void Deberia_ClasificarElVocabularioCerradoDeOAuth(
        string oauthError, string esperado, int estadoEsperado)
    {
        var errorCode = GoogleOAuthErrors.ToErrorCode(oauthError);

        errorCode.Should().Be(esperado);
        GoogleOidcErrorMapper.StatusFor(errorCode).Should().Be(estadoEsperado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(GoogleOAuthErrors.Unknown)]
    [InlineData("temporarily_unavailable")]
    [InlineData("INVALID_GRANT")]
    public void Deberia_TratarComoFalloPropio_LoQueNoEstaClasificado(string? oauthError)
    {
        // El defecto va hacia el 500 a propósito. Clasificar por descarte convertiría una caída de
        // Google —o una respuesta que no sabemos leer— en un 4xx silencioso, que es justo la avería
        // que las alertas tienen que ver. Incluida la diferencia de mayúsculas: el vocabulario de la
        // RFC va en minúsculas, y aceptar variantes sería inventarse el contrato del proveedor.
        var errorCode = GoogleOAuthErrors.ToErrorCode(oauthError);

        errorCode.Should().Be(ErrorCodes.AuthGoogleExchangeFailed);
        GoogleOidcErrorMapper.IsServerFault(errorCode).Should().BeTrue();
    }

    [Fact]
    public void Deberia_MantenerEn401_ElIdTokenQueNoValida()
    {
        // No es parte del vocabulario de OAuth —el intercambio fue bien y falla la validación del
        // `id_token`— pero comparte destino: la credencial presentada no sirve. Estaba en 401 antes de
        // MVP-713 y la historia no lo mueve.
        GoogleOidcErrorMapper.StatusFor(ErrorCodes.AuthGoogleTokenInvalid)
            .Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Theory]
    [InlineData(ErrorCodes.AuthGoogleCodeInvalid)]
    [InlineData(ErrorCodes.AuthGoogleRequestInvalid)]
    [InlineData(ErrorCodes.AuthGoogleTokenInvalid)]
    [InlineData(ErrorCodes.AuthGoogleExchangeFailed)]
    public void Deberia_DevolverElMismoCodigo_EnElCuerpoDeLaRespuesta(string errorCode)
    {
        // El cliente elige el mensaje por el código, así que responder con otro dejaría a la pantalla
        // de callback sin poder explicar lo que ha pasado (CA-1).
        GoogleOidcErrorMapper.ToApiError(errorCode).Code.Should().Be(errorCode);
    }

    // ── CA-3 · Los casos de cliente no mueven el SLO ─────────────────────────────

    [Theory]
    [InlineData(GoogleOAuthErrors.InvalidGrant)]
    [InlineData(GoogleOAuthErrors.InvalidRequest)]
    public async Task Deberia_NoIncrementarElNumeradorDelSlo_Cuando_ElErrorEsDelCliente(string oauthError)
    {
        var contadores = await MedirCallbackAsync(oauthError);

        contadores.Should().NotContainKey(TelemetryMetrics.ApiRequests5xx);
        contadores[TelemetryMetrics.ApiRequests4xx].Should().Be(1);
        // Sigue en el divisor: la petición se sirvió, y la disponibilidad y la latencia la cuentan.
        contadores[TelemetryMetrics.ApiRequests].Should().Be(1);
    }

    [Fact]
    public async Task Deberia_SeguirContandoComo5xx_ElFalloDeConfiguracion()
    {
        // La otra mitad de la historia: si `invalid_client` dejara de contar, el arreglo habría
        // cambiado una alerta ruidosa por una alerta ciega.
        var contadores = await MedirCallbackAsync(GoogleOAuthErrors.InvalidClient);

        contadores[TelemetryMetrics.ApiRequests5xx].Should().Be(1);
    }

    // ── CA-4 · El escenario que disparó la alerta en MVP-699 ─────────────────────

    [Fact]
    public async Task Deberia_NoDispararHighErrorRate_Cuando_SeRepiteElEscenarioDeLaRevision()
    {
        // Medido en `MVP-699`: un solo 500 de este tipo sobre 70 peticiones dio 1,43 % —por encima del
        // umbral del 1 %— y disparó `HighErrorRate`, que es crítica, con envío de correo real.
        var veredicto = await EvaluarErrorRateAsync(GoogleOAuthErrors.InvalidGrant);

        veredicto.IsFiring.Should().BeFalse();
        veredicto.Detail.Should().Contain("70 peticiones");
    }

    [Fact]
    public async Task Deberia_SeguirDisparandoHighErrorRate_Cuando_ElFalloEsDelServidor()
    {
        // El mismo experimento con `invalid_client`: la alerta tiene que seguir sirviendo.
        (await EvaluarErrorRateAsync(GoogleOAuthErrors.InvalidClient)).IsFiring.Should().BeTrue();
    }

    /// <summary>
    /// Reproduce la ventana de la revisión —69 peticiones servidas más un intercambio de código que
    /// falla— y devuelve el veredicto de <c>HighErrorRate</c>.
    /// </summary>
    private static async Task<AlertVerdict> EvaluarErrorRateAsync(string oauthError)
    {
        var contadores = new TelemetryCounterAccumulator(new FakeTimeProvider(Momento));

        for (var i = 0; i < 69; i++)
            await MedirAsync(contadores, "/api/v1/diary", StatusCodes.Status200OK);

        await MedirAsync(contadores, CallbackPath, EstadoDe(oauthError), "POST");

        var ventana = contadores.Drain().ToDictionary(c => c.Metric, c => c.Value);

        return AlertEvaluator.Evaluate(ventana, consecutiveFailedProbes: 0)
            .Single(v => v.Name == AlertNames.HighErrorRate);
    }

    /// <summary>Estado HTTP con el que el callback responde a un error de OAuth concreto.</summary>
    private static int EstadoDe(string oauthError)
        => GoogleOidcErrorMapper.StatusFor(GoogleOAuthErrors.ToErrorCode(oauthError));

    /// <summary>
    /// Pasa la respuesta del callback por la misma instrumentación que alimenta el SLO
    /// (<see cref="RequestMetricsMiddleware"/>) y devuelve lo que quedó contado.
    /// </summary>
    private static async Task<Dictionary<string, long>> MedirCallbackAsync(string oauthError)
    {
        var contadores = new TelemetryCounterAccumulator(new FakeTimeProvider(Momento));

        await MedirAsync(contadores, CallbackPath, EstadoDe(oauthError), "POST");

        return contadores.Drain().ToDictionary(c => c.Metric, c => c.Value);
    }

    private static async Task MedirAsync(
        ITelemetryCounters counters, string path, int statusCode, string method = "GET")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;

        var middleware = new RequestMetricsMiddleware(ctx =>
        {
            ctx.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, counters);
    }
}
