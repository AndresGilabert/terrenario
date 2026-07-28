using FluentAssertions;
using Terrenario.Api.Infrastructure.Invitations;

namespace Terrenario.Api.Tests.Invitations;

public class InvitationEmailComposerTests
{
    private static readonly EmailOptions Options = new()
    {
        Host = "smtp.ejemplo.com",
        FromAddress = "no-reply@terrenario.com",
        FromName = "Terrenario"
    };

    private static InvitationEmail EmailFor(string? inviter = "Antonio") =>
        new("vecino@ejemplo.com", "Finca El Olivar", inviter,
            "https://app.terrenario.com/invitations/token-en-claro");

    [Fact]
    public void Deberia_ComponerElCorreo_Cuando_HayRemitenteYDestinatario()
    {
        // Act
        var message = InvitationEmailComposer.Compose(Options, EmailFor());

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
        var message = InvitationEmailComposer.Compose(Options, EmailFor(inviter: null));

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
        var message = InvitationEmailComposer.Compose(Options, invitation);

        // Assert
        message.HtmlBody.Should().NotContain("<script>");
        message.HtmlBody.Should().Contain("&lt;script&gt;");
    }
}
