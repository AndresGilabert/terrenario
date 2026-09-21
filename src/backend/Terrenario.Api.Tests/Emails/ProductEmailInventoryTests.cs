using FluentAssertions;
using System.Text.RegularExpressions;
using Terrenario.Api.Infrastructure.Email;

namespace Terrenario.Api.Tests.Emails;

/// <summary>
/// MVP-715 — Lo que <b>todo</b> correo del producto tiene que cumplir, comprobado sobre el
/// inventario entero en vez de correo a correo (HU-2: que ningún correo nuevo salga sin ello).
///
/// Estas pruebas son la parte de CA-5 que sí puede automatizarse. La otra parte —cómo se ve en un
/// cliente real— la hace una persona sobre los ficheros que deja
/// <see cref="ProductEmailPreviewTests"/>.
/// </summary>
public class ProductEmailInventoryTests
{
    public static TheoryData<string> Inventario()
    {
        var data = new TheoryData<string>();
        foreach (var (slug, _, _) in ProductEmailCatalog.All()) data.Add(slug);
        return data;
    }

    private static (string Html, string Text) BodiesOf(string slug)
    {
        var message = ProductEmailCatalog.All().Single(email => email.Slug == slug).Message;

        // `HtmlBody` y `TextBody` son anulables en MimeKit, y un correo del producto sin cuerpo es un
        // defecto, no un caso a tolerar. Se comprueba aqui en vez de callar la nulabilidad con `!`:
        // asi el fallo dice cual es el correo y que le falta, en lugar de salir como una referencia
        // nula tres lineas mas abajo, en la asercion que solo queria mirar el contenido.
        return (
            message.HtmlBody ?? throw new InvalidOperationException($"El correo '{slug}' no tiene cuerpo HTML."),
            message.TextBody ?? throw new InvalidOperationException($"El correo '{slug}' no tiene cuerpo en texto."));
    }

    [Fact]
    public void ElInventario_Deberia_TenerTodosLosCorreosDelProducto()
    {
        // Si esto cambia, cambia también `docs/06-integraciones/correos-del-producto.md`. Eran cinco
        // en `MVP-715` —donde el spec daba «al menos cuatro»—, seis desde `MVP-711` (canal de
        // sugerencias e incidencias) y ocho desde `MKT-101` (resumen operativo diario y semanal).
        ProductEmailCatalog.All().Select(email => email.Slug).Should().BeEquivalentTo(
        [
            "invitacion-a-workspace",
            "baja-de-workspace",
            "solicitud-de-reactivacion",
            "alerta-disparada",
            "alerta-resuelta",
            "canal-de-feedback",
            "resumen-operativo-diario",
            "resumen-operativo-semanal"
        ]);
    }

    [Theory]
    [MemberData(nameof(Inventario))]
    public void CadaCorreo_Deberia_IdentificarAlResponsableDelTratamiento(string slug)
    {
        // Arrange — la identidad real, la misma que publican la Política de Privacidad y los Términos.
        var legal = ProductEmailCatalog.LegalEntity();
        var (html, text) = BodiesOf(slug);

        // Assert — CA-3, en las dos versiones: quién es el responsable, dónde está y dónde se
        // ejercen los derechos.
        foreach (var body in new[] { html, text })
        {
            body.Should().Contain(legal.LegalName);
            body.Should().Contain(legal.TaxId);
            body.Should().Contain(legal.Address);
            body.Should().Contain(legal.PrivacyEmail);
            body.Should().Contain(legal.PrivacyPolicyUrl);
        }
    }

    [Theory]
    [MemberData(nameof(Inventario))]
    public void CadaCorreo_Deberia_DecirPorQueSeEnvia(string slug)
    {
        var (html, text) = BodiesOf(slug);

        // CA-3 — el motivo del envío, que es lo que separa un aviso legítimo de correo no deseado.
        html.Should().Contain("Recibes este correo porque");
        text.Should().Contain("Recibes este correo porque");
    }

