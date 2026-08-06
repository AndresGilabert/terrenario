using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-603 (CA-1) — Comprobación de salud. Es la superficie que sondea la plataforma de alojamiento y
/// la que hace verificable la alerta `ServiceDown` de la KB («health check falla &gt; 1 min»).
///
/// <b>Anónima</b>, porque la sonda no tiene sesión. Y por eso mismo <b>no cuenta nada de dentro</b>: ni
/// versión, ni cadena de conexión, ni el motivo del fallo. Un endpoint de salud es también una
/// superficie expuesta a Internet.
///
/// Responde <c>503</c> cuando no puede prestar servicio, no <c>200</c> con un cuerpo que diga que va
/// mal: las sondas miran el código de estado, y un 200 con «unhealthy» dentro es un servicio caído que
/// nadie detecta.
/// </summary>
[ApiController]
[Route("api/v1/health")]
public sealed class HealthController(HealthProbe probe) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var report = await probe.CheckAsync(ct);

        var body = new { status = report.IsHealthy ? "healthy" : "degraded", database = report.Database };

        return report.IsHealthy
            ? Ok(body)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }
}
