using MimeKit;
using Microsoft.Extensions.Options;
using Terrenario.Api.Infrastructure.Email;
using Terrenario.Api.Infrastructure.Feedback;
using Terrenario.Api.Infrastructure.Invitations;
using Terrenario.Api.Infrastructure.Telemetry.Alerts;

namespace Terrenario.Api.Tests.Emails;

/// <summary>
/// MVP-715 — <b>El inventario de correos salientes, ejecutable.</b>
///
/// La versión en prosa vive en <c>docs/06-integraciones/correos-del-producto.md</c>, pero un
/// inventario en un documento envejece en silencio. Este catálogo compone los correos con datos de
/// ejemplo y es lo que recorren las pruebas transversales: añadir un correo nuevo sin añadirlo aquí
/// deja de ser un olvido invisible, porque el correo nuevo se queda sin las garantías que el resto
/// tiene comprobadas (pie legal, motivo, versión en texto plano, cero recursos remotos).
///
/// MVP-711 — Entra el sexto, el del canal de sugerencias e incidencias. Es el primero que llega por
/// esta puerta desde que existe el inventario, y por eso importa que llegue: `MVP-715` dejó escrito
/// que tendría que hacerlo.
/// </summary>
internal static class ProductEmailCatalog
{
    /// <summary>Cuenta de envío de ejemplo. Nunca se conecta a nada: solo se compone.</summary>
    internal static readonly EmailOptions Account = new()
    {
        Host = "smtp.ejemplo.com",
        FromAddress = "no-reply@terrenario.com",
        FromName = "Terrenario"
    };

    /// <summary>
    /// La identidad real del despliegue, no una inventada: los tests del pie legal deben fallar si
    /// el fichero compartido se queda sin NIF, que es justo el accidente contra el que protegen.
    /// </summary>
    internal static LegalEntityOptions LegalEntity()
    {
        var legal = new LegalEntityOptions();
        legal.FillBlanksFrom(VersionedLegalEntity.Value);
        return legal;
    }

    internal static ProductEmailTemplate Template() =>
        new(Options.Create(Account), Options.Create(LegalEntity()));

    /// <summary>Cada correo del producto, con el nombre con el que aparece en el inventario de la KB.</summary>
    internal static IReadOnlyList<(string Slug, string Nombre, MimeMessage Message)> All()
    {
        var template = Template();

        return
        [
            ("invitacion-a-workspace", "Invitación a Workspace",
                InvitationEmailComposer.Compose(template, new InvitationEmail(
                    "vecino@ejemplo.com",
                    "Finca El Olivar",
                    "Antonio",
                    "https://app.terrenario.com/invitations/token-en-claro"))),

            ("baja-de-workspace", "Baja de Workspace",
                WorkspaceLifecycleEmailComposer.ComposeWorkspaceClosed(template, new WorkspaceClosedEmail(
                    "lucia@ejemplo.com",
                    "Finca El Olivar",
                    "Antonio",
                    "https://app.terrenario.com/reactivations/token-en-claro"))),

            ("solicitud-de-reactivacion", "Solicitud de traspaso y reactivación",
                WorkspaceLifecycleEmailComposer.ComposeReactivationRequested(template, new ReactivationRequestedEmail(
                    "antonio@ejemplo.com",
                    "Finca El Olivar",
                    "Lucía",
                    "https://app.terrenario.com/reactivations"))),

            ("alerta-disparada", "Alerta de operación disparada",
                AlertEmailComposer.ComposeFiring(template, "operacion@ejemplo.com", new AlertVerdict(
                    "login_error_rate",
                    AlertSeverity.High,
                    IsFiring: true,
                    "12,4 % de errores de login en los últimos 30 minutos (umbral 5 %)."))),

            ("alerta-resuelta", "Alerta de operación resuelta",
                AlertEmailComposer.ComposeResolved(template, "operacion@ejemplo.com", new AlertVerdict(
                    "login_error_rate",
                    AlertSeverity.High,
                    IsFiring: false,
                    "0,8 % de errores de login en los últimos 30 minutos (umbral 5 %)."),
                    TimeSpan.FromMinutes(42))),

            ("canal-de-feedback", "Sugerencia o incidencia del usuario",
                FeedbackEmailComposer.Compose(template, new FeedbackEmail
                {
                    ToEmail = "operacion@ejemplo.com",
                    Kind = FeedbackKinds.Incident,
                    Message =
                        "Al guardar una labor con dos terrenos me dice que falta la temporada, "
                        + "pero la tengo activa.",
                    ReporterDisplayName = "Antonio",
                    ReporterEmail = "antonio@ejemplo.com",
                    Context = new FeedbackContext(
                        "v0.6.0-hito-f",
                        "/app/diario",
                        "3f8c1d9a4b2e4f6a8c0d2e4f6a8c0d2e",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0")
                }))
        ];
    }
}
