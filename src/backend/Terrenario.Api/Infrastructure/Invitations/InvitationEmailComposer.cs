using MimeKit;
using MimeKit.Text;
using System.Net;

namespace Terrenario.Api.Infrastructure.Invitations;

/// <summary>
/// Construye el correo de invitación. Se mantiene separado del transporte para poder probarlo
/// sin levantar un servidor SMTP.
/// </summary>
public static class InvitationEmailComposer
{
    public static MimeMessage Compose(EmailOptions options, InvitationEmail invitation)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        message.To.Add(MailboxAddress.Parse(invitation.ToEmail));
        message.Subject = $"Te han invitado a {invitation.WorkspaceName} en Terrenario";

        message.Body = new BodyBuilder
        {
            TextBody = BuildTextBody(invitation),
            HtmlBody = BuildHtmlBody(invitation)
        }.ToMessageBody();

        return message;
    }

    private static string BuildTextBody(InvitationEmail invitation) =>
        $"""
        {Invitation(invitation)}

        Entra con tu cuenta de Google desde este enlace:
        {invitation.AcceptUrl}

        El enlace es de un solo uso y caduca en unos días. Si no esperabas esta invitación,
        puedes ignorar este mensaje.
        """;

    // El nombre del Workspace y el de quien invita los escriben personas: se escapan siempre.
    private static string BuildHtmlBody(InvitationEmail invitation) =>
        $"""
        <p>{WebUtility.HtmlEncode(Invitation(invitation))}</p>
        <p><a href="{WebUtility.HtmlEncode(invitation.AcceptUrl)}">Unirme al Workspace</a></p>
        <p>El enlace es de un solo uso y caduca en unos días.
        Si no esperabas esta invitación, puedes ignorar este mensaje.</p>
        """;

    private static string Invitation(InvitationEmail invitation) =>
        invitation.InviterDisplayName is { Length: > 0 } inviter
            ? $"{inviter} te invita a colaborar en {invitation.WorkspaceName} dentro de Terrenario."
            : $"Te invitan a colaborar en {invitation.WorkspaceName} dentro de Terrenario.";
}
