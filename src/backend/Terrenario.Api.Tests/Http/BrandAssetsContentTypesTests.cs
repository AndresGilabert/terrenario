using FluentAssertions;
using Microsoft.AspNetCore.StaticFiles;

namespace Terrenario.Api.Tests.Http;

/// <summary>
/// MVP-710 — Los recursos de marca los sirve <c>UseStaticFiles</c> desde <c>wwwroot</c>, y ese
/// middleware **no sirve lo que no sabe nombrar**: ante una extensión que no reconoce devuelve 404
/// en vez de un tipo genérico.
///
/// Es un fallo silencioso y caro: el `manifest.webmanifest` solo lo pide el navegador al añadir la
/// aplicación al inicio, así que un 404 ahí no aparece en ningún log ni en ninguna pantalla; lo
/// único que se ve es que el icono del escritorio vuelve a ser una captura. Por eso se fija aquí,
/// contra el mismo proveedor de tipos que usa el pipeline, y no se da por supuesto.
/// </summary>
public class BrandAssetsContentTypesTests
{
    private static readonly FileExtensionContentTypeProvider Proveedor = new();

    [Theory]
    [InlineData("manifest.webmanifest", "application/manifest+json")]
    [InlineData("favicon.svg", "image/svg+xml")]
    [InlineData("favicon.ico", "image/x-icon")]
    [InlineData("apple-touch-icon.png", "image/png")]
    [InlineData("og-image.png", "image/png")]
    public void Deberia_ServirLosRecursosDeMarca_ConSuTipoDeContenido(string fichero, string tipo)
    {
        Proveedor.TryGetContentType(fichero, out var resultado).Should().BeTrue(
            "un fichero cuya extensión el proveedor no conoce se responde con 404, no con un tipo por defecto");
        resultado.Should().Be(tipo);
    }
}
