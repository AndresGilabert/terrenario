namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-602 — Nombres canónicos de las señales de uso del producto. Mismo criterio que
/// <see cref="LoginFunnelEvents"/>: conjunto cerrado, para que emisor e ingestor no se desincronicen y
/// para que ningún cliente pueda inventarse una métrica.
/// </summary>
public static class UsageEvents
{
    /// <summary>
    /// La sesión ha entrado al área autenticada. Es el <b>denominador</b> del KPI «uso del dashboard
    /// en sesiones activas»: sin él, «85 % de las sesiones» no tiene sobre qué calcularse.
    /// </summary>
    public const string AppSessionStarted = "app_session_started";

    /// <summary>Entrada a la pantalla del dashboard.</summary>
    public const string DashboardViewed = "dashboard_viewed";

    /// <summary>
    /// Pulsación de «Actualizar» (RN-006). Señal <b>separada</b> de la entrada, como pide CA-2: entrar
    /// y recargar responden a preguntas distintas —si se consulta y si se vuelve a consultar—.
    /// </summary>
    public const string DashboardManualRefresh = "dashboard_manual_refresh";

    /// <summary>Resultado de pintar los widgets del dashboard, para la cobertura de widgets MVP.</summary>
    public const string DashboardWidgets = "dashboard_widgets";

    public static readonly IReadOnlySet<string> ClientIngestable =
        new HashSet<string>(StringComparer.Ordinal)
        {
            AppSessionStarted,
            DashboardViewed,
            DashboardManualRefresh,
            DashboardWidgets,
        };
}

/// <summary>
/// Widgets MVP del dashboard (<c>MVP-403</c>/<c>MVP-404</c>) y estado con el que cada uno se resuelve.
/// Cerrado por la misma razón que <c>device_type</c>: una clave de contador que dependa de texto libre
/// deja de ser un contador.
/// </summary>
public static class DashboardWidgets
{
    public const string Summary = "summary";
    public const string KgByDestination = "kg_by_destination";
    public const string KgByPlot = "kg_by_plot";
    public const string YieldEvolution = "yield_evolution";

    /// <summary>Se pintó con datos.</summary>
    public const string StatusOk = "ok";

    /// <summary>
    /// Se pintó sin datos que mostrar. <b>Cuenta como cubierto</b>: el KPI de la KB admite
    /// expresamente «estados vacío/incompleto cuando aplique». Un Workspace que aún no ha cosechado no
    /// es un widget roto.
    /// </summary>
    public const string StatusEmpty = "empty";

    /// <summary>No se pudo mostrar. Es lo único que resta cobertura.</summary>
    public const string StatusError = "error";

    public static readonly IReadOnlySet<string> Keys =
        new HashSet<string>(StringComparer.Ordinal) { Summary, KgByDestination, KgByPlot, YieldEvolution };

    public static readonly IReadOnlySet<string> Statuses =
        new HashSet<string>(StringComparer.Ordinal) { StatusOk, StatusEmpty, StatusError };
}
