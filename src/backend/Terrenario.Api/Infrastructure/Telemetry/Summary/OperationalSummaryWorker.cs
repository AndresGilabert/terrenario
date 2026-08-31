using Microsoft.Extensions.Options;
using Terrenario.Api.Application.Ops;
using Terrenario.Api.Infrastructure.Email;

namespace Terrenario.Api.Infrastructure.Telemetry.Summary;

/// <summary>
/// MKT-101 — Envía el resumen operativo diario y semanal a <c>Ops:AlertEmail</c>, para seguir tráfico y
/// conversión sin tener que llamar a <c>GET /api/v1/ops/signals</c> a mano.
///
/// Mismo patrón que <see cref="Alerts.AlertMonitor"/>: un <see cref="BackgroundService"/> que despierta
/// cada minuto y decide si toca enviar, en vez de programar un temporizador exacto a las 05:00. La
/// cadencia («una vez al día», «una vez cada 7 días») la da la marca de la última fecha enviada, no el
/// intervalo de reloj: reiniciar el proceso a las 05:03 no duplica el envío del día.
///
/// Un fallo de envío <b>se registra y no se marca como enviado</b>: se reintenta en la siguiente
/// pasada, dentro de la misma ventana horaria, y nunca tumba la aplicación (CA-3).
/// </summary>
public sealed class OperationalSummaryWorker(
    IServiceProvider services,
    IOptions<OpsOptions> options,
    TimeProvider clock,
    ILogger<OperationalSummaryWorker> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    /// <summary>Hora local (Europe/Madrid) a partir de la cual toca enviar, según lo acordado con el PO.</summary>
    private static readonly TimeOnly SendTimeLocal = new(5, 0);

    private static readonly TimeZoneInfo MadridZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    private DateOnly _lastDailySentOn = DateOnly.MinValue;

    /// <summary>Fecha (lunes) de la última semana ya enviada.</summary>
    private DateOnly _lastWeeklySentOn = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.SummaryEnabled)
        {
            logger.LogInformation("Resumen operativo por email (MKT-101) desactivado por configuración.");
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(Interval, clock, stoppingToken);
                await RunOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Parada normal de la aplicación.
        }
    }

    /// <summary>
    /// Una pasada de vigilancia. Público para poder ejercitarlo sin esperar al temporizador, igual que
    /// <c>TelemetryFlushWorker.FlushOnceAsync</c> y <c>AlertMonitor.RunOnceAsync</c>.
    /// </summary>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        var localNow = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), MadridZone);
        var localDate = DateOnly.FromDateTime(localNow.DateTime);
        var timeIsDue = TimeOnly.FromDateTime(localNow.DateTime) >= SendTimeLocal;

        var dailyDue = timeIsDue && _lastDailySentOn != localDate;
        var weeklyDue = timeIsDue && localNow.DayOfWeek == DayOfWeek.Monday && _lastWeeklySentOn != localDate;

        if (!dailyDue && !weeklyDue) return;

        try
        {
            // Un solo ámbito para toda la pasada: la fuente de señales y el transporte son servicios
            // por petición, y aquí no hay petición que los provea.
            using var scope = services.CreateScope();

            var signals = await scope.ServiceProvider.GetRequiredService<OperationalSignalsService>()
                .BuildAsync(dailyDays: 2, ct);
            var firingAlerts = signals.Alerts.Where(a => a.IsFiring).ToList();

            var mailer = scope.ServiceProvider.GetRequiredService<SmtpMailer>();
            var template = scope.ServiceProvider.GetRequiredService<ProductEmailTemplate>();
            var recipient = options.Value.AlertEmail;

            if (dailyDue && await TrySendAsync(
                    mailer,
                    () => OperationalSummaryEmailComposer.ComposeDaily(
                        template, recipient, signals.Daily[0], firingAlerts),
                    "resumen-operativo-diario",
                    recipient,
                    ct))
                _lastDailySentOn = localDate;

            if (weeklyDue && await TrySendAsync(
                    mailer,
                    () => OperationalSummaryEmailComposer.ComposeWeekly(
                        template, recipient, localDate, signals.LoginFunnel7d, signals.ProductUsage7d,
                        signals.Slo, firingAlerts),
                    "resumen-operativo-semanal",
                    recipient,
                    ct))
                _lastWeeklySentOn = localDate;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Parada durante la pasada.
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex, "No se pudo generar el resumen operativo. Se reintentará en la siguiente pasada.");
        }
    }

    /// <summary>
    /// Sin destinatario o sin cuenta de envío configurados, se da por «gestionado» y no se reintenta
    /// cada minuto: mismo criterio que el resto de correos del producto (ADR-0010), que no fingen un
    /// envío que no puede ocurrir. Un fallo real del transporte sí se reintenta en la siguiente pasada.
    /// </summary>
    private async Task<bool> TrySendAsync(
        SmtpMailer mailer, Func<MimeKit.MimeMessage> compose, string context, string recipient, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(recipient) || !mailer.IsEnabled)
        {
            logger.LogInformation(
                "Resumen operativo ({Context}) sin enviar: falta destinatario o cuenta de envío.", context);
            return true;
        }

        try
        {
            await mailer.SendAsync(compose(), context, ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex, "No se pudo enviar el resumen operativo ({Context}). Se reintentará en la siguiente pasada.",
                context);
            return false;
        }
    }
}