    [Theory]
    [MemberData(nameof(Inventario))]
    public void CadaCorreo_Deberia_TenerVersionEnTextoPlano(string slug)
    {
        var (html, text) = BodiesOf(slug);

        // CA-4 — no basta con que exista: tiene que decir lo mismo, así que se comprueba que lleva
        // el titular del cuerpo y el pie, no solo que no está vacía.
        text.Should().NotBeNullOrWhiteSpace();
        text.Should().Contain("TERRENARIO");
        text.Should().NotContain("<p");
        text.Should().NotContain("<div");
        html.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [MemberData(nameof(Inventario))]
    public void CadaCorreo_Deberia_ViajarSinRecursosRemotos(string slug)
    {
        var (html, _) = BodiesOf(slug);

        // CA-6 — ni imágenes, ni tipografías, ni hojas de estilo externas. Cualquiera de ellas
        // delataría el momento de apertura al servidor que la sirve, y además dejaría el correo
        // incompleto en un cliente que bloquea remotos.
        html.Should().NotContain("<img", "una imagen remota convierte abrir el correo en una señal");
        html.Should().NotContain("<link");
        html.Should().NotContain("<script");
        html.Should().NotContain("@import");
        html.Should().NotContain("@font-face");
        html.Should().NotContain("background-image");
        html.Should().NotContain("url(");

        // La red de seguridad de verdad: el único atributo que puede traer una URL es `href`. Si
        // aparece cualquier otro (`src`, `background`, `poster`…), es un recurso que se descarga.
        Regex.Matches(html, """\s(?<attr>[a-zA-Z-]+)\s*=\s*"[^"]*(?:https?:|//)""")
            .Select(match => match.Groups["attr"].Value)
            .Should().OnlyContain(attr => attr == "href");
    }

    [Theory]
    [MemberData(nameof(Inventario))]
    public void CadaCorreo_Deberia_LlevarRemitenteYDestinatario(string slug)
    {
        var message = ProductEmailCatalog.All().Single(email => email.Slug == slug).Message;

        message.From.Mailboxes.Single().Address.Should().Be(ProductEmailCatalog.Account.FromAddress);
        message.To.Mailboxes.Should().ContainSingle();
        message.Subject.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void LaPlantilla_Deberia_EscaparLoQueEscribenLasPersonas()
    {
        // Arrange — el nombre del Workspace es texto libre y llega tal cual a la plantilla.
        var message = ProductEmailCatalog.Template().Compose(new ProductEmailContent
        {
            ToEmail = "vecino@ejemplo.com",
            Subject = "Prueba",
            Heading = "<script>alert('x')</script>",
            Paragraphs = ["Nombre: <b>Antonio</b> & Lucía"],
            Action = new EmailAction("Entrar", "https://app.terrenario.com/x?a=1&b=2"),
            Reason = "es una prueba"
        });

        // Assert — el escapado es de la plantilla, no de cada emisor: por eso se prueba aquí.
        message.HtmlBody.Should().NotContain("<script>");
        message.HtmlBody.Should().Contain("&lt;script&gt;");
        message.HtmlBody.Should().Contain("&amp;");
        // El texto plano no se escapa: escaparlo lo haría ilegible sin ganar nada.
        message.TextBody.Should().Contain("<script>alert('x')</script>");
    }

    [Fact]
    public void LaIdentidadLegal_Deberia_SalirDelFicheroCompartidoConLasPaginasLegales()
    {
        // El recurso incrustado es el mismo `legal-entity.json` del que se alimentan la Política de
        // Privacidad y los Términos. Si alguien reescribe el NIF en C#, esto deja de cuadrar.
        var versioned = VersionedLegalEntity.Value;

        versioned.MissingFieldsForEmailFooter().Should().BeEmpty();
        versioned.LegalName.Should().Be("Andrés Gilabert Sánchez");
        versioned.TaxId.Should().Be("21.679.361-K");
    }

    [Fact]
    public void LaConfiguracion_Deberia_PoderSobreescribirUnCampoSinBorrarElResto()
    {
        // Arrange — un despliegue que solo cambia el titular.
        var legal = new LegalEntityOptions { LegalName = "Cooperativa de ejemplo, S. Coop." };

        // Act
        legal.FillBlanksFrom(VersionedLegalEntity.Value);

        // Assert — mismo comportamiento que `resolveLegalEntity` en el cliente.
        legal.LegalName.Should().Be("Cooperativa de ejemplo, S. Coop.");
        legal.TaxId.Should().Be(VersionedLegalEntity.Value.TaxId);
    }
}
