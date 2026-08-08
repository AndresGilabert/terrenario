using FluentAssertions;
using System.IO;
using System.Text;

namespace Terrenario.Api.Tests.Emails;

/// <summary>
/// MVP-715 — Deja en disco el HTML y el texto plano de cada correo del inventario.
///
/// <b>Por qué es un test y no un script</b>: CA-5 exige revisar cada correo en un cliente real, y
/// eso lo hace una persona. Lo que puede hacer la suite es que esa persona no tenga que adivinar
/// —ni provocar una baja de Workspace de verdad para ver cómo queda el aviso—: cada ejecución
/// regenera los ficheros a partir del mismo código que compone los correos que salen, así que lo que
/// se inspecciona es el correo, no una maqueta parecida.
///
/// Los ficheros van a <c>artifacts/correos/</c>, fuera del control de versiones: son salida
/// reproducible, no contenido del proyecto.
/// </summary>
public class ProductEmailPreviewTests
{
    [Fact]
    public void Deberia_EscribirElHtmlYElTextoDeCadaCorreo()
    {
        // Arrange
        var directory = Path.Combine(RepositoryRoot(), "artifacts", "correos");
        Directory.CreateDirectory(directory);

        var index = new StringBuilder();

        // Act
        foreach (var (slug, nombre, message) in ProductEmailCatalog.All())
        {
            var html = Path.Combine(directory, $"{slug}.html");
            var text = Path.Combine(directory, $"{slug}.txt");

            // El asunto no se ve al abrir el `.html` en un navegador, así que se anota arriba del
            // todo: revisarlo forma parte de la revisión (es lo primero que se lee en la bandeja).
            File.WriteAllText(
                html,
                $"<!-- {nombre} · Asunto: {message.Subject} -->{Environment.NewLine}{message.HtmlBody}",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            File.WriteAllText(
                text,
                $"Asunto: {message.Subject}{Environment.NewLine}{Environment.NewLine}{message.TextBody}",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            index.AppendLine($"- {nombre} — {slug}.html · {slug}.txt");

            // Assert — que se hayan escrito y no estén vacíos: un preview vacío es peor que ninguno.
            new FileInfo(html).Length.Should().BeGreaterThan(0);
            new FileInfo(text).Length.Should().BeGreaterThan(0);
        }

        File.WriteAllText(
            Path.Combine(directory, "LEEME.txt"),
            $"""
            Correos del producto (MVP-715), regenerados por ProductEmailPreviewTests.

            {index}
            Abre los .html en el navegador o envíatelos para revisarlos en un cliente real (CA-5).
            El inventario en prosa está en docs/06-integraciones/correos-del-producto.md.
            """,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>
    /// Sube desde el directorio del ensamblado hasta encontrar la solución. No se usa una ruta
    /// relativa fija porque cambia entre <c>Debug</c>, <c>Release</c> y la carpeta de publicación.
    /// </summary>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "src", "backend", "Terrenario.sln")))
            directory = directory.Parent;

        return directory?.FullName
               ?? throw new InvalidOperationException(
                   "No se encuentra la raíz del repositorio desde " + AppContext.BaseDirectory);
    }
}
