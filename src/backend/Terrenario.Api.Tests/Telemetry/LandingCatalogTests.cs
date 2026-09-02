using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Telemetry;

/// <summary>
/// MKT-106 (CA-1) — El catálogo de landings es abierto: la validación es que el fichero exista, no
/// una lista de slugs declarada.
/// </summary>
public class LandingCatalogTests : IDisposable
{
    private readonly string _webRoot = Directory.CreateTempSubdirectory("terrenario-landings-").FullName;

    public LandingCatalogTests()
    {
        File.WriteAllText(Path.Combine(_webRoot, "home.html"), "<html></html>");

        Directory.CreateDirectory(Path.Combine(_webRoot, "funcionalidades", "gestion-terrenos"));
        File.WriteAllText(
            Path.Combine(_webRoot, "funcionalidades", "gestion-terrenos", "index.html"), "<html></html>");

        Directory.CreateDirectory(Path.Combine(_webRoot, "para", "agricultor-particular"));
        File.WriteAllText(
            Path.Combine(_webRoot, "para", "agricultor-particular", "index.html"), "<html></html>");
    }

    public void Dispose() => Directory.Delete(_webRoot, recursive: true);

    [Fact]
    public void Deberia_Clasificar_LaHome() =>
        LandingCatalog.TryClassifyRequestPath(_webRoot, "/").Should().Be("home");

    [Fact]
    public void Deberia_Clasificar_UnaFuncionalidadExistente() =>
        LandingCatalog.TryClassifyRequestPath(_webRoot, "/funcionalidades/gestion-terrenos")
            .Should().Be("funcionalidades.gestion-terrenos");

    [Fact]
    public void Deberia_Clasificar_UnPerfilExistente() =>
        LandingCatalog.TryClassifyRequestPath(_webRoot, "/para/agricultor-particular")
            .Should().Be("para.agricultor-particular");

    [Theory]
    [InlineData("/funcionalidades/no-existe")]
    [InlineData("/para/no-existe")]
    [InlineData("/app/diario")]
    [InlineData("/funcionalidades/../../etc/passwd")]
    [InlineData("/funcionalidades/con espacios")]
    public void Deberia_DevolverNull_Cuando_NoEsUnaLandingReal(string path) =>
        LandingCatalog.TryClassifyRequestPath(_webRoot, path).Should().BeNull();

    [Fact]
    public void Deberia_DevolverNull_Cuando_NoHayWebRoot() =>
        LandingCatalog.TryClassifyRequestPath(string.Empty, "/").Should().BeNull();
}
