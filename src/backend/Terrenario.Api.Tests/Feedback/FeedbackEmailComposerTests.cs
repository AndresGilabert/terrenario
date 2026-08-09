using FluentAssertions;
using Terrenario.Api.Infrastructure.Feedback;
using Terrenario.Api.Tests.Emails;

namespace Terrenario.Api.Tests.Feedback;

/// <summary>
/// MVP-711 (HU-2, CA-2) — Lo específico del correo del canal. Lo transversal —pie legal, motivo del
/// envío, texto plano y cero recursos remotos— ya lo cubre <c>ProductEmailInventoryTests</c> sobre el
/// inventario entero, y por eso este correo entra en <c>ProductEmailCatalog</c>.
/// </summary>
public class FeedbackEmailComposerTests
{
    private static FeedbackEmail Report(
        string kind = FeedbackKinds.Incident,
        string message = "Al guardar una labor me dice que falta la temporada.",
        string? path = "/app/diario",
        string? requestId = "3f8c1d9a4b2e4f6a8c0d2e4f6a8c0d2e") => new()
        {
            ToEmail = "operacion@ejemplo.com",
            Kind = kind,
            Message = message,
            ReporterDisplayName = "Antonio",
            ReporterEmail = "antonio@ejemplo.com",
            Context = new FeedbackContext("v0.6.0-hito-f", path, requestId, "Mozilla/5.0 Chrome/128.0")
        };

    private static (string Html, string Text) Compose(FeedbackEmail report)
    {
        var message = FeedbackEmailComposer.Compose(ProductEmailCatalog.Template(), report);

        // Mismo criterio que en el inventario: los cuerpos son anulables en MimeKit y aqui su ausencia
        // es un fallo del compositor, asi que se dice cual falta en vez de silenciarlo con `!`.
        return (
            message.HtmlBody ?? throw new InvalidOperationException("El reporte compuesto no tiene cuerpo HTML."),
            message.TextBody ?? throw new InvalidOperationException("El reporte compuesto no tiene cuerpo en texto."));
    }

    [Fact]
    public void Deberia_LlevarElContextoTecnicoCompleto()
    {
        var (html, text) = Compose(Report());

        // HU-2: que baste para reproducirlo sin una conversación de ida y vuelta. Se comprueba en las
        // dos versiones porque quien triaja la bandeja puede estar leyendo cualquiera de ellas.
        foreach (var body in new[] { html, text })
        {
            body.Should().Contain("v0.6.0-hito-f");
            body.Should().Contain("/app/diario");
            body.Should().Contain("3f8c1d9a4b2e4f6a8c0d2e4f6a8c0d2e");
            body.Should().Contain("Chrome/128.0");
            body.Should().Contain("antonio@ejemplo.com");
        }
    }

    [Fact]
    public void Deberia_DecirQueNoHuboPeticionFallida_EnLugarDeDejarElHueco()
    {
        var (_, text) = Compose(Report(requestId: null));

        // Un hueco en blanco no distingue «no falló nada» de «no se pudo capturar», y esa diferencia
        // cambia por dónde se empieza a mirar.
        text.Should().Contain("ninguna registrada en esta sesión");
    }

    [Fact]
    public void Deberia_DistinguirIncidenciaDeSugerencia_EnElAsuntoYEnElTitular()
    {
        var incidencia = FeedbackEmailComposer.Compose(
            ProductEmailCatalog.Template(), Report(FeedbackKinds.Incident));
        var sugerencia = FeedbackEmailComposer.Compose(
            ProductEmailCatalog.Template(), Report(FeedbackKinds.Suggestion, "Estaría bien un buscador."));

        incidencia.Subject.Should().StartWith("[Terrenario] Incidencia:");
        sugerencia.Subject.Should().StartWith("[Terrenario] Sugerencia:");
        incidencia.HtmlBody.Should().Contain("Incidencia de Antonio");
    }

    [Fact]
    public void Deberia_ResumirElMensajeEnElAsunto_SinSaltosDeLinea()
    {
        var largo = string.Join('\n', Enumerable.Repeat("Una línea del reporte que ocupa lo suyo.", 5));

        var message = FeedbackEmailComposer.Compose(ProductEmailCatalog.Template(), Report(message: largo));

        // Un asunto vacío es un fallo del compositor tan real como uno mal formado, y el `Subject` de
        // MimeKit es anulable: se comprueba antes de mirarlo, para que el caso salga como lo que es y
        // no como una referencia nula en la primera aserción.
        var asunto = message.Subject ?? throw new InvalidOperationException("El reporte no tiene asunto.");

        // Un salto de línea dentro de una cabecera es la forma clásica de inyectar otras. MimeKit ya
        // codifica el valor, pero el asunto no debería salir sin normalizar de un campo de texto libre.
        asunto.Should().NotContain("\n").And.NotContain("\r");
        asunto.Length.Should().BeLessThan(120);
        asunto.Should().EndWith("…");
    }

    [Fact]
    public void Deberia_NoLlevarNadaDeLaExplotacion()
    {
        // El contexto responde a «dónde estaba» y «qué petición falló», y a nada más. Si algún día
        // alguien añade el Workspace o la temporada «porque ayuda», esto lo para: un canal de soporte
        // no es una vía lateral para sacar datos operativos a un buzón de correo.
        //
        // El mensaje de ejemplo se elige sin esas palabras a propósito: lo que se vigila es lo que
        // añade el sistema, no lo que decida escribir la persona en su reporte.
        var (html, text) = Compose(Report(message: "Al guardar una labor me dice que falta un dato."));

        foreach (var body in new[] { html, text })
        {
            body.Should().NotContain("Workspace");
            body.Should().NotContain("workspace_id");
            body.Should().NotContain("temporada");
        }
    }

    [Fact]
    public void Deberia_EscaparElTextoQueEscribeLaPersona()
    {
        // El mensaje es texto libre y llega tal cual: es el caso en el que olvidarse de escapar duele.
        // Escapa la plantilla, no este composer, y esto lo comprueba desde fuera.
        var (html, text) = Compose(Report(message: "Falla al pulsar <b>Guardar</b> & salir"));

        html.Should().NotContain("<b>Guardar</b>");
        html.Should().Contain("&lt;b&gt;Guardar&lt;/b&gt;");
        text.Should().Contain("<b>Guardar</b>");
    }
}
