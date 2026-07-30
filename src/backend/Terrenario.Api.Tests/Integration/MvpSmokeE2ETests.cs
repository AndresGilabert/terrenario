using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-501, CA-3 — <b>Smoke E2E de los flujos mínimos del MVP</b>: login, captura diaria, cosecha,
/// compra/imputación y dashboard.
///
/// Es E2E <b>de servidor</b>, no de navegador: recorre entera la aplicación real (autenticación,
/// filtros de scope, controladores, handlers, dominio, EF y SQL) sobre una base de datos de verdad,
/// pero no ejercita el cliente React. La cobertura de navegador (Playwright) queda registrada como
/// hueco conocido: el login es Google OIDC y no se puede automatizar sin sembrar sesión.
///
/// Un solo test recorre el flujo completo a propósito. Trocearlo obligaría a resembrar el estado en
/// cada uno y dejaría de comprobar lo que aquí importa: que las piezas encajan <b>en secuencia</b>,
/// que es donde fallan los sistemas y donde no llegan los tests de handler.
/// </summary>
public sealed class MvpSmokeE2ETests : IAsyncLifetime
{
    private readonly TerrenarioApiFactory _factory = new();

    public Task InitializeAsync()
    {
        _factory.Google.WithIdentity("codigo-de-andres", "google-sub-andres", "Andrés", "andres@ejemplo.test");
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Deberia_RecorrerElNucleoDelMvp_Cuando_SeUsaLaApiComoLaUsaElProducto()
    {
        // ── 1. Login (MVP-101/102) ────────────────────────────────────────────
        var session = await ApiSession.LoginAsync(_factory, "codigo-de-andres");

        session.AccessToken.Should().NotBeNullOrWhiteSpace();
        // Primer acceso: la cuenta existe pero todavía no tiene Workspace donde trabajar.
        session.WorkspaceId.Should().BeNull();

        var me = await session.GetJsonAsync("/api/v1/auth/me");
        me.GetProperty("display_name").GetString().Should().Be("Andrés");

        // ── 2. Workspace y temporada (MVP-102/201) ───────────────────────────
        var workspaceId = await session.CreateWorkspaceAsync("Finca El Olivar");
        workspaceId.Should().NotBeEmpty();

        var season = await session.PostJsonAsync("/api/v1/seasons", new
        {
            name = "Campaña 2025/26",
            start_date = "2025-10-01",
            end_date = "2026-03-31"
        });
        var seasonId = season.GetProperty("id").GetGuid();

        // ── 3. Maestros mínimos para poder registrar (MVP-202/204) ───────────
        var plot = await session.PostJsonAsync("/api/v1/plots", new
        {
            name = "La Hoya",
            ownership_type = "propia",
            tree_count = 250
        });
        var plotId = plot.GetProperty("id").GetGuid();

        var worker = await session.PostJsonAsync("/api/v1/workers", new
        {
            name = "Antonio Ruiz",
            hourly_rate = 12.5m
        });
        var workerId = worker.GetProperty("id").GetGuid();

        // ── 4. Captura diaria: una labor (MVP-301/302) ───────────────────────
        var activity = await session.PostJsonAsync("/api/v1/activities", new
        {
            date = "2025-11-12",
            plot_id = plotId,
            season_id = seasonId,
            worker_id = workerId,
            task_text = "Poda de formación",
            hours = 6m,
            manual_cost = 75m,
            save_task_to_catalog = true
        });
        var activityId = activity.GetProperty("id").GetGuid();

        // La tarea escrita al vuelo se aprende en el catálogo del Workspace (RN-026).
        var tasks = await session.GetJsonAsync("/api/v1/tasks");
        tasks.GetProperty("data").EnumerateArray()
            .Select(task => task.GetProperty("name").GetString())
            .Should().Contain("Poda de formación");

        // ── 5. Cosecha (MVP-401/402) ─────────────────────────────────────────
        var harvest = await session.PostJsonAsync("/api/v1/harvests", new
        {
            date = "2025-12-05",
            plot_id = plotId,
            season_id = seasonId,
            product = "aceituna_olivar",
            kgs = 4200m,
            destination = "aceite_para_venta",
            // Rendimiento derivado de los litros obtenidos (RN-014, tercer origen), que es el caso
            // que más se usa: la almazara entrega litros, no un porcentaje.
            liters = 840m
        });
        harvest.GetProperty("kgs").GetDecimal().Should().Be(4200m);

        // ── 6. Compra e imputación (MVP-303/304) ─────────────────────────────
        var purchase = await session.PostJsonAsync("/api/v1/purchases", new
        {
            purchase_date = "2025-11-02",
            product = "Abono foliar",
            season_id = seasonId,
            total_quantity = 200m,
            total_cost = 400m
        });
        var purchaseId = purchase.GetProperty("id").GetGuid();

        var imputation = await session.PostJsonAsync($"/api/v1/purchases/{purchaseId}/consumptions", new
        {
            date = "2025-11-08",
            plot_id = plotId,
            quantity = 50m
        });
        // 50 de 200 unidades a 400 € ⇒ 100 € repartidos a ese terreno.
        imputation.GetProperty("proportional_cost").GetDecimal().Should().Be(100m);

        // ── 7. El diario los reúne todos (MVP-305, RN-033) ───────────────────
        var diary = await session.GetJsonAsync("/api/v1/diary");
        var entries = diary.GetProperty("data").EnumerateArray().ToList();

        entries.Select(e => e.GetProperty("type").GetString())
            .Should().BeEquivalentTo(["actividad", "cosecha", "compra", "consumo"]);

        // Orden por fecha de negocio descendente (RN-033), no por fecha de captura.
        entries.Select(e => e.GetProperty("date").GetString())
            .Should().BeInDescendingOrder();

        var meta = diary.GetProperty("meta");
        meta.GetProperty("total").GetInt32().Should().Be(4);
        meta.GetProperty("total_kg").GetDecimal().Should().Be(4200m);
        // R-01 (MVP-399) — labor (75) + compra (400). La imputación NO suma: reparte lo que la compra
        // ya aportó, y contarla sería contar el mismo dinero dos veces.
        meta.GetProperty("total_cost").GetDecimal().Should().Be(475m);
        meta.GetProperty("imputed_cost").GetDecimal().Should().Be(100m);

        // ── 8. Dashboard (MVP-403/404) ───────────────────────────────────────
        var summary = await session.GetJsonAsync("/api/v1/dashboard/summary");
        summary.GetProperty("total_kg").GetDecimal().Should().Be(4200m);
        // 840 L sobre 4.200 kg ⇒ 20 L/100kg en la unidad canónica (RN-013).
        summary.GetProperty("average_yield").GetDecimal().Should().Be(20m);
        // 4.200 kg entre 250 olivos (RN-010).
        summary.GetProperty("kg_per_tree").GetDecimal().Should().Be(16.8m);

        var kgByPlot = await session.GetJsonAsync("/api/v1/dashboard/kg-by-plot");
        var plotTotal = kgByPlot.GetProperty("data").EnumerateArray().Should().ContainSingle().Subject;
        plotTotal.GetProperty("plot_name").GetString().Should().Be("La Hoya");
        plotTotal.GetProperty("kg").GetDecimal().Should().Be(4200m);

        var kgByDestination = await session.GetJsonAsync("/api/v1/dashboard/kg-by-destination");
        kgByDestination.GetProperty("meta").GetProperty("total_kg").GetDecimal().Should().Be(4200m);

        // ── 9. Corregir y eliminar exigen la versión vigente (ADR-0005, RN-037) ─
        var stale = await session.DeleteAsync($"/api/v1/activities/{activityId}", ifMatch: 99);
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var deleted = await session.DeleteAsync($"/api/v1/activities/{activityId}", ifMatch: 1);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // El borrado es lógico: desaparece del diario, no de la base de datos (RN-037).
        var afterDelete = await session.GetJsonAsync("/api/v1/diary");
        afterDelete.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(3);

        await using var db = _factory.CreateDbContext();
        (await db.Activities.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Deberia_RechazarElAcceso_Cuando_GoogleNoValidaElCodigo()
    {
        var client = _factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/google/callback", new
        {
            code = "codigo-que-no-existe",
            redirect_uri = "https://terrenario.test/auth/callback",
            code_verifier = "verificador-de-prueba",
            flow_id = "0123456789abcdef0123456789abcdef"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("AUTH_GOOGLE_TOKEN_INVALID");
    }
}
