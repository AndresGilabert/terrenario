using Microsoft.Extensions.Options;
using MimeKit;
using Terrenario.Api.Infrastructure.Email;

namespace Terrenario.Api.Infrastructure.Telemetry.Alerts;

/// <summary>
/// MVP-603 — Publica las alertas.
///
/// <b>Traza siempre</b>, con el nivel que corresponde a la severidad, para que la alerta quede en el
/// registro aunque no haya canal configurado. <b>Correo cuando se puede</b>: sin destinatario, una
/// alerta es una anotación que nadie lee, y la KB pide un canal de incidentes. Reutiliza el transporte
/// SMTP que ya existe (ADR-0010) en vez de traer nada nuevo.
///
/// Un fallo de envío **no se propaga**: el aviso ya ha quedado en la traza, y que el mecanismo de
/// alerta tumbe el proceso al que vigila sería el peor final posible.
///
/// MVP-715 — El mensaje lo arma <see cref="AlertEmailComposer"/> con la plantilla común del
/// producto, no este notificador: aquí queda la decisión de cuándo se traza y cuándo se envía.
/// </summary>
public sealed class AlertNotifier(
    SmtpMailer mailer,
    ProductEmailTemplate template,
    IOptions<OpsOptions> options,
    ILogger<AlertNotifier> logger) : IAlertNotifier
{
    public async Task NotifyFiringAsync(AlertVerdict verdict, CancellationToken ct)
    {
        Log(verdict.Severity,
            "alert.fired name={Alert} severity={Severity} detail={Detail}",
            verdict.Name, verdict.Severity.ToString().ToLowerInvariant(), verdict.Detail);

        await SendAsync(
            recipient => AlertEmailComposer.ComposeFiring(template, recipient, verdict),
            ct);
    }

    public async Task NotifyResolvedAsync(AlertVerdict verdict, TimeSpan duration, CancellationToken ct)
    {
        logger.LogInformation(
            "alert.resolved name={Alert} duration_minutes={Minutes} detail={Detail}",
            verdict.Name, (long)duration.TotalMinutes, verdict.Detail);

        await SendAsync(
            recipient => AlertEmailComposer.ComposeResolved(template, recipient, verdict, duration),
            ct);
    }

    private void Log(AlertSeverity severity, string template, params object[] args)
    {
        if (severity == AlertSeverity.Warning) logger.LogWarning(template, args);
        else logger.LogError(template, args);
    }

    private async Task SendAsync(Func<string, MimeMessage> compose, CancellationToken ct)
    {
        var recipient = options.Value.AlertEmail;
        if (string.IsNullOrWhiteSpace(recipient) || !mailer.IsEnabled) return;

        try
        {
            await mailer.SendAsync(compose(recipient), "alerta-operativa", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar el aviso de alerta por correo. Queda en la traza.");
        }
    }
}
