using MimeKit;
using Terrenario.Api.Infrastructure.Email;

namespace Terrenario.Api.Infrastructure.Telemetry.Alerts;

/// <summary>
/// MVP-715 — Composición de los avisos de alerta (MVP-603) sobre la plantilla común.
///
/// Van a la dirección de operación (<c>Ops:AlertEmail</c>), no a un usuario, y por eso hasta ahora
/// eran el único correo del producto sin ninguna maquetación: un <c>TextPart</c> suelto. Se migran
/// igualmente porque el inventario no distingue destinatarios —un correo del producto es un correo
/// del producto— y porque aquí el «motivo del envío» y el «cómo dejar de recibirlo» son
/// **información operativa útil**: quien hereda una bandeja de alertas necesita saber de dónde sale
/// y dónde se apaga.
///
/// Sin llamada a la acción a propósito: la respuesta a una alerta es el runbook, no un enlace.
/// </summary>
public static class AlertEmailComposer
{
    private const string Reason =
        "esta dirección está configurada como destinatario de las alertas de operación de Terrenario";

    private const string OptOut =
        "Para dejar de recibirlas, retira la dirección de la configuración «Ops:AlertEmail» del "
        + "despliegue.";

    public static MimeMessage ComposeFiring(
        ProductEmailTemplate template,
        string recipient,
        AlertVerdict verdict) =>
        template.Compose(new ProductEmailContent
        {
            ToEmail = recipient,
            Subject = $"[Terrenario] Alerta {verdict.Name}",
            Heading = $"Alerta {verdict.Name}",
            Paragraphs =
            [
                $"La alerta {verdict.Name} ({verdict.Severity}) se ha disparado.",
                verdict.Detail
            ],
            Notes = ["Runbook: docs/08-procesos/gestion-incidentes.md"],
            Reason = Reason,
            OptOut = OptOut
        });

    public static MimeMessage ComposeResolved(
        ProductEmailTemplate template,
        string recipient,
        AlertVerdict verdict,
        TimeSpan duration) =>
        template.Compose(new ProductEmailContent
        {
            ToEmail = recipient,
            Subject = $"[Terrenario] Resuelta {verdict.Name}",
            Heading = $"Resuelta {verdict.Name}",
            Paragraphs =
            [
                $"La alerta {verdict.Name} se ha resuelto tras {(long)duration.TotalMinutes} minutos.",
                verdict.Detail
            ],
            Reason = Reason,
            OptOut = OptOut
        });
}
