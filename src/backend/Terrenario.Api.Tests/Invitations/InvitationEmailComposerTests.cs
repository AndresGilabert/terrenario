using FluentAssertions;
using Terrenario.Api.Infrastructure.Invitations;
using Terrenario.Api.Tests.Emails;

namespace Terrenario.Api.Tests.Invitations;

/// <summary>
/// Contenido propio del correo de invitación. Lo transversal —pie legal, motivo del envío, versión
/// en texto plano, ausencia de recursos remotos— se comprueba una sola vez para todo el inventario
/// en <c>ProductEmailInventoryTests</c> (MVP-715).
/// </summary>
public class InvitationEmailComposerTests
{
    private static InvitationEmail EmailFor(string? inviter = "Antonio") =>
        new("vecino@ejemplo.com", "Finca El Olivar", inviter,
            "https://app.terrenario.com/invitations/token-en-claro");

    [Fact]
    public void Deberia_ComponerElCorreo_Cuando_HayRemitenteYDestinatario()
    {
        // Act
        var message = InvitationEmailComposer.Compose(ProductEmailCatalog.Template(), EmailFor());

        // Assert
        message.From.ToString().Should().Contain("no-reply@terrenario.com");
        message.To.ToString().Should().Contain("vecino@ejemplo.com");
        message.Subject.Should().Be("Te han invitado a Finca El Olivar en Terrenario");
        message.TextBody.Should().Contain("Antonio");
        message.TextBody.Should().Contain("https://app.terrenario.com/invitations/token-en-claro");
        message.HtmlBody.Should().Contain("https://app.terrenario.com/invitations/token-en-claro");
    }

    [Fact]
    public void Deberia_OmitirQuienInvita_Cuando_LaSesionNoTraeNombre()
    {
        // Act
        var message = InvitationEmailComposer.Compose(
            ProductEmailCatalog.Template(),
            EmailFor(inviter: null));

        // Assert
        message.TextBody.Should().Contain("Te invitan a colaborar en Finca El Olivar");
    }

    [Fact]
    public void Deberia_EscaparElHtml_Cuando_ElNombreDelWorkspaceLlevaMarcado()
    {
        // Arrange — el nombre del Workspace lo escribe una persona
        var invitation = new InvitationEmail(
            "vecino@ejemplo.com",
            "<script>alert('x')</script>",
            null,
            "https://app.terrenario.com/invitations/token-en-claro");

        // Act
        var message = InvitationEmailComposer.Compose(ProductEmailCatalog.Template(), invitation);

        // Assert
        message.HtmlBody.Should().NotContain("<script>");
        message.HtmlBody.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public void Deberia_ExplicarLaSalida_Cuando_QuienLoRecibeNoEsUsuarioTodavia()
    {
        // Act
        var message = InvitationEmailComposer.Compose(ProductEmailCatalog.Template(), EmailFor());

        // Assert — es el único correo que llega a quien no tiene cuenta: no puede limitarse a decir
        // «sal del Workspace» como los demás (MVP-715, CA-3).
        message.TextBody.Should().Contain("No estás suscrito a ninguna lista");
        message.TextBody.Should().Contain("puedes ignorar este mensaje");
    }

    [Fact]
    public void Deberia_DecirQueLaDireccionInvitadaSirve_AunqueNoSeaDeGmail()
    {
        // Act
        var message = InvitationEmailComposer.Compose(ProductEmailCatalog.Template(), EmailFor());

        // Assert — MVP-712 (CA-4). Es el primer contacto con el producto y llega a una dirección que
        // puede no ser de Gmail: si aquí «cuenta de Google» se lee como «Gmail», no hay segunda
        // pantalla donde desmentirlo, porque la invitación solo se acepta desde esa dirección
        // (`P-089`). En las dos versiones, que no pueden decir cosas distintas.
        foreach (var body in new[] { message.TextBody, message.HtmlBody })
        {
            body.Should().Contain("Esta misma dirección sirve, sea o no de Gmail");
            body.Should().Contain("dada de alta como Cuenta de Google");
            body.Should().Contain("https://accounts.google.com/signup");
        }
    }

    [Fact]
    public void Deberia_DejarElAltaDeGoogleComoTexto_YNoComoSegundaLlamadaALaAccion()
    {
        // Act
        var message = InvitationEmailComposer.Compose(ProductEmailCatalog.Template(), EmailFor());

        // Assert — la plantilla admite una sola llamada a la acción, y es aceptar la invitación
        // (MVP-715). El alta se menciona en claro: un segundo botón compitiendo con el primero
        // dejaría el correo sin acción principal. Además, nada de Google se **descarga** al abrirlo.
        message.HtmlBody.Should().NotContain("href=\"https://accounts.google.com");
        message.HtmlBody.Should().Contain(
            "href=\"https://app.terrenario.com/invitations/token-en-claro\"");
    }
}
