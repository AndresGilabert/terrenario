using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-502 — El <b>borde de transporte</b>: qué responde la API cuando el cuerpo de la petición no
/// es el que el contrato espera.
///
/// Cubre los dos puntos que `MVP-999` asignó a esta historia, que viven en el mismo sitio y por eso
/// se resuelven en la misma pasada:
///
/// <list type="bullet">
/// <item><b>`P-027`</b> — un `PATCH` con bytes JSON que no son UTF-8 válido respondía <b>500</b>: el
/// patrón <c>[FromBody] Dictionary&lt;string, JsonElement&gt;</c> acepta los bytes y revienta después
/// en <c>GetString()</c>. Un cuerpo mal formado es culpa del cliente: debe ser <b>400</b>.</item>
/// <item><b>`P-043`</b> — la validación del alta colapsaba a <c>VALIDATION_REQUIRED</c>, así que un
/// cliente no podía distinguir «falta» de «demasiado largo»; y un valor con formato inválido salía
/// con el mensaje por defecto de ASP.NET <b>en inglés</b>, que la UI mostraba tal cual.</item>
/// </list>
/// </summary>
public sealed class TransportValidationTests : IAsyncLifetime
{
    private readonly TerrenarioApiFactory _factory = new();

    private ApiSession _session = null!;
    private Guid _plotId;
    private Guid _workerId;
    private Guid _taskId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _factory.Google.WithIdentity("codigo", "sub", "Andrés", "andres@ejemplo.test");

        _session = await ApiSession.LoginAsync(_factory, "codigo");
        await _session.CreateWorkspaceAsync("Finca El Olivar");

        _plotId = (await _session.PostJsonAsync("/api/v1/plots", new { name = "La Hoya", ownership_type = "propia" }))
            .GetProperty("id").GetGuid();
        _workerId = (await _session.PostJsonAsync("/api/v1/workers", new { name = "Antonio Ruiz" }))
            .GetProperty("id").GetGuid();
        _taskId = (await _session.PostJsonAsync("/api/v1/tasks", new { name = "Poda" }))
            .GetProperty("id").GetGuid();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static async Task<(string? Code, string? Message)> ErrorOf(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (!body.TryGetProperty("error", out var error)) return (null, null);
        return (error.GetProperty("code").GetString(), error.GetProperty("message").GetString());
    }

    /// <summary>
    /// Cuerpo con bytes que no son UTF-8 válido. <c>0xFF</c> no puede aparecer en UTF-8, así que el
    /// JSON es sintácticamente plausible pero la cadena no se puede transcodificar.
    /// </summary>
    private async Task<HttpResponseMessage> PatchWithInvalidUtf8Async(
        string path,
        string field = "name",
        bool withIfMatch = false)
    {
        var prefix = Encoding.UTF8.GetBytes($"{{\"{field}\":\"");
        var suffix = Encoding.UTF8.GetBytes("\"}");
        var payload = new byte[prefix.Length + 2 + suffix.Length];
        prefix.CopyTo(payload, 0);
        payload[prefix.Length] = 0xFF;
        payload[prefix.Length + 1] = 0xFE;
        suffix.CopyTo(payload, prefix.Length + 2);

        using var request = new HttpRequestMessage(HttpMethod.Patch, path)
        {
            Content = new ByteArrayContent(payload)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);

        // Los registros operativos exigen `If-Match` antes de mirar el cuerpo (ADR-0005): sin él la
        // respuesta sería 400 por otro motivo y el test no probaría nada.
        if (withIfMatch) request.Headers.TryAddWithoutValidation("If-Match", "1");

        return await _session.Client.SendAsync(request);
    }

    [Theory]
    [InlineData("plots")]
    [InlineData("workers")]
    [InlineData("tasks")]
    public async Task Deberia_ResponderBadRequest_Cuando_ElCuerpoDelPatchNoEsUtf8Valido(string resource)
    {
        var id = resource switch
        {
            "plots" => _plotId,
            "workers" => _workerId,
            _ => _taskId
        };

        var response = await PatchWithInvalidUtf8Async($"/api/v1/{resource}/{id}");

        // `P-027`: antes eran 500. Un cuerpo que el cliente envió mal no es un fallo del servidor, y
        // además ensuciaba la observabilidad con errores que no lo eran.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var (code, message) = await ErrorOf(response);
        code.Should().StartWith("VALIDATION_");
        message.Should().NotBeNullOrWhiteSpace();
    }

