using System.Globalization;
using MimeKit;
using Terrenario.Api.Application.Ops;
using Terrenario.Api.Infrastructure.Email;
using Terrenario.Api.Infrastructure.Telemetry.Alerts;

namespace Terrenario.Api.Infrastructure.Telemetry.Summary;

/// <summary>
/// MKT-101 — Compone el resumen operativo diario y semanal a partir de las mismas señales que expone
/// <c>GET /api/v1/ops/signals</c>, para no mantener una segunda fuente de verdad de las métricas.
///
/// El resumen semanal incluye desde <c>MKT-106</c> el top de landings por conversión
/// (<c>landing_view -&gt; login_view -&gt; login_success</c>); el diario no, porque esa serie solo se
/// calcula sobre 7 días.
/// </summary>
public static class OperationalSummaryEmailComposer
{
    private const string Reason =
        "esta dirección está configurada como destinatario de los resúmenes operativos de Terrenario "
        + "(«Ops:AlertEmail»)";

    private const string OptOut =
        "Para dejar de recibirlos, retira la dirección de la configuración «Ops:AlertEmail» del "
        + "despliegue o desactiva «Ops:SummaryEnabled».";

    private static readonly IReadOnlyList<string> DailyNotes =
    [
        "Las visitas a landing se resumen solo en el correo semanal: la serie de conversión se calcula "
        + "sobre 7 días."
    ];

    public static MimeMessage ComposeDaily(
        ProductEmailTemplate template,
        string recipient,
        DailySignals day,
        IReadOnlyList<AlertState> firingAlerts) =>
        template.Compose(new ProductEmailContent
        {
            ToEmail = recipient,
            Subject = $"[Terrenario] Resumen diario — {day.Date:yyyy-MM-dd}",
            Heading = "Resumen operativo diario",
            Paragraphs =
            [
                $"Sesiones: {day.Sessions}.",
                $"Acceso a login: {day.LoginScreenViewed}.",
                $"Login exitoso: {day.LoginSuccess}.",
                $"Tasa de conversión: {FormatRatio(day.LoginConversion)}.",
                AlertsParagraph(firingAlerts)
            ],
            Notes = DailyNotes,
            Reason = Reason,
            OptOut = OptOut
        });

    public static MimeMessage ComposeWeekly(
        ProductEmailTemplate template,
        string recipient,
        DateOnly weekStart,
        LoginFunnelSignals loginFunnel7d,
        ProductUsageSignals productUsage7d,
        SloSignals slo,
        IReadOnlyList<LandingSignals> landingConversion7d,
        IReadOnlyList<AlertState> firingAlerts) =>
        template.Compose(new ProductEmailContent
        {
            ToEmail = recipient,
            Subject = $"[Terrenario] Resumen semanal — semana del {weekStart:yyyy-MM-dd}",
            Heading = "Resumen operativo semanal",
            Paragraphs =
            [
                $"Sesiones (7 días): {productUsage7d.Sessions}.",
                $"Acceso a login (7 días): {loginFunnel7d.ScreenViewed}.",
                $"Login exitoso (7 días): {loginFunnel7d.Success}.",
                $"Tasa de conversión (7 días): {FormatRatio(loginFunnel7d.Conversion)}.",
                $"Tasa de error (7 días): {FormatRatio(slo.ErrorRate7d)} "
                + $"(objetivo {FormatRatio(slo.ErrorRateObjective)}).",
                LandingConversionParagraph(landingConversion7d),
                AlertsParagraph(firingAlerts)
            ],
            Reason = Reason,
            OptOut = OptOut
        });

    /// <summary>
    /// MKT-106 — Las 5 landings con más vistas de la semana, con su conversión a login exitoso. Sin
    /// datos todavía (catálogo abierto: puede no haber ninguna landing con vistas esa semana) se dice
    /// explícitamente, no se omite el párrafo.
    /// </summary>
    private static string LandingConversionParagraph(IReadOnlyList<LandingSignals> landingConversion7d)
    {
        if (landingConversion7d.Count == 0) return "Landings (7 días): sin visitas registradas.";

        var top = landingConversion7d
            .OrderByDescending(l => l.Views)
            .Take(5)
            .Select(l => $"{l.Landing}: {l.Views} vistas, conversión {FormatRatio(l.Conversion)}");

        return $"Landings (7 días): {string.Join("; ", top)}.";
    }

    private static string AlertsParagraph(IReadOnlyList<AlertState> firingAlerts) =>
        firingAlerts.Count == 0
            ? "Sin alertas activas."
            : $"Alertas activas: {string.Join(", ", firingAlerts.Select(a => a.Name))}.";

    /// <summary>
    /// Igual que <c>AlertEvaluator.Percent</c>: un cociente ausente (divisor cero) se dice «sin datos»
    /// en vez de inventar un 0 %.
    /// </summary>
    private static string FormatRatio(double? ratio) =>
        ratio is null ? "sin datos" : (ratio.Value * 100).ToString("0.##", CultureInfo.InvariantCulture) + " %";
}
