using Microsoft.Extensions.Options;
using Terrenario.Api.Application.Retention;
using Terrenario.Api.Infrastructure.Data;

namespace Terrenario.Api.Infrastructure.Retention;

/// <summary>
/// MVP-504 (B-3) — Quién ejecuta la rutina de expurgo de <c>RN-041</c>.
///
/// <b>Por qué dentro de la API</b> y no como tarea programada del alojamiento o job de contenedor:
/// esas dos alternativas exigen infraestructura que todavía no existe —es lo que bloquea <c>B-2</c>—
/// y habrían dejado el expurgo esperando a que la hubiera. Un <see cref="BackgroundService"/> viaja
/// con la aplicación, funciona igual en local y en el alojamiento, y no añade nada que desplegar.
/// El precio es que solo corre si la aplicación está viva; con plazos de 24 meses —y de 30 días en
/// el más corto, el de los tokens de refresco (MVP-714)— perder algún día no tiene consecuencia.
///
/// <b>Varias instancias</b>: si el día de mañana la API escala, dos réplicas intentarían purgar a la
/// vez. Se evita con un <i>advisory lock</i> de PostgreSQL de ámbito de transacción: la primera lo
/// coge, las demás ven que está cogido y se van. No espera —volver dentro de 24 horas es mejor que
/// tener un hilo bloqueado—, y al ser de transacción se libera solo, también si algo revienta.
///
/// Toda la purga va en <b>una transacción</b>: media purga aplicada es un estado que nadie ha
/// pensado, y aquí no hay prisa que justifique el riesgo.
/// </summary>
public sealed class RetentionPurgeWorker(
    IServiceProvider services,
    IOptions<RetentionOptions> options,
    ILogger<RetentionPurgeWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        if (!settings.Enabled)
        {
            logger.LogInformation("Rutina de expurgo (RN-041) desactivada por configuración.");
            return;
        }

        try
        {
            await Task.Delay(settings.InitialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOnceAsync(stoppingToken);
                await Task.Delay(settings.Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Parada normal de la aplicación.
        }
    }

    /// <summary>
    /// Una pasada. Un fallo se registra y no se propaga: si el expurgo revienta, lo que no puede
    /// pasar es que se lleve por delante el proceso que sirve la aplicación.
    /// </summary>
    internal async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TerrenarioDbContext>();
            var purge = scope.ServiceProvider.GetRequiredService<RetentionPurgeService>();

            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            if (!await RetentionAdvisoryLock.TryAcquireAsync(db, ct))
            {
                logger.LogInformation("Expurgo omitido: otra instancia lo está ejecutando.");
                return;
            }

            var report = await purge.PurgeAsync(DateTimeOffset.UtcNow, ct);
            await transaction.CommitAsync(ct);

            if (report.Total == 0 && report.AccountsRetained == 0)
                logger.LogInformation("Expurgo completado (RN-041): nada había cumplido el plazo.");
            else
                logger.LogInformation("Expurgo completado (RN-041): {Report}", report);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo en la rutina de expurgo (RN-041). Se reintentará en la siguiente pasada.");
        }
    }

}
