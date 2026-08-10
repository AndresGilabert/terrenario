using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-806 — Depuración de maestros contra la <b>API real</b>: las cuatro superficies, el mismo
/// contrato.
///
/// Los tests de repositorio ya comprueban el recuento y el reapuntado contra PostgreSQL. Lo que se
/// añade aquí es lo que solo se ve por el borde de transporte: que las cuatro respondan igual, que el
/// error de uso llegue con su código y su cifra, y que la ficha desaparezca de verdad del listado
/// —activos e inactivos— y no solo de la tabla.
/// </summary>
public sealed class MasterDepurationIntegrationTests : IAsyncLifetime
{
    private readonly TerrenarioApiFactory _factory = new();

    private ApiSession _session = null!;
    private Guid _seasonId;
    private Guid _plotId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _factory.Google.WithIdentity("codigo", "sub", "Andrés Gilabert", "andres@ejemplo.test");
        _session = await ApiSession.LoginAsync(_factory, "codigo");
        await _session.CreateWorkspaceAsync("Finca El Olivar");

        _seasonId = await CreateSeasonAsync("Campaña 2025/26", "2025-10-01");
        _plotId = await CreatePlotAsync("La Hoya");
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private async Task<Guid> CreateSeasonAsync(string name, string start)
        => (await _session.PostJsonAsync("/api/v1/seasons", new { name, start_date = start }))
            .GetProperty("id").GetGuid();

    private async Task<Guid> CreatePlotAsync(string name)
        => (await _session.PostJsonAsync("/api/v1/plots", new { name, ownership_type = "propia" }))
            .GetProperty("id").GetGuid();

    private async Task<Guid> CreateWorkerAsync(string name)
        => (await _session.PostJsonAsync("/api/v1/workers", new { name })).GetProperty("id").GetGuid();

    private async Task<Guid> CreateTaskAsync(string name)
        => (await _session.PostJsonAsync("/api/v1/tasks", new { name })).GetProperty("id").GetGuid();

    private Task<JsonElement> CreateActivityAsync(Guid plotId, Guid seasonId, Guid workerId, Guid? taskId)
        => _session.PostJsonAsync("/api/v1/activities", new
        {
            date = "2025-12-05",
            plot_id = plotId,
            season_id = seasonId,
            worker_id = workerId,
            task_id = taskId,
            task_text = taskId is null ? "Labor al vuelo" : null,
            hours = 4m,
            manual_cost = 40m
        });

    private Task<JsonElement> CreateHarvestAsync(Guid plotId, Guid seasonId)
        => _session.PostJsonAsync("/api/v1/harvests", new
        {
            date = "2025-12-05",
            plot_id = plotId,
            season_id = seasonId,
            product = "aceituna_olivar",
            kgs = 1_000m,
            destination = "desconocido"
        });

    private Task<JsonElement> CreatePurchaseAsync(Guid seasonId)
        => _session.PostJsonAsync("/api/v1/purchases", new
        {
            purchase_date = "2025-11-02",
            product = "Abono foliar",
            season_id = seasonId,
            total_quantity = 200m,
            total_cost = 400m
        });

    private Task<JsonElement> CreateConsumptionAsync(Guid plotId, Guid seasonId)
        => _session.PostJsonAsync("/api/v1/consumptions", new
        {
            date = "2025-11-10",
            plot_id = plotId,
            season_id = seasonId,
            product = "Abono foliar",
            quantity = 20m
        });

    private static async Task<(string Code, string Message)> ErrorOf(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = body.GetProperty("error");
        return (error.GetProperty("code").GetString()!, error.GetProperty("message").GetString()!);
    }

    private async Task<JsonElement> ListAsync(string resource) => await _session.GetJsonAsync(resource);

