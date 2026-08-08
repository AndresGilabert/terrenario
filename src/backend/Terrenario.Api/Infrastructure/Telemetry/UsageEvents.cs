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
    /// Pulsación de «Actualizar» (RN-006). <b>Discontinuada en MVP-706</b>: el PO retiró el botón, que
    /// era su única fuente, así que ni el cliente la emite ni el informe operativo la publica.
    ///
    /// Se sigue <b>aceptando</b> a propósito: un cliente cacheado en un navegador puede seguir
    /// enviándola durante un tiempo tras el despliegue, y responderle <c>400</c> convertiría un resto
    /// inofensivo en un error de cliente contado. Su contador se sigue escribiendo, de modo que la
    /// serie histórica de la tabla no se rompe; simplemente ya no se lee.
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

    /// <summary>
    /// MVP-707 — Lectura económica de la campaña. RN-009 amplía los widgets obligatorios con gasto e
    /// ingreso, así que la cobertura tiene que contarlo: si no, un panel con el widget económico roto
    /// seguiría midiendo 100 %.
    /// </summary>
    public const string Economics = "economics";

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
        new HashSet<string>(StringComparer.Ordinal)
        {
            Summary, KgByDestination, KgByPlot, YieldEvolution, Economics
        };

    public static readonly IReadOnlySet<string> Statuses =
        new HashSet<string>(StringComparer.Ordinal) { StatusOk, StatusEmpty, StatusError };
}
