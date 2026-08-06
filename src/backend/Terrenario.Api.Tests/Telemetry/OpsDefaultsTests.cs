using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Tests.Telemetry;

/// <summary>
/// MVP-603 — Guarda sobre la configuración **que se publica**.
///
/// Detectado verificando el arranque de verdad: `appsettings.json` llevaba
/// <c>"ApiKey": "REPLACE_IN_SECRETS"</c>, siguiendo el patrón del resto de secretos del producto. Aquí
/// ese patrón es un fallo de seguridad y no una convención: los demás marcadores rompen ruidosamente
/// si nadie los sustituye —la base de datos no conecta, el login falla—, pero este **abre** el endpoint
/// de señales con una llave que está escrita en un repositorio público.
///
/// El test mira el fichero real que viaja en el paquete, no una instancia de la clase: el defecto no
/// estaba en el código, estaba en el valor publicado.
/// </summary>
public class OpsDefaultsTests
{
    private static OpsOptions LoadShippedOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        return configuration.GetSection(OpsOptions.SectionName).Get<OpsOptions>() ?? new OpsOptions();
    }

    [Fact]
    public void LaConfiguracionPublicada_NoDebeHabilitarElEndpointDeSenales()
    {
        var options = LoadShippedOptions();

        options.ApiKey.Should().BeEmpty(
            "una llave escrita en un repositorio público no es una llave: dejaría el endpoint abierto");
        options.IsSignalsEndpointEnabled.Should().BeFalse();
    }

    [Fact]
    public void LaConfiguracionPublicada_NoDebeLlevarDestinatarioDeAlertas()
        // Es una dirección de correo real: en un repositorio público queda en el historial para siempre.
        => LoadShippedOptions().AlertEmail.Should().BeEmpty();
}
