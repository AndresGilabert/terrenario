using MimeKit;
using Terrenario.Api.Infrastructure.Email;

namespace Terrenario.Api.Infrastructure.Feedback;

/// <summary>
/// MVP-711 — El sexto correo del producto, compuesto con la plantilla común
/// (<see cref="ProductEmailTemplate"/>) como los otros cinco.
///
/// No es una formalidad: <c>MVP-715</c> unificó la composición justo porque cada correo nuevo tendía
/// a escribirse su propio marcado, y el primero que apareciera fuera de la plantilla reabriría el
/// problema entero —pie legal ausente, escapado a cargo del emisor y una maquetación distinta—.
/// Aquí, además, el cuerpo contiene <b>texto que escribe una persona</b>, que es exactamente el caso
/// en el que olvidarse de escapar duele.
///
/// Sin llamada a la acción, como los avisos de alerta: la respuesta a un reporte es leerlo y
/// contestar, no pulsar un enlace.
/// </summary>
public static class FeedbackEmailComposer
{
    private const string Reason =
        "esta dirección está configurada como buzón del canal de sugerencias e incidencias de "
        + "Terrenario";

    private const string OptOut =
        "Para dejar de recibirlos, retira la dirección de la configuración «Feedback:Recipient» del "
        + "despliegue.";

    /// <summary>Caracteres del mensaje que caben en el asunto antes de recortarlo.</summary>
    private const int SubjectExcerptLength = 70;

    public static MimeMessage Compose(ProductEmailTemplate template, FeedbackEmail feedback)
    {
        var etiqueta = feedback.Kind == FeedbackKinds.Incident ? "Incidencia" : "Sugerencia";

        return template.Compose(new ProductEmailContent
        {
            ToEmail = feedback.ToEmail,
            Subject = $"[Terrenario] {etiqueta}: {SubjectExcerpt(feedback.Message)}",
            Heading = $"{etiqueta} de {feedback.ReporterDisplayName}",
            Paragraphs = [feedback.Message],
            Notes = TechnicalContext(feedback),
            Reason = Reason,
            OptOut = OptOut
        });
    }

    /// <summary>
    /// El contexto técnico, en el pie del cuerpo y en cuerpo pequeño: quien lee el reporte quiere
    /// primero lo que le han contado, y solo después los datos para reproducirlo.
    ///
    /// Cada línea se emite <b>siempre</b>, incluida la de «no hubo petición fallida». Un hueco en
    /// blanco no distingue «no pasó» de «no se pudo capturar», y esa diferencia cambia por dónde se
    /// empieza a mirar.
    /// </summary>
    private static IReadOnlyList<string> TechnicalContext(FeedbackEmail feedback)
    {
        var context = feedback.Context;

        return
        [
            $"Contexto técnico · Enviado por {feedback.ReporterDisplayName} <{feedback.ReporterEmail}>.",
            $"Versión desplegada: {context.AppVersion}.",
            $"Ruta desde la que se envía: {Presented(context.Path)}.",
            context.LastFailedRequestId is { Length: > 0 } requestId
                ? $"Última petición fallida (X-Request-Id): {requestId}."
                : "Última petición fallida: ninguna registrada en esta sesión.",
            $"Navegador: {Presented(context.UserAgent)}."
        ];
    }

    private static string Presented(string? value) =>
        value is { Length: > 0 } present ? present : "no disponible";

    /// <summary>
    /// Un extracto del mensaje en el asunto, para poder triar la bandeja sin abrir cada correo.
    ///
    /// Se colapsan los espacios y saltos de línea antes de recortar. Es legibilidad, pero también la
    /// defensa que corresponde: un salto de línea dentro de una cabecera es la forma clásica de
    /// inyectar otras, y aunque MimeKit ya codifica el valor, la entrada de una cabecera nunca debería
    /// salir de un campo de texto libre sin normalizar.
    /// </summary>
    private static string SubjectExcerpt(string message)
    {
        var singleLine = string.Join(' ', message.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return singleLine.Length <= SubjectExcerptLength
            ? singleLine
            : singleLine[..SubjectExcerptLength].TrimEnd() + "…";
    }
}
