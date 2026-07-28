using MimeKit;
using System.Net;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Infrastructure.Email;

/// <summary>
/// Construye los correos del ciclo de vida del Workspace (MVP-206). Separado del transporte para
/// poder probarlo sin levantar un servidor SMTP, igual que el de invitaciones (MVP-103).
/// </summary>
public static class WorkspaceLifecycleEmailComposer
{
    public static MimeMessage ComposeWorkspaceClosed(EmailOptions options, WorkspaceClosedEmail message)
    {
        var mail = NewMessage(options, message.ToEmail);
        mail.Subject = $"Se ha dado de baja {message.WorkspaceName} en Terrenario";

        var opening = message.ClosedByDisplayName is { Length: > 0 } author
            ? $"{author} ha dado de baja el Workspace {message.WorkspaceName}, del que formas parte."
            : $"Se ha dado de baja el Workspace {message.WorkspaceName}, del que formas parte.";

        const string explanation =
            "No se ha borrado nada: los datos siguen guardados. Si todavía lo necesitas, puedes pedir "
            + "que te lo traspasen y se reactive; la decisión final es de quien lo dio de baja.";

        mail.Body = new BodyBuilder
        {
            TextBody = $"""
                {opening}

                {explanation}

                Solicita su traspaso y reactivación desde este enlace:
                {message.ReactivationUrl}

                El enlace es de un solo uso y caduca en unos días.
                """,
            // El nombre del Workspace y el de quien da de baja los escriben personas: se escapan siempre.
            HtmlBody = $"""
                <p>{WebUtility.HtmlEncode(opening)}</p>
                <p>{explanation}</p>
                <p><a href="{WebUtility.HtmlEncode(message.ReactivationUrl)}">Solicitar traspaso y reactivación</a></p>
                <p>El enlace es de un solo uso y caduca en unos días.</p>
                """
        }.ToMessageBody();

        return mail;
    }

    public static MimeMessage ComposeReactivationRequested(
        EmailOptions options,
        ReactivationRequestedEmail message)
    {
        var mail = NewMessage(options, message.ToEmail);
        mail.Subject = $"Piden recuperar {message.WorkspaceName} en Terrenario";

        var opening =
            $"{message.RequesterDisplayName} solicita que le traspases {message.WorkspaceName} y se "
            + "reactive. Como fuiste quien lo dio de baja, la decisión es tuya.";

        mail.Body = new BodyBuilder
        {
            TextBody = $"""
                {opening}

                Autoriza o rechaza la solicitud desde tu cuenta:
                {message.AuthorizationsUrl}
                """,
            HtmlBody = $"""
                <p>{WebUtility.HtmlEncode(opening)}</p>
                <p><a href="{WebUtility.HtmlEncode(message.AuthorizationsUrl)}">Revisar la solicitud</a></p>
                """
        }.ToMessageBody();

        return mail;
    }

    private static MimeMessage NewMessage(EmailOptions options, string toEmail)
    {
        var mail = new MimeMessage();
        mail.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        mail.To.Add(MailboxAddress.Parse(toEmail));
        return mail;
    }
}
