using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Infrastructure.Data;

namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-603 — Comprobación de salud del servicio.
///
/// Comprueba lo único que la aplicación puede comprobar y de verdad importa: que **responde** y que
/// **alcanza su base de datos**. Sin base de datos el producto no sirve para nada aunque el proceso
/// esté vivo, y ese es exactamente el fallo parcial que un 200 en la raíz no distinguiría.
///
/// La consulta es la más barata que existe (<c>SELECT 1</c> implícito): esto lo llama una sonda cada
/// pocos segundos, así que no puede costar nada.
/// </summary>
public sealed class HealthProbe(TerrenarioDbContext db, ILogger<HealthProbe> logger)
{
    public async Task<HealthReport> CheckAsync(CancellationToken ct)
    {
        try
        {
            var reachable = await db.Database.CanConnectAsync(ct);

            return reachable
                ? new HealthReport(true, "healthy")
                : new HealthReport(false, "unreachable");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // El motivo va a la traza, no a la respuesta: el detalle de por qué falla la base de datos
            // no se le cuenta a quien pregunta desde fuera.
            logger.LogError(ex, "La comprobación de salud no pudo alcanzar la base de datos.");
            return new HealthReport(false, "unreachable");
        }
    }
}

/// <param name="IsHealthy">Si el servicio puede prestar el servicio, no solo si está vivo.</param>
/// <param name="Database">Estado de la dependencia, sin detalle interno.</param>
public sealed record HealthReport(bool IsHealthy, string Database);
