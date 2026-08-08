using MimeKit;

namespace Terrenario.Api.Infrastructure.Email;

/// <summary>
/// Construye los correos del ciclo de vida del Workspace (MVP-206). Separado del transporte para
/// poder probarlo sin levantar un servidor SMTP, igual que el de invitaciones (MVP-103).
///
/// MVP-715 — La maquetación pasa a <see cref="ProductEmailTemplate"/>; aquí queda solo el qué se
/// dice. Los dos correos son avisos <b>imprescindibles</b> del servicio: informan de una decisión
/// que ya se ha tomado sobre datos de la persona, así que su «cómo dejar de recibirlo» no es una
/// baja, es dejar de tener relación con ese Workspace. Decirlo así es más honesto que ofrecer una
/// baja que no existe.
/// </summary>
public static class WorkspaceLifecycleEmailComposer
{
    private const string ServiceNotice =
        "Es un aviso imprescindible del servicio y no se puede desactivar: dejarás de recibir avisos "
        + "de este Workspace cuando dejes de formar parte de él.";

    public static MimeMessage ComposeWorkspaceClosed(
        ProductEmailTemplate template,
        WorkspaceClosedEmail message)
    {
        var opening = message.ClosedByDisplayName is { Length: > 0 } author
            ? $"{author} ha dado de baja el Workspace {message.WorkspaceName}, del que formas parte."
            : $"Se ha dado de baja el Workspace {message.WorkspaceName}, del que formas parte.";

        return template.Compose(new ProductEmailContent
        {
            ToEmail = message.ToEmail,
            Subject = $"Se ha dado de baja {message.WorkspaceName} en Terrenario",
            Heading = $"Se ha dado de baja {message.WorkspaceName}",
            Paragraphs =
            [
                opening,
                "No se ha borrado nada: los datos siguen guardados. Si todavía lo necesitas, puedes "
                + "pedir que te lo traspasen y se reactive; la decisión final es de quien lo dio de baja."
            ],
            Action = new EmailAction("Solicitar traspaso y reactivación", message.ReactivationUrl),
            Notes = ["El enlace es de un solo uso y caduca en unos días."],
            Reason =
                $"eras miembro del Workspace «{message.WorkspaceName}» en Terrenario y se ha dado de baja",
            OptOut = ServiceNotice
        });
    }

    public static MimeMessage ComposeReactivationRequested(
        ProductEmailTemplate template,
        ReactivationRequestedEmail message) =>
        template.Compose(new ProductEmailContent
        {
            ToEmail = message.ToEmail,
            Subject = $"Piden recuperar {message.WorkspaceName} en Terrenario",
            Heading = $"Piden recuperar {message.WorkspaceName}",
            Paragraphs =
            [
                $"{message.RequesterDisplayName} solicita que le traspases {message.WorkspaceName} y "
                + "se reactive. Como fuiste quien lo dio de baja, la decisión es tuya."
            ],
            Action = new EmailAction("Revisar la solicitud", message.AuthorizationsUrl),
            // El enlace lleva a su bandeja, no a una acción directa: autorizar exige entrar con su
            // cuenta, y conviene que el correo no dé a entender lo contrario.
            Notes = ["Para autorizar o rechazar tendrás que entrar con tu cuenta."],
            Reason =
                $"diste de baja el Workspace «{message.WorkspaceName}» en Terrenario y alguien pide "
                + "que se lo traspases",
            OptOut = ServiceNotice
        });
}
