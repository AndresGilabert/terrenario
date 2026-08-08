using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-713 (`P-079`, CA-1/CA-2) — Qué responde de verdad <c>POST /auth/google/callback</c> cuando el
/// intercambio con Google falla, recorriendo la aplicación real.
///
/// El test de mapeo (<c>GoogleOAuthErrorMappingTests</c>) comprueba la tabla; este comprueba que la
/// tabla llega al cliente. Son cosas distintas: antes de esta historia la clasificación vivía repartida
/// en cláusulas <c>catch … when</c> del controlador, así que un código bien clasificado podía acabar
/// igualmente en un 500 por no tener cláusula.
/// </summary>
public sealed class GoogleOAuthErrorContractTests : IAsyncLifetime
{
    private readonly TerrenarioApiFactory _factory = new();

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();

        _factory.Google
            .WithOAuthError("codigo-ya-usado", GoogleOAuthErrors.InvalidGrant)
            .WithOAuthError("peticion-incompleta", GoogleOAuthErrors.InvalidRequest)
            .WithOAuthError("credenciales-mal-configuradas", GoogleOAuthErrors.InvalidClient);
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Theory]
    // CA-1 — Recargar la pantalla de vuelta de Google reenvía un código ya consumido.
    [InlineData("codigo-ya-usado", HttpStatusCode.Unauthorized, "AUTH_GOOGLE_CODE_INVALID")]
    [InlineData("peticion-incompleta", HttpStatusCode.BadRequest, "AUTH_GOOGLE_REQUEST_INVALID")]
    // CA-2 — Un fallo de configuración sigue siendo lo que es.
    [InlineData("credenciales-mal-configuradas", HttpStatusCode.InternalServerError, "AUTH_GOOGLE_EXCHANGE_FAILED")]
    public async Task Deberia_ResponderSegunDeQuienEsElError(
        string code, HttpStatusCode estadoEsperado, string codigoEsperado)
    {
        var response = await _factory.CreateApiClient().PostAsJsonAsync("/api/v1/auth/google/callback", new
        {
            code,
            redirect_uri = "https://terrenario.test/auth/callback",
            code_verifier = "verificador-de-prueba",
            flow_id = "0123456789abcdef0123456789abcdef"
        });

        response.StatusCode.Should().Be(estadoEsperado);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = body.GetProperty("error");
        error.GetProperty("code").GetString().Should().Be(codigoEsperado);
        // El mensaje va en español y sin detalle del proveedor (MVP-502, `P-043`).
        error.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Deberia_ExplicarQueHayQueVolverAEntrar_Cuando_ElCodigoYaSeUso()
    {
        // CA-1 — «Error al completar el acceso» no orientaba a nadie en el caso más común, que es
        // haber recargado la pantalla de vuelta.
        var response = await _factory.CreateApiClient().PostAsJsonAsync("/api/v1/auth/google/callback", new
        {
            code = "codigo-ya-usado",
            redirect_uri = "https://terrenario.test/auth/callback",
            code_verifier = "verificador-de-prueba"
        });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetProperty("message").GetString()
            .Should().Contain("caducado").And.Contain("Vuelve a entrar");
    }
}
