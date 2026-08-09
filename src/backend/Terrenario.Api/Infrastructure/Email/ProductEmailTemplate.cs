using MimeKit;
using System.Net;
using System.Text;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Infrastructure.Email;

/// <summary>Llamada a la acción principal del correo. Solo una: si hay dos, no hay ninguna.</summary>
public sealed record EmailAction(string Label, string Url);

/// <summary>
/// MVP-715 — Lo que cada correo aporta a la plantilla común. Todo es texto plano: la plantilla es la
/// única que sabe de HTML, y por tanto la única responsable de escapar. Ningún emisor construye
/// marcado, así que ninguno puede olvidarse de escapar un nombre escrito por una persona.
/// </summary>
public sealed record ProductEmailContent
{
    public required string ToEmail { get; init; }

    public required string Subject { get; init; }

    /// <summary>Titular del cuerpo. Dice de qué va el correo sin tener que leerlo entero.</summary>
    public required string Heading { get; init; }

    /// <summary>Cuerpo, un párrafo por elemento.</summary>
    public required IReadOnlyList<string> Paragraphs { get; init; }

    /// <summary>Sin acción en los correos que solo informan (las alertas de operación).</summary>
    public EmailAction? Action { get; init; }

    /// <summary>Advertencias secundarias: caducidad del enlace, qué hacer si no lo esperabas…</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];

    /// <summary>
    /// Motivo del envío, en segunda persona y sin el «Recibes este correo porque», que lo pone la
    /// plantilla. Obligatorio: es el criterio de contenido legal que <c>P-001</c> pedía garantizar
    /// desde la plantilla y no correo a correo (CA-3).
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Cómo dejar de recibirlo, cuando existe forma de hacerlo. En los avisos imprescindibles del
    /// servicio se dice justo eso, en vez de callarlo: la transparencia del art. 13 no es solo
    /// ofrecer una baja, es no dar a entender que la hay cuando no la hay.
    /// </summary>
    public string? OptOut { get; init; }
}

