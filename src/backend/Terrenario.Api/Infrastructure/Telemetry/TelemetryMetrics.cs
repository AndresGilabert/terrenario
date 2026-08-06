namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-601 — Nombres canónicos de los contadores agregados. Centralizarlos evita que quien suma y
/// quien lee (la revisión semanal de KPIs, y las señales de <c>MVP-603</c>) se desincronicen, que es
/// exactamente lo que ya pasó con los nombres de los eventos y resolvió <see cref="LoginFunnelEvents"/>.
///
/// <b>Solo contadores</b>: ni una fila por evento ni un identificador conservado. Es lo que permite
/// calcular las ventanas de 7 y 30 días que piden los SLO sin retener nada de nadie (RN-042).
/// </summary>
public static class TelemetryMetrics
{
    public const string LoginScreenViewed = "login.screen_viewed";
    public const string LoginGoogleClicked = "login.google_clicked";
    public const string LoginSuccess = "login.success";
    public const string LoginError = "login.error";
    public const string LoginAbandonment = "login.abandonment";

    /// <summary>
    /// Suma de duraciones de los logins con éxito, en milisegundos. Con
    /// <see cref="LoginSuccess"/> como divisor da el «tiempo medio de login exitoso» del SLO
    /// (&lt;= 45 s). Se guarda como suma y no como media porque las medias no se pueden agregar entre
    /// días: la de la semana no es la media de las medias diarias.
    /// </summary>
    public const string LoginSuccessDurationMsSum = "login.success.duration_ms.sum";

    /// <summary>
    /// Logins con éxito de los que **sí** se conocía el instante de entrada a la pantalla, que es el
    /// divisor correcto de <see cref="LoginSuccessDurationMsSum"/>. No coincide con
    /// <see cref="LoginSuccess"/>: si la aplicación se reinicia a mitad de un intento, ese éxito se
    /// cuenta pero su duración no se conoce, y dividir por el total daría una media falsamente baja.
    /// </summary>
    public const string LoginSuccessTimedCount = "login.success.timed";

    // ── Uso del producto (MVP-602) ───────────────────────────────────────────────

    /// <summary>Sesiones que han entrado al área autenticada: el divisor del uso del dashboard.</summary>
    public const string AppSessionStarted = "app.session_started";

    /// <summary>Entradas al dashboard, todas.</summary>
    public const string DashboardViewed = "dashboard.viewed";

    /// <summary>
    /// Sesiones que han visto el dashboard <b>al menos una vez</b>. No es lo mismo que
    /// <see cref="DashboardViewed"/>: el KPI de la KB pregunta por sesiones, no por visitas, y quien
    /// entra ocho veces en una sesión sigue siendo una sesión.
    /// </summary>
    public const string DashboardSessionWithView = "dashboard.session_with_view";

    /// <summary>Pulsaciones de «Actualizar» (CA-2).</summary>
    public const string DashboardManualRefresh = "dashboard.manual_refresh";

    /// <summary>Widgets que se pudieron mostrar (con datos o en estado vacío legítimo).</summary>
    public const string DashboardWidgetRendered = "dashboard.widget.rendered";

    /// <summary>Widgets que no se pudieron mostrar. Es lo que resta cobertura.</summary>
    public const string DashboardWidgetBlocked = "dashboard.widget.blocked";

    /// <summary>
    /// Desglose por widget y estado (<c>dashboard.widget.summary.error</c>). Sin él, la cobertura diría
    /// que algo falla pero no qué, que es justo lo que hace falta para arreglarlo.
    /// </summary>
    public static string DashboardWidgetFor(string widget, string status)
        => $"dashboard.widget.{widget}.{status}";

    // ── Salud operativa (MVP-603) ────────────────────────────────────────────────

    /// <summary>Peticiones servidas. Divisor de la tasa de error y de la latencia.</summary>
    public const string ApiRequests = "api.requests";

    /// <summary>Respuestas 4xx: error **visible para quien usa**, no fallo del servidor.</summary>
    public const string ApiRequests4xx = "api.requests.4xx";

    /// <summary>
    /// MVP-699 (`R-03`) — Tráfico que el servidor sirve pero por el que **no espera nadie**: la sonda de
    /// salud y la ingesta de telemetría. Se cuenta aparte, no se descarta: si dejara de existir habría
    /// que poder verlo, y un contador que desaparece en silencio es peor que uno que estorba.
    /// </summary>
    public const string ApiInternalRequests = "api.internal.requests";

    public const string ApiInternalRequests5xx = "api.internal.requests.5xx";

    /// <summary>Respuestas 5xx: el SLO de tasa de error de la KB.</summary>
    public const string ApiRequests5xx = "api.requests.5xx";

    /// <summary>Altas (POST con 201) en total y por recurso: el `registros_creados_semana` de la KB.</summary>
    public const string ApiCreated = "api.created";

    /// <summary>
    /// Minutos en los que la aplicación se observó a sí misma sana / degradada. Mide **degradación**,
    /// no caída: un proceso muerto no se observa (ver <c>observabilidad.md</c>).
    /// </summary>
    public const string HealthProbeOk = "health.probe.ok";

    public const string HealthProbeFailed = "health.probe.failed";

    /// <summary>Veces que una alerta ha pasado a estado «disparada».</summary>
    public static string AlertFiredFor(string alertName) => $"alert.fired.{alertName.ToLowerInvariant()}";

    /// <summary>
    /// Cortes del histograma de latencia, en milisegundos. Se guarda un histograma y no la media
    /// porque el SLO habla de **P95**, y un percentil no se puede reconstruir a partir de una media.
    /// Los cortes rodean los dos umbrales que importan: 300 ms (objetivo) y 500 ms (alerta).
    /// </summary>
    public static readonly int[] LatencyBucketsMs = [50, 100, 200, 300, 500, 1000, 2000, int.MaxValue];

    public static string LatencyBucket(int upperBoundMs) =>
        upperBoundMs == int.MaxValue ? "api.latency_ms.bucket.inf" : $"api.latency_ms.bucket.{upperBoundMs}";

    /// <summary>Contador de altas por recurso (<c>api.created.harvests</c>).</summary>
    public static string CreatedFor(string resource) => $"{ApiCreated}.{resource}";

    private const int ErrorCodeMaxLength = 48;

    /// <summary>
    /// Contador por código de error (<c>login.error.auth_google_token_invalid</c>). El código viene de
    /// <c>ErrorCodes</c>, que es un conjunto cerrado, pero se sanea igualmente: un contador cuyo nombre
    /// dependa de texto no acotado deja de ser un contador y pasa a ser una fuga de cardinalidad.
    /// </summary>
    public static string LoginErrorFor(string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
            return $"{LoginError}.{TelemetryDimensions.Unknown}";

        var saneado = new string(errorCode
            .Where(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-')
            .Take(ErrorCodeMaxLength)
            .ToArray())
            .ToLowerInvariant();

        return saneado.Length == 0
            ? $"{LoginError}.{TelemetryDimensions.Unknown}"
            : $"{LoginError}.{saneado}";
    }
}