    // ── CA-1 · borrar lo que nunca se usó ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("plots")]
    [InlineData("tasks")]
    [InlineData("workers")]
    public async Task Borrar_Deberia_QuitarLaFichaDeLosListados_EnLosCuatroMaestros(string resource)
    {
        var id = resource switch
        {
            "plots" => await CreatePlotAsync("Creado por error"),
            "tasks" => await CreateTaskAsync("Creada por error"),
            _ => await CreateWorkerAsync("Creado por error")
        };
        // Se inactiva primero: es justo la fila que hoy se queda para siempre en «inactivos».
        (await _session.PatchAsync($"/api/v1/{resource}/{id}", new { is_active = false }))
            .EnsureSuccessStatusCode();

        var response = await _session.DeleteAsync($"/api/v1/{resource}/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var listed = (await ListAsync($"/api/v1/{resource}")).GetProperty("data")
            .EnumerateArray().Select(row => row.GetProperty("id").GetGuid());
        listed.Should().NotContain(id);
    }

    [Fact]
    public async Task BorrarUnaTemporada_Deberia_QuitarlaDelListado()
    {
        var disposable = await CreateSeasonAsync("Campaña creada por error", "2020-01-01");

        (await _session.DeleteAsync($"/api/v1/seasons/{disposable}")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        var listed = (await ListAsync("/api/v1/seasons")).GetProperty("data")
            .EnumerateArray().Select(row => row.GetProperty("id").GetGuid());
        listed.Should().NotContain(disposable);
    }

    [Fact]
    public async Task Borrar_Deberia_Responder404_Cuando_LaFichaNoExiste()
    {
        var response = await _session.DeleteAsync($"/api/v1/plots/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── CA-2 · un caso por cada tipo de referencia, contra la API ───────────────────────────────

    [Fact]
    public async Task BorrarUnTerrenoConActividades_Deberia_Responder422_ConLaCifra()
    {
        var worker = await CreateWorkerAsync("Juan");
        await CreateActivityAsync(_plotId, _seasonId, worker, null);
        await CreateActivityAsync(_plotId, _seasonId, worker, null);

        var response = await _session.DeleteAsync($"/api/v1/plots/{_plotId}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var (code, message) = await ErrorOf(response);
        code.Should().Be(ErrorCodes.BusinessRuleMasterInUse);
        message.Should().Contain("2 actividades");
    }

    [Fact]
    public async Task BorrarUnTerrenoConCosechas_Deberia_Responder422()
    {
        var plot = await CreatePlotAsync("Solo con cosecha");
        await CreateHarvestAsync(plot, _seasonId);

        var (code, message) = await ErrorOf(await _session.DeleteAsync($"/api/v1/plots/{plot}"));

        code.Should().Be(ErrorCodes.BusinessRuleMasterInUse);
        message.Should().Contain("1 cosecha");
    }

    [Fact]
    public async Task BorrarUnTerrenoConConsumos_Deberia_Responder422()
    {
        // El caso que el spec destaca: comprobar solo contra el diario dejaría borrar este terreno.
        var plot = await CreatePlotAsync("Solo con consumo");
        await CreateConsumptionAsync(plot, _seasonId);

        var (code, message) = await ErrorOf(await _session.DeleteAsync($"/api/v1/plots/{plot}"));

        code.Should().Be(ErrorCodes.BusinessRuleMasterInUse);
        message.Should().Contain("1 consumo");
    }

    [Fact]
    public async Task BorrarUnaTemporadaConActividades_Deberia_Responder422()
    {
        var season = await CreateSeasonAsync("Campaña con diario", "2021-01-01");
        var worker = await CreateWorkerAsync("Juan");
        await CreateActivityAsync(_plotId, season, worker, null);

        var (code, message) = await ErrorOf(await _session.DeleteAsync($"/api/v1/seasons/{season}"));

        code.Should().Be(ErrorCodes.BusinessRuleMasterInUse);
        message.Should().Contain("1 actividad");
    }

    [Fact]
    public async Task BorrarUnaTemporadaConCosechas_Deberia_Responder422()
    {
        var season = await CreateSeasonAsync("Campaña con cosecha", "2021-02-01");
        await CreateHarvestAsync(_plotId, season);

        var (code, message) = await ErrorOf(await _session.DeleteAsync($"/api/v1/seasons/{season}"));

        code.Should().Be(ErrorCodes.BusinessRuleMasterInUse);
        message.Should().Contain("1 cosecha");
    }

    [Fact]
    public async Task BorrarUnaTemporadaConCompras_Deberia_Responder422()
    {
        var season = await CreateSeasonAsync("Campaña con compra", "2021-03-01");
        await CreatePurchaseAsync(season);

        var (code, message) = await ErrorOf(await _session.DeleteAsync($"/api/v1/seasons/{season}"));

        code.Should().Be(ErrorCodes.BusinessRuleMasterInUse);
        message.Should().Contain("1 compra");
    }

    [Fact]
    public async Task BorrarUnaTemporadaConConsumos_Deberia_Responder422()
    {
        var season = await CreateSeasonAsync("Campaña con consumo", "2021-04-01");
        await CreateConsumptionAsync(_plotId, season);

        var (code, message) = await ErrorOf(await _session.DeleteAsync($"/api/v1/seasons/{season}"));

        code.Should().Be(ErrorCodes.BusinessRuleMasterInUse);
        message.Should().Contain("1 consumo");
    }

    [Fact]
    public async Task BorrarUnTrabajadorConActividades_Deberia_Responder422()
    {
        var worker = await CreateWorkerAsync("Con histórico");
        await CreateActivityAsync(_plotId, _seasonId, worker, null);

        var (code, message) = await ErrorOf(await _session.DeleteAsync($"/api/v1/workers/{worker}"));

        code.Should().Be(ErrorCodes.BusinessRuleMasterInUse);
        message.Should().Contain("1 actividad");
    }

    [Fact]
    public async Task BorrarUnaTareaConActividades_Deberia_Responder422()
    {
        var worker = await CreateWorkerAsync("Juan");
        var task = await CreateTaskAsync("Poda");
        await CreateActivityAsync(_plotId, _seasonId, worker, task);

        var (code, message) = await ErrorOf(await _session.DeleteAsync($"/api/v1/tasks/{task}"));

        code.Should().Be(ErrorCodes.BusinessRuleMasterInUse);
        message.Should().Contain("1 actividad");
    }

    [Fact]
    public async Task ElListado_Deberia_TraerElRecuentoDeUso_ParaQueLaInterfazSepaAQuienOfrecerBorrar()
    {
        var worker = await CreateWorkerAsync("Juan");
        await CreateActivityAsync(_plotId, _seasonId, worker, null);
        var unused = await CreatePlotAsync("Sin usar");

        var rows = (await ListAsync("/api/v1/plots")).GetProperty("data").EnumerateArray()
            .ToDictionary(row => row.GetProperty("id").GetGuid(),
                          row => row.GetProperty("usage_count").GetInt32());

        rows[_plotId].Should().Be(1);
        rows[unused].Should().Be(0);
    }

    // ── CA-3 y CA-5 · fusión ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Fusionar_Deberia_ReapuntarLosRegistros_Y_DejarLasClavesAjenasResolviendo()
    {
        var survivor = await CreatePlotAsync("Bancal de arriba");
        var absorbed = await CreatePlotAsync("Bancal de arriba (2)");
        var worker = await CreateWorkerAsync("Juan");
        await CreateActivityAsync(survivor, _seasonId, worker, null);
        await CreateActivityAsync(absorbed, _seasonId, worker, null);
        await CreateHarvestAsync(absorbed, _seasonId);

        var result = await _session.PostJsonAsync(
            $"/api/v1/plots/{survivor}/merge", new { absorbed_id = absorbed });

        result.GetProperty("reassigned_count").GetInt32().Should().Be(2);
        result.GetProperty("absorbed_name").GetString().Should().Be("Bancal de arriba (2)");

        var rows = (await ListAsync("/api/v1/plots")).GetProperty("data").EnumerateArray()
            .ToDictionary(row => row.GetProperty("id").GetGuid(),
                          row => row.GetProperty("usage_count").GetInt32());
        rows.Should().NotContainKey(absorbed);
        rows[survivor].Should().Be(3);

        // CA-5 — el diario sigue resolviendo el nombre del terreno de los registros reapuntados.
        var diary = await ListAsync("/api/v1/activities");
        diary.GetProperty("data").EnumerateArray()
            .Select(row => row.GetProperty("plot_name").GetString())
            .Should().AllBe("Bancal de arriba");
    }

    [Fact]
    public async Task Fusionar_Deberia_Responder400_Cuando_LaFichaAbsorbidaNoExiste()
    {
        var response = await _session.PostAsync(
            $"/api/v1/plots/{_plotId}/merge", new { absorbed_id = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorOf(response)).Code.Should().Be(ErrorCodes.ForeignKeyWorkspaceMismatch);
    }

    [Fact]
    public async Task Fusionar_Deberia_Responder422_Cuando_SeIntentaConsigoMisma()
    {
        var response = await _session.PostAsync(
            $"/api/v1/plots/{_plotId}/merge", new { absorbed_id = _plotId });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await ErrorOf(response)).Code.Should().Be(ErrorCodes.BusinessRuleMasterMergeSelf);
    }

    // ── CA-4 · el caso miembro / cuadrilla ──────────────────────────────────────────────────────

    [Fact]
    public async Task FusionarCuadrillaEnMiembro_Deberia_ConservarLaFichaDelMiembro_Y_SuIndiceUnico()
    {
        // El escenario que motiva la historia: MVP-208 materializó al miembro «Andrés Gilabert» y la
        // cuadrilla homónima quedó renombrada « (2)» por la política de MVP-207.
        var workers = (await ListAsync("/api/v1/workers")).GetProperty("data").EnumerateArray().ToList();
        var member = workers.Single(w => w.GetProperty("kind").GetString() == WorkerKinds.Member);
        var memberId = member.GetProperty("id").GetGuid();
        var crew = await CreateWorkerAsync("Andrés Gilabert (2)");
        await CreateActivityAsync(_plotId, _seasonId, crew, null);

        var result = await _session.PostJsonAsync(
            $"/api/v1/workers/{memberId}/merge", new { absorbed_id = crew });

        result.GetProperty("reassigned_count").GetInt32().Should().Be(1);

        var remaining = (await ListAsync("/api/v1/workers")).GetProperty("data").EnumerateArray().ToList();
        remaining.Select(w => w.GetProperty("id").GetGuid()).Should().NotContain(crew);
        remaining.Single(w => w.GetProperty("id").GetGuid() == memberId)
            .GetProperty("name").GetString().Should().Be("Andrés Gilabert");

        // El índice único parcial de MVP-208 se sigue cumpliendo: una sola fila por cuenta y Workspace.
        await using var db = _factory.CreateDbContext();
        var accountRows = await db.Workers
            .CountAsync(w => w.WorkspaceId == _session.WorkspaceId!.Value && w.UserAccountId != null);
        accountRows.Should().Be(1);
    }

    [Fact]
    public async Task FusionarMiembroEnCuadrilla_Deberia_Responder422()
    {
        var workers = (await ListAsync("/api/v1/workers")).GetProperty("data").EnumerateArray().ToList();
        var memberId = workers.Single(w => w.GetProperty("kind").GetString() == WorkerKinds.Member)
            .GetProperty("id").GetGuid();
        var crew = await CreateWorkerAsync("Andrés Gilabert (2)");

        var response = await _session.PostAsync(
            $"/api/v1/workers/{crew}/merge", new { absorbed_id = memberId });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await ErrorOf(response)).Code.Should().Be(ErrorCodes.BusinessRuleMasterMergeMemberSurvives);
    }

    [Fact]
    public async Task BorrarLaFichaDeUnMiembro_Deberia_Responder422()
    {
        var workers = (await ListAsync("/api/v1/workers")).GetProperty("data").EnumerateArray().ToList();
        var memberId = workers.Single(w => w.GetProperty("kind").GetString() == WorkerKinds.Member)
            .GetProperty("id").GetGuid();

        var response = await _session.DeleteAsync($"/api/v1/workers/{memberId}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await ErrorOf(response)).Code.Should().Be(ErrorCodes.BusinessRuleWorkerMembershipManaged);
    }

    // ── Aislamiento multi-tenant ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Borrar_Deberia_Responder404_ConUnaFichaDeOtroWorkspace()
    {
        _factory.Google.WithIdentity("codigo-ajeno", "sub-ajeno", "Otra", "otra@ejemplo.test");
        var other = await ApiSession.LoginAsync(_factory, "codigo-ajeno");
        await other.CreateWorkspaceAsync("Finca ajena");
        var foreignPlot = (await other.PostJsonAsync(
            "/api/v1/plots", new { name = "Parcela ajena", ownership_type = "propia" }))
            .GetProperty("id").GetGuid();

        var response = await _session.DeleteAsync($"/api/v1/plots/{foreignPlot}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await using var db = _factory.CreateDbContext();
        (await db.Plots.AnyAsync(p => p.Id == foreignPlot)).Should().BeTrue();
    }
}