/// <summary>
/// MVP-715 — <b>La única forma de componer un correo del producto.</b>
///
/// El transporte ya era común desde <c>MVP-206</c> (<see cref="SmtpMailer"/>, ADR-0010); lo que
/// seguía siendo ad-hoc era la composición: cada flujo se escribía su mensaje, con su propio tono y
/// sin pie legal. Esta plantilla fija la estructura —cabecera, cuerpo, llamada a la acción y pie
/// legal— para que un correo nuevo no pueda salir sin identificar al responsable ni decir por qué
/// se envía (HU-2).
///
/// <b>Sin recursos remotos</b> (CA-6): ni imágenes, ni tipografías, ni hojas de estilo externas. Dos
/// motivos y los dos importan. El correo es la única vía del producto hacia alguien que todavía no
/// tiene cuenta, y un cliente que bloquea imágenes dejaría la invitación en un hueco gris; además,
/// cualquier recurso remoto delata al servidor que lo aloja el momento exacto en que se abre el
/// mensaje, que es seguimiento de apertura aunque no se haya pedido. La cabecera es texto y el
/// aspecto sale de estilos en línea, que es lo único que respetan los clientes de correo.
/// </summary>
public sealed class ProductEmailTemplate(
    IOptions<EmailOptions> emailOptions,
    IOptions<LegalEntityOptions> legalOptions)
{
    private readonly EmailOptions _email = emailOptions.Value;
    private readonly LegalEntityOptions _legal = legalOptions.Value;

    // Paleta y tipografía en línea. La familia son fuentes del sistema a propósito: pedir una
    // tipografía web sería exactamente el recurso remoto que CA-6 prohíbe.
    private const string FontStack =
        "-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif";
    private const string TextColor = "#1c1917";
    private const string MutedColor = "#57534e";
    private const string BorderColor = "#e7e5e4";
    private const string AccentColor = "#3f6212";

    public MimeMessage Compose(ProductEmailContent content)
    {
        var mail = new MimeMessage();
        mail.From.Add(new MailboxAddress(_email.FromName, _email.FromAddress));
        mail.To.Add(MailboxAddress.Parse(content.ToEmail));
        mail.Subject = content.Subject;

        mail.Body = new BodyBuilder
        {
            // Las dos versiones salen del mismo contenido: no pueden decir cosas distintas (CA-4).
            TextBody = RenderText(content),
            HtmlBody = RenderHtml(content)
        }.ToMessageBody();

        return mail;
    }

    /// <summary>
    /// Versión en texto plano. No es un descarte del HTML: hay quien lee el correo así por elección
    /// o por lector de pantalla, y es lo que ve un cliente que no renderiza HTML.
    /// </summary>
    private string RenderText(ProductEmailContent content)
    {
        var text = new StringBuilder();

        text.AppendLine(_email.FromName.ToUpperInvariant());
        text.AppendLine();
        text.AppendLine(content.Heading);
        text.AppendLine(new string('=', content.Heading.Length));

        foreach (var paragraph in content.Paragraphs)
        {
            text.AppendLine();
            text.AppendLine(paragraph);
        }

        if (content.Action is { } action)
        {
            text.AppendLine();
            text.AppendLine($"{action.Label}:");
            text.AppendLine(action.Url);
        }

        foreach (var note in content.Notes)
        {
            text.AppendLine();
            text.AppendLine(note);
        }

        text.AppendLine();
        text.AppendLine("--");
        text.AppendLine($"Recibes este correo porque {content.Reason}.");

        if (content.OptOut is { Length: > 0 } optOut)
            text.AppendLine(optOut);

        text.AppendLine();
        text.AppendLine($"Responsable del tratamiento: {_legal.LegalName} (NIF {_legal.TaxId}).");
        text.AppendLine(_legal.Address);
        text.AppendLine($"Política de privacidad: {_legal.PrivacyPolicyUrl}");
        text.AppendLine($"Derechos de protección de datos: {_legal.PrivacyEmail}");

        return text.ToString();
    }

    private string RenderHtml(ProductEmailContent content)
    {
        var html = new StringBuilder();

        html.Append(
            $"""
            <div style="margin:0;padding:24px 12px;background-color:#fafaf9;font-family:{FontStack};">
              <div style="max-width:560px;margin:0 auto;background-color:#ffffff;border:1px solid {BorderColor};border-radius:8px;padding:24px;">
                <p style="margin:0 0 20px;font-size:13px;font-weight:600;letter-spacing:0.08em;text-transform:uppercase;color:{AccentColor};">{Escape(_email.FromName)}</p>
                <h1 style="margin:0 0 16px;font-size:20px;line-height:1.3;color:{TextColor};">{Escape(content.Heading)}</h1>

            """);

        foreach (var paragraph in content.Paragraphs)
            html.AppendLine(Paragraph(Escape(paragraph), TextColor, "15px"));

        if (content.Action is { } action)
        {
            var url = Escape(action.Url);

            html.AppendLine(
                $"""    <p style="margin:0 0 12px;"><a href="{url}" style="display:inline-block;padding:12px 20px;background-color:{AccentColor};color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;border-radius:6px;">{Escape(action.Label)}</a></p>""");

            // El enlace también en claro: los botones se rompen en más clientes de los que parece, y
            // el enlace visible deja además ver a dónde lleva antes de pulsarlo.
            html.AppendLine(
                $"""    <p style="margin:0 0 16px;font-size:13px;color:{MutedColor};word-break:break-all;">Si el botón no funciona, copia esta dirección en el navegador:<br />{url}</p>""");
        }

        foreach (var note in content.Notes)
            html.AppendLine(Paragraph(Escape(note), MutedColor, "13px"));

        html.AppendLine(
            $"""    <hr style="border:none;border-top:1px solid {BorderColor};margin:24px 0 16px;" />""");
        html.AppendLine($"""    <div style="font-size:12px;line-height:1.6;color:{MutedColor};">""");
        html.AppendLine(
            $"""      <p style="margin:0 0 8px;">Recibes este correo porque {Escape(content.Reason)}.{OptOutFragment(content)}</p>""");
        html.AppendLine(
            $"""      <p style="margin:0 0 8px;">Responsable del tratamiento: {Escape(_legal.LegalName)} (NIF {Escape(_legal.TaxId)}). {Escape(_legal.Address)}.</p>""");
        html.AppendLine(
            $"""      <p style="margin:0;"><a href="{Escape(_legal.PrivacyPolicyUrl)}" style="color:{MutedColor};">Política de privacidad</a> · Derechos de protección de datos: <a href="mailto:{Escape(_legal.PrivacyEmail)}" style="color:{MutedColor};">{Escape(_legal.PrivacyEmail)}</a></p>""");
        html.AppendLine("    </div>");
        html.AppendLine("  </div>");
        html.AppendLine("</div>");

        return html.ToString();
    }

    private static string OptOutFragment(ProductEmailContent content) =>
        content.OptOut is { Length: > 0 } optOut ? $" {Escape(optOut)}" : string.Empty;

    private static string Paragraph(string escapedText, string color, string size) =>
        $"""    <p style="margin:0 0 16px;font-size:{size};line-height:1.6;color:{color};">{escapedText}</p>""";

    /// <summary>
    /// Nombres de Workspace, nombres de personas y URLs con token: todo lo que entra en el marcado lo
    /// escribe alguien, así que se escapa sin excepciones y en un solo sitio.
    ///
    /// No se usa <see cref="WebUtility.HtmlEncode"/> porque convierte además cada acento en una
    /// entidad numérica (<c>Andr&amp;#233;s</c>): el correo viaja en UTF-8, así que eso no aporta
    /// nada y sí deja el HTML ilegible justo donde más se revisa, que es el pie legal. Se escapan
    /// los cinco caracteres que pueden cambiar el significado del marcado, incluidas las comillas
    /// porque hay valores que van dentro de un atributo.
    /// </summary>
    private static string Escape(string value) => value
        .Replace("&", "&amp;")   // el primero, o se escaparían los que vienen detrás
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'", "&#39;");
}