    // El campo tiene que ser uno que **ese** recurso lea de verdad: si se manda un campo que el
    // controlador ignora, el cuerpo ni se toca y la respuesta acaba siendo el 404 del id inexistente,
    // que es un verde que no prueba nada.
    [Theory]
    [InlineData("seasons", "name", false)]
    [InlineData("activities", "description", true)]
    [InlineData("harvests", "product", true)]
    [InlineData("purchases", "product", true)]
    [InlineData("consumptions", "product", true)]
    public async Task Deberia_ResponderBadRequest_Cuando_ElCuerpoDelPatchNoEsUtf8Valido_EnElRestoDeRecursos(
        string resource,
        string field,
        bool needsIfMatch)
    {
        // `P-027` nombraba tres controladores, pero el patrón —y con él el 500— estaba en los ocho.
        // Se cubren todos: dejar cinco con el mismo defecto sería cerrarlo solo de nombre. El id no
        // existe a propósito: el cuerpo se lee **antes** de buscar el recurso, así que el 400 llega
        // primero que el 404.
        var response = await PatchWithInvalidUtf8Async($"/api/v1/{resource}/{Guid.NewGuid()}", field, needsIfMatch);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorOf(response)).Code.Should().StartWith("VALIDATION_");
    }

    [Fact]
    public async Task Deberia_ResponderBadRequest_Cuando_ElCuerpoDelPatchNoEsJson()
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/plots/{_plotId}")
        {
            Content = new StringContent("{esto no es json", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);

        var response = await _session.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deberia_DistinguirFaltaDeDemasiadoLargo_Cuando_SeDaDeAltaUnTerreno()
    {
        var falta = await _session.PostAsync("/api/v1/plots", new { ownership_type = "propia" });
        var largo = await _session.PostAsync(
            "/api/v1/plots",
            new { name = new string('x', 500), ownership_type = "propia" });

        falta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        largo.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var (codigoFalta, _) = await ErrorOf(falta);
        var (codigoLargo, _) = await ErrorOf(largo);

        // `P-043`: los dos respondían `VALIDATION_REQUIRED`, así que el cliente no podía saber qué
        // arreglar. El `PATCH` sí devolvía el código específico: la asimetría era el problema.
        codigoFalta.Should().Be("VALIDATION_REQUIRED_NAME");
        codigoLargo.Should().Be("VALIDATION_PLOT_NAME_LENGTH");
        codigoFalta.Should().NotBe(codigoLargo);
    }

    [Theory]
    [InlineData("workers", "VALIDATION_REQUIRED_NAME", "VALIDATION_WORKER_NAME_LENGTH")]
    [InlineData("tasks", "VALIDATION_REQUIRED_TASK_NAME", "VALIDATION_TASK_NAME_LENGTH")]
    public async Task Deberia_DistinguirFaltaDeDemasiadoLargo_Cuando_SeDaDeAltaEnOtrosMaestros(
        string resource,
        string codigoEsperadoFalta,
        string codigoEsperadoLargo)
    {
        var falta = await _session.PostAsync($"/api/v1/{resource}", new { });
        var largo = await _session.PostAsync($"/api/v1/{resource}", new { name = new string('x', 500) });

        (await ErrorOf(falta)).Code.Should().Be(codigoEsperadoFalta);
        (await ErrorOf(largo)).Code.Should().Be(codigoEsperadoLargo);
    }

    [Fact]
    public async Task Deberia_ResponderEnEspanol_Cuando_UnaFechaLlegaConFormatoInvalido()
    {
        // `P-043`: esto devolvía «The request field is required.», el texto por defecto de ASP.NET,
        // y la UI lo mostraba tal cual al usuario.
        var response = await _session.PostAsync("/api/v1/seasons", new
        {
            name = "Campaña 2025/26",
            start_date = "no-es-una-fecha"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var (code, message) = await ErrorOf(response);

        code.Should().Be("VALIDATION_FORMAT_INVALID");
        message.Should().NotBeNullOrWhiteSpace();
        message!.Should().NotContain("The ");
        message.Should().NotContain("field is required");
    }

    [Fact]
    public async Task Deberia_SeguirValidandoElResto_Cuando_ElCuerpoEsCorrecto()
    {
        // Guarda de no-regresión: endurecer el borde no puede romper el camino feliz.
        var response = await _session.PatchAsync($"/api/v1/plots/{_plotId}", new { name = "La Hoya Alta" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
