using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using Terrenario.Api.Application.Ops;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-603 (CA-3) — Las señales operativas del MVP en una sola respuesta: los tres SLO, el embudo de
/// login, el uso del producto, el monitoreo de negocio mínimo y las alertas vivas.
///
/// <b>Autenticación de servicio</b> por llave (`X-Ops-Key`), no sesión de usuario: quien consulta esto
/// es el equipo, y el producto no tiene roles con los que distinguir a un operador de un agricultor
/// (`autenticacion-autorizacion.md` contempla justamente llaves M2M para esto). Sin llave configurada
/// el endpoint **no existe** (404): desplegar sin configurarlo debe impedir consultarlo, no abrirlo.
///
/// La comparación es en tiempo constante: comparar secretos con <c>==</c> filtra información por el
/// tiempo de respuesta.
/// </summary>
[ApiController]
[Route("api/v1/ops")]
public sealed class OpsController(
    OperationalSignalsService signals, IOptions<OpsOptions> options) : ControllerBase
{
    public const string ApiKeyHeader = "X-Ops-Key";

    [HttpGet("signals")]
    [AllowAnonymous]
    public async Task<IActionResult> Signals(CancellationToken ct)
    {
        var configured = options.Value;

        if (!configured.IsSignalsEndpointEnabled) return NotFound();

        if (!IsAuthorized(Request.Headers[ApiKeyHeader].ToString(), configured.ApiKey))
            return Unauthorized();

        var report = await signals.BuildAsync(ct);

        return Ok(new
        {
            generated_at = report.GeneratedAt,
            slo = new
            {
                error_rate_7d = report.Slo.ErrorRate7d,
                error_rate_objective = report.Slo.ErrorRateObjective,
                latency_p95_7d_ms = report.Slo.LatencyP95Ms7d,
                latency_p95_objective_ms = report.Slo.LatencyP95ObjectiveMs,
                // Deliberadamente no se llama `uptime`: mide minutos **observados**, y un proceso caído
                // no se observa a sí mismo (ver `observabilidad.md`).
                healthy_minutes_30d = report.Slo.HealthyMinutes30d,
                degraded_minutes_30d = report.Slo.DegradedMinutes30d
            },
            login_funnel_7d = new
            {
                screen_viewed = report.LoginFunnel7d.ScreenViewed,
                google_clicked = report.LoginFunnel7d.GoogleClicked,
                success = report.LoginFunnel7d.Success,
                errors = report.LoginFunnel7d.Errors,
                abandonment = report.LoginFunnel7d.Abandonment,
                conversion = report.LoginFunnel7d.Conversion,
                abandonment_rate = report.LoginFunnel7d.AbandonmentRate,
                average_success_ms = report.LoginFunnel7d.AverageSuccessMs
            },
            product_usage_7d = new
            {
                sessions = report.ProductUsage7d.Sessions,
                sessions_with_dashboard = report.ProductUsage7d.SessionsWithDashboard,
                dashboard_usage = report.ProductUsage7d.DashboardUsage,
                manual_refresh_per_session = report.ProductUsage7d.ManualRefreshPerSession,
                widget_coverage = report.ProductUsage7d.WidgetCoverage
            },
            business_7d = new
            {
                logins = report.Business7d.Logins,
                records_created = report.Business7d.RecordsCreated,
                visible_error_rate = report.Business7d.VisibleErrorRate
            },
            live = new
            {
                window_minutes = report.Live.WindowMinutes,
                requests = report.Live.Requests,
                error_rate = report.Live.ErrorRate,
                latency_p95_ms = report.Live.LatencyP95Ms,
                login_screen_viewed = report.Live.LoginScreenViewed,
                login_conversion = report.Live.LoginConversion
            },
            alerts = report.Alerts.Select(alert => new
            {
                name = alert.Name,
                severity = alert.Severity.ToString().ToLowerInvariant(),
                state = alert.IsFiring ? "firing" : "ok",
                detail = alert.Detail,
                firing_since = alert.FiringSince
            })
        });
    }

    private static bool IsAuthorized(string provided, string expected)
        => !string.IsNullOrEmpty(provided) &&
           CryptographicOperations.FixedTimeEquals(
               Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected));
}
