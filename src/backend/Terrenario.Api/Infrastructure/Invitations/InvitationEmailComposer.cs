using MimeKit;
using Terrenario.Api.Infrastructure.Email;

namespace Terrenario.Api.Infrastructure.Invitations;

/// <summary>
/// Construye el correo de invitación. Se mantiene separado del transporte para poder probarlo
/// sin levantar un servidor SMTP.
///
/// MVP-715 — Ya no arma marcado: aporta el contenido y delega la maquetación en
/// <see cref="ProductEmailTemplate"/>. Es el correo más delicado del producto porque va a alguien
/// que <b>todavía no tiene cuenta</b>: si no se reconoce como legítimo, no hay historia que contar.
/// </summary>
public static class InvitationEmailComposer
{
    public static MimeMessage Compose(ProductEmailTemplate template, InvitationEmail invitation) =>
        template.Compose(new ProductEmailContent
        {
            ToEmail = invitation.ToEmail,
            Subject = $"Te han invitado a {invitation.WorkspaceName} en Terrenario",
            Heading = $"Te invitan a {invitation.WorkspaceName}",
            Paragraphs =
            [
                Invitation(invitation),
                "Terrenario es un cuaderno de campo para llevar el día a día de una explotación "
                + "agrícola. Se entra con una Cuenta de Google, desde el enlace de abajo."
            ],
            Action = new EmailAction("Unirme al Workspace", invitation.AcceptUrl),
            Notes =
            [
                // MVP-712 — Este correo llega a una dirección que puede no ser de Gmail, y es el
                // primer contacto con el producto: si aquí se lee «cuenta de Google» como «Gmail»,
                // no hay segunda pantalla donde desmentirlo. La invitación solo se acepta desde la
                // dirección invitada, así que sin esta frase quien no tenga Cuenta de Google en ella
                // se queda en un callejón sin salida (`P-089`).
                //
                // La dirección va en texto plano, no como enlace: la plantilla admite una sola
                // llamada a la acción —que es aceptar la invitación— y añadir un segundo botón
                // compitiendo con ella dejaría el correo sin acción principal.
                "Esta misma dirección sirve, sea o no de Gmail: solo tiene que estar dada de alta "
                + "como Cuenta de Google. Si todavía no lo está, puedes darla de alta gratis en "
                + "https://accounts.google.com/signup y aceptar la invitación con ella.",
                "El enlace es de un solo uso y caduca en unos días.",
                "Si no esperabas esta invitación, puedes ignorar este mensaje: sin aceptarla no se "
                + "crea ninguna cuenta a tu nombre y la invitación caduca sola."
            ],
            Reason =
                $"alguien te ha invitado a colaborar en «{invitation.WorkspaceName}» dentro de "
                + "Terrenario y facilitó esta dirección",
            // Es el único correo del producto que llega a quien no es usuario, así que es el único
            // en el que la baja no puede ser «sal del Workspace»: la vía real es el derecho de
            // supresión sobre la invitación, que se ejerce en la dirección del pie.
            OptOut =
                "No estás suscrito a ninguna lista y no volveremos a escribirte si no aceptas. Para "
                + "que tu dirección deje de constar, escribe a la dirección de derechos de aquí abajo."
        });

    private static string Invitation(InvitationEmail invitation) =>
        invitation.InviterDisplayName is { Length: > 0 } inviter
            ? $"{inviter} te invita a colaborar en {invitation.WorkspaceName} dentro de Terrenario."
            : $"Te invitan a colaborar en {invitation.WorkspaceName} dentro de Terrenario.";
}
