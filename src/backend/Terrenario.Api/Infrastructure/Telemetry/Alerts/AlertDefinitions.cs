namespace Terrenario.Api.Infrastructure.Telemetry.Alerts;

/// <summary>
/// MVP-603 — Las cinco alertas que <c>docs/05-infraestructura/observabilidad.md</c> declara activas, con
/// sus umbrales. Los nombres son literalmente los de la KB: si aquí se llamaran de otra forma, la tabla
/// de la KB dejaría de servir para saber qué está vigilando el sistema.
/// </summary>
public static class AlertNames
{
    public const string HighErrorRate = "HighErrorRate";
    public const string HighLatency = "HighLatency";
    public const string ServiceDown = "ServiceDown";
    public const string LoginAbandonmentSpike = "LoginAbandonmentSpike";
    public const string LoginSuccessDrop = "LoginSuccessDrop";
}

public enum AlertSeverity
{
    Warning,
    High,
    Critical
}

/// <summary>Umbrales, todos con origen en la KB. No se inventa ninguno aquí.</summary>
public static class AlertThresholds
{
    /// <summary>Ventana de evaluación de las alertas del embudo, fijada por la KB («durante 30 min»).</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(30);

    /// <summary>`kpis.md`, KPI técnico «Tasa de errores», umbral de alerta &gt; 1 %.</summary>
    public const double ErrorRate = 0.01;

    /// <summary>`kpis.md`, KPI técnico «Tiempo de respuesta P95», umbral de alerta &gt; 500 ms.</summary>
    public const int LatencyP95Ms = 500;

    /// <summary>`observabilidad.md`, `LoginAbandonmentSpike`: abandono &gt; 25 %.</summary>
    public const double LoginAbandonment = 0.25;

    /// <summary>`observabilidad.md`, `LoginSuccessDrop`: conversión &lt; 70 %.</summary>
    public const double LoginConversion = 0.70;

    /// <summary>
    /// Peticiones mínimas en la ventana antes de juzgar tasa de error o latencia.
    ///
    /// Sin esto, una sola respuesta 500 en una madrugada con tres peticiones daría un 33 % de error y
    /// dispararía una alerta crítica por nada. Una alerta que salta sin motivo se acaba ignorando, y
    /// entonces tampoco sirve cuando el motivo es real.
    /// </summary>
    public const int MinRequests = 20;

    /// <summary>Pantallas de login mínimas en la ventana antes de juzgar el embudo, por el mismo motivo.</summary>
    public const int MinLoginScreens = 10;

    /// <summary>
    /// Comprobaciones de salud fallidas seguidas antes de dar el servicio por caído. La KB dice «&gt; 1
    /// min» y la sonda corre cada minuto, así que dos fallos seguidos es la traducción exacta —y de paso
    /// evita alertar por un corte de red de un segundo—.
    /// </summary>
    public const int FailedProbesToAlert = 2;
}

/// <summary>Resultado de evaluar una alerta sobre una ventana.</summary>
public sealed record AlertVerdict(
    string Name,
    AlertSeverity Severity,
    bool IsFiring,
    string Detail);
