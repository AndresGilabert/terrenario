using FluentAssertions;
using Terrenario.Api.Infrastructure.Email;
using Terrenario.Api.Tests.Emails;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests de composición de los correos del ciclo de vida del Workspace (MVP-206, CA-6). Verifican
/// que el enlace de reactivación viaja íntegro y que el nombre del Workspace —texto que escribe una
/// persona— se escapa en la parte HTML.
///
/// MVP-715 — Lo transversal (pie legal, motivo, texto plano, recursos remotos) se comprueba para
/// todo el inventario en <c>ProductEmailInventoryTests</c>, no aquí.
/// </summary>
public class WorkspaceLifecycleEmailComposerTests
{
    [Fact]
    public void BajaDeWorkspace_Deberia_LlevarDestinatarioYEnlace()
    {
        var message = WorkspaceLifecycleEmailComposer.ComposeWorkspaceClosed(
            ProductEmailCatalog.Template(),
            new WorkspaceClosedEmail(
                "lucia@ejemplo.com",
                "Finca El Olivar",
                "Antonio",
                "http://localhost:5173/reactivations/abc123"));

        message.To.Mailboxes.Single().Address.Should().Be("lucia@ejemplo.com");
        message.Subject.Should().Contain("Finca El Olivar");
        message.TextBody.Should().Contain("http://localhost:5173/reactivations/abc123");
        message.TextBody.Should().Contain("Antonio");
        message.HtmlBody.Should().Contain("http://localhost:5173/reactivations/abc123");
    }

    [Fact]
    public void BajaDeWorkspace_Deberia_EscaparElNombreEnHtml()
    {
        var message = WorkspaceLifecycleEmailComposer.ComposeWorkspaceClosed(
            ProductEmailCatalog.Template(),
            new WorkspaceClosedEmail(
                "lucia@ejemplo.com",
                "<script>alert(1)</script>",
                null,
                "http://localhost:5173/reactivations/abc123"));

        message.HtmlBody.Should().NotContain("<script>");
        message.HtmlBody.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public void SolicitudDeReactivacion_Deberia_ApuntarALaBandejaDeQuienDioDeBaja()
    {
        var message = WorkspaceLifecycleEmailComposer.ComposeReactivationRequested(
            ProductEmailCatalog.Template(),
            new ReactivationRequestedEmail(
                "antonio@ejemplo.com",
                "Finca El Olivar",
                "Lucía",
                "http://localhost:5173/reactivations"));

        message.To.Mailboxes.Single().Address.Should().Be("antonio@ejemplo.com");
        message.TextBody.Should().Contain("Lucía");
        message.TextBody.Should().Contain("http://localhost:5173/reactivations");
    }

    [Fact]
    public void LosAvisosDelCicloDeVida_Deberian_DecirQueNoSePuedenDesactivar()
    {
        // MVP-715 — Ofrecer una baja que no existe sería peor que no ofrecer ninguna: el aviso
        // informa de una decisión ya tomada sobre datos de la persona.
        var message = WorkspaceLifecycleEmailComposer.ComposeWorkspaceClosed(
            ProductEmailCatalog.Template(),
            new WorkspaceClosedEmail("lucia@ejemplo.com", "Finca El Olivar", null, "https://x/y"));

        message.TextBody.Should().Contain("aviso imprescindible del servicio");
    }
}
