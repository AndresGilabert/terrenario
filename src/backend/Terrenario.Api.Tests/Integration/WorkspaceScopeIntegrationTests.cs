using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-501, CA-2 — Integración de los <b>errores principales</b> y del aislamiento por Workspace
/// (MVP-105, RN-034), contra la API real y con SQL real.
///
/// Es la cobertura que faltaba cuando `GET /workspaces` devolvió <b>500</b> durante toda su vida útil
/// con 130 tests en verde (`P-014`): los tests de handler mockean el repositorio, así que nunca vieron
/// que EF no sabía traducir el <c>ORDER BY</c>. Aquí cada endpoint se ejercita de punta a punta, y por
/// eso el listado de Workspaces tiene su propia regresión explícita.
/// </summary>
public sealed class WorkspaceScopeIntegrationTests : IAsyncLifetime
{
    private readonly TerrenarioApiFactory _factory = new();

    private ApiSession _andres = null!;
    private ApiSession _lucia = null!;
    private Guid _andresPlotId;

    public async Task InitializeAsync()
    {
        _factory.Google
            .WithIdentity("codigo-andres", "sub-andres", "Andrés", "andres@ejemplo.test")
            .WithIdentity("codigo-lucia", "sub-lucia", "Lucía", "lucia@ejemplo.test");

        _andres = await ApiSession.LoginAsync(_factory, "codigo-andres");
        await _andres.CreateWorkspaceAsync("Finca El Olivar");
        var plot = await _andres.PostJsonAsync("/api/v1/plots", new { name = "La Hoya", ownership_type = "propia" });
        _andresPlotId = plot.GetProperty("id").GetGuid();

        // Segunda cuenta con su propio Workspace: es la que prueba que el aislamiento existe.
        _lucia = await ApiSession.LoginAsync(_factory, "codigo-lucia");
        await _lucia.CreateWorkspaceAsync("Cortijo del Río");
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static async Task<string?> ErrorCodeOf(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.TryGetProperty("error", out var error) ? error.GetProperty("code").GetString() : null;
    }

    [Fact]
    public async Task Deberia_ExigirSesion_Cuando_NoSeEnviaToken()
    {
        var anonymous = _factory.CreateApiClient();

        var response = await anonymous.GetAsync("/api/v1/plots");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deberia_ExigirWorkspaceActivo_Cuando_LaSesionNoLoTiene()
    {
        // Cuenta recién creada: tiene sesión válida pero todavía ningún Workspace.
        _factory.Google.WithIdentity("codigo-sin-workspace", "sub-nuevo", "Nuevo", "nuevo@ejemplo.test");
        var reciente = await ApiSession.LoginAsync(_factory, "codigo-sin-workspace");

        var response = await reciente.GetAsync("/api/v1/plots");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorCodeOf(response)).Should().Be("AUTH_WORKSPACE_SCOPE_REQUIRED");
    }

    [Fact]
    public async Task Deberia_AislarLosMaestros_Cuando_DosWorkspacesConviven()
    {
        var mios = await _andres.GetJsonAsync("/api/v1/plots");
        var suyos = await _lucia.GetJsonAsync("/api/v1/plots");

        mios.GetProperty("data").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("name").GetString().Should().Be("La Hoya");
        // El terreno de Andrés no puede asomar en el Workspace de Lucía, ni siquiera en un listado.
        suyos.GetProperty("data").EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Deberia_NegarElAcceso_Cuando_SePideUnRecursoDeOtroWorkspace()
    {
        var response = await _lucia.PatchAsync($"/api/v1/plots/{_andresPlotId}", new { name = "Robado" });

        // Ni 200 ni un 500: el recurso simplemente no existe para quien no es de ese Workspace.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deberia_ListarLosWorkspacesDelUsuario_Cuando_SePideElSelector()
    {
        // Regresión explícita de `P-014`: este endpoint devolvía 500 y el frontend lo tragaba a lista
        // vacía, así que el selector parecía vacío en vez de roto.
        var response = await _andres.GetAsync("/api/v1/workspaces");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("name").GetString().Should().Be("Finca El Olivar");
    }

    [Fact]
    public async Task Deberia_RechazarElAlta_Cuando_FaltaUnCampoObligatorio()
    {
        var response = await _andres.PostAsync("/api/v1/plots", new { ownership_type = "propia" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeOf(response)).Should().StartWith("VALIDATION_");
    }

    [Fact]
    public async Task Deberia_RechazarElAlta_Cuando_ElNombreYaExisteEnElWorkspace()
    {
        // Guarda de duplicados de MVP-205/207, extendida a los cuatro maestros: se comprueba contra
        // el índice único real, no contra un mock que siempre dice que no existe.
        var response = await _andres.PostAsync("/api/v1/plots", new { name = "la hoya", ownership_type = "propia" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorCodeOf(response)).Should().StartWith("CONFLICT_");
    }

    [Fact]
    public async Task Deberia_PermitirElMismoNombre_Cuando_EsOtroWorkspace()
    {
        // El nombre es único **dentro** del Workspace: dos explotaciones distintas pueden tener cada
        // una su «La Hoya».
        var response = await _lucia.PostAsync("/api/v1/plots", new { name = "La Hoya", ownership_type = "propia" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Deberia_ResponderConTrazabilidad_Cuando_SeAtiendeCualquierPeticion()
    {
        // `X-Request-Id` en todas las respuestas (P-006) y cabeceras de seguridad (P-005): son
        // transversales, así que se comprueban sobre el pipeline real, no sobre el middleware aislado.
        var response = await _andres.GetAsync("/api/v1/plots");

        response.Headers.Contains("X-Request-Id").Should().BeTrue();
        response.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
    }
}
