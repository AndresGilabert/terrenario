using System.Diagnostics;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Common.Http;

/// <summary>
/// MVP-603 — Mide cada petición servida: cuántas, cuántas fallan y cuánto tardan. Es la materia prima
/// de los tres SLO de <c>observabilidad.md</c> (disponibilidad, tasa de error 5xx y latencia P95).
///
/// Va **muy arriba** en la tubería, junto a <see cref="RequestIdMiddleware"/>, para que el tiempo
/// medido sea el que percibe quien llama y no solo el del controlador, y para que una respuesta de
/// error generada por otro middleware también se cuente.
///
/// Se cuentan también las **altas** (POST con 201) por recurso: es el `registros_creados_semana` del
/// monitoreo de negocio mínimo, y sale de aquí sin tocar ni un manejador.
///
/// La latencia se guarda como **histograma**, no como media: el SLO habla de P95 y un percentil no se
/// reconstruye a partir de una media.
/// </summary>
public sealed class RequestMetricsMiddleware(RequestDelegate next)
{
    /// <summary>
    /// MVP-699 (`R-03`) — Rutas que el servidor sirve pero que **no son tráfico de nadie**: la sonda de
    /// salud del alojamiento, la consulta de señales del propio equipo y la ingesta de telemetría.
    ///
    /// Quedan fuera del SLO porque lo hundían. Medido en la revisión: una hora de sonda más ocho
    /// peticiones de negocio dejaba la sonda en el **87 % del divisor**. Con tráfico realista —1440
    /// sondas al día frente a 200 peticiones reales— un 5 % de fallo real se lee como 0,61 % y
    /// <c>HighErrorRate</c> **no salta**. La latencia sufría lo mismo: la sonda es trivial y arrastraba
    /// el P95 hacia abajo.
    ///
    /// No se descartan: se cuentan en <c>api.internal.*</c>, para que dejar de servirlas siga siendo
    /// visible.
    /// </summary>
    private static readonly string[] NonUserFacingPaths =
    [
        "/api/v1/health",
        "/api/v1/ops",
        "/api/v1/telemetry",
        "/api/v1/auth/telemetry",
    ];

    public async Task InvokeAsync(HttpContext context, ITelemetryCounters counters)
    {
        // Los ficheros del cliente no son la API: contarlos hundiría la latencia media y metería en el
        // divisor del SLO peticiones que no ejecutan nada del servidor.
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        catch
        {
            // Una excepción sin capturar acaba en 500: se cuenta como tal y se deja subir.
            stopwatch.Stop();
            Record(counters, context, StatusCodes.Status500InternalServerError, stopwatch.Elapsed);
            throw;
        }

        stopwatch.Stop();
        Record(counters, context, context.Response.StatusCode, stopwatch.Elapsed);
    }

    private static void Record(
        ITelemetryCounters counters, HttpContext context, int statusCode, TimeSpan elapsed)
    {
        if (IsNonUserFacing(context.Request.Path))
        {
            counters.Add(TelemetryMetrics.ApiInternalRequests);
            if (statusCode >= 500) counters.Add(TelemetryMetrics.ApiInternalRequests5xx);
            return;
        }

        counters.Add(TelemetryMetrics.ApiRequests);

        if (statusCode >= 500) counters.Add(TelemetryMetrics.ApiRequests5xx);
        else if (statusCode >= 400) counters.Add(TelemetryMetrics.ApiRequests4xx);

        var elapsedMs = (long)elapsed.TotalMilliseconds;
        foreach (var bucket in TelemetryMetrics.LatencyBucketsMs)
        {
            if (elapsedMs <= bucket)
            {
                counters.Add(TelemetryMetrics.LatencyBucket(bucket));
                break;
            }
        }

        if (statusCode == StatusCodes.Status201Created && HttpMethods.IsPost(context.Request.Method))
        {
            counters.Add(TelemetryMetrics.ApiCreated);
            if (ResourceOf(context) is { } resource) counters.Add(TelemetryMetrics.CreatedFor(resource));
        }
    }

    private static bool IsNonUserFacing(PathString path)
        => NonUserFacingPaths.Any(prefix => path.StartsWithSegments(prefix));

    /// <summary>
    /// Primer segmento tras <c>/api/v1/</c>, saneado. Sale de la ruta y no de un valor de la petición,
    /// así que el conjunto es cerrado: un contador cuyo nombre dependa de texto de usuario dejaría de
    /// ser un contador.
    /// </summary>
    private static string? ResourceOf(HttpContext context)
    {
        var segments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments is not { Length: >= 3 }) return null;

        var resource = segments[2];
        return resource.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') ? resource : null;
    }
}
