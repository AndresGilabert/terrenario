using FluentAssertions;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Telemetry;

/// <summary>
/// MKT-106 (CA-2) — Clasificación de origen a partir del `Referer`. Primera parte y agregada: nunca
/// se conserva el valor en crudo, solo el cubo (RN-042, ADR-0011).
/// </summary>
public class ReferrerClassifierTests : IDisposable
{
    private const string RequestHost = "terrenario.example";
    private readonly string _webRoot = Directory.CreateTempSubdirectory("terrenario-referrer-").FullName;

    public ReferrerClassifierTests()
    {
        Directory.CreateDirectory(Path.Combine(_webRoot, "funcionalidades", "gestion-terrenos"));
        File.WriteAllText(
            Path.Combine(_webRoot, "funcionalidades", "gestion-terrenos", "index.html"), "<html></html>");
    }

    public void Dispose() => Directory.Delete(_webRoot, recursive: true);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-es-una-url")]
    [InlineData("ftp://terrenario.example/funcionalidades/gestion-terrenos")]
    public void Deberia_ClasificarComoDirect_Cuando_NoHayReferrerUtilizable(string? referrer) =>
        ReferrerClassifier.Classify(referrer, RequestHost, _webRoot).Should().Be(ReferrerClassifier.Direct);

    [Fact]
    public void Deberia_ClasificarComoLanding_Cuando_ElReferrerEsUnaLandingPropiaExistente() =>
        ReferrerClassifier.Classify(
            "https://terrenario.example/funcionalidades/gestion-terrenos", RequestHost, _webRoot)
            .Should().Be("landing.funcionalidades.gestion-terrenos");

    [Fact]
    public void Deberia_ClasificarComoInternal_Cuando_ElReferrerEsPropio_PeroNoUnaLanding() =>
        ReferrerClassifier.Classify("https://terrenario.example/app/diario", RequestHost, _webRoot)
            .Should().Be(ReferrerClassifier.Internal);

    [Fact]
    public void Deberia_ClasificarComoInternal_Cuando_LaRutaDeLandingNoTieneFicheroReal() =>
        // Un `entry_referrer` fabricado con una ruta de landing inventada no se cuenta como landing.
        ReferrerClassifier.Classify(
            "https://terrenario.example/funcionalidades/no-existe", RequestHost, _webRoot)
            .Should().Be(ReferrerClassifier.Internal);

    [Fact]
    public void Deberia_ClasificarComoExterno_Y_SanearElDominio() =>
        ReferrerClassifier.Classify("https://www.Google.com/search?q=terrenario", RequestHost, _webRoot)
            .Should().Be("external.google.com");

    [Fact]
    public void Deberia_IgnorarElPuerto_AlCompararConElHostDeLaPeticion() =>
        ReferrerClassifier.Classify(
                "https://terrenario.example/funcionalidades/gestion-terrenos", "terrenario.example:443", _webRoot)
            .Should().Be("landing.funcionalidades.gestion-terrenos");

    [Fact]
    public void Deberia_AcotarLaLongitudDelDominioExterno()
    {
        var dominioLargo = string.Join(".", Enumerable.Repeat("sub", 40)) + ".com";

        ReferrerClassifier.Classify($"https://{dominioLargo}/x", RequestHost, _webRoot)
            .Length.Should().BeLessOrEqualTo("external.".Length + 64);
    }
}
