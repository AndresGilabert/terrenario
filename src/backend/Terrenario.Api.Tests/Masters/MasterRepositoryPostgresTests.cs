using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Activities;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Harvests;
using Terrenario.Api.Domain.Masters;
using Terrenario.Api.Domain.Operations;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Purchases;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Tasks;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workers;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Tests.Integration;

namespace Terrenario.Api.Tests.Masters;

/// <summary>
/// MVP-806 — Depuración de maestros contra <b>PostgreSQL real</b>. Es donde tiene que estar: los
/// mocks no ven la traducción a SQL de las consultas de recuento, ni las claves ajenas
/// <c>RESTRICT</c>, ni el token de concurrencia, ni la transacción de la fusión. Con el repositorio
/// mockeado, un recuento que se dejara una tabla seguiría pasando en verde.
///
/// El CA-2 exige «un caso de cada tipo de referencia, no solo con uno»: hay una prueba por cada una de
/// las nueve formas de referenciar a un maestro que declara <c>MasterReferenceMap</c>.
/// </summary>
public sealed class MasterRepositoryPostgresTests : RepositoryTestBase
{
    private Guid _workspaceId;
    private Guid _userId;

    private static readonly DateOnly Today = new(2026, 8, 10);

    private async Task<(Guid Workspace, Guid User)> SeedWorkspaceAsync(string suffix)
    {
        var user = User.Create($"google-sub{suffix}", "Antonio", $"antonio{suffix}@ejemplo.com");
        Db.Users.Add(user);
        var workspace = Workspace.Create(user.Id, $"Finca El Olivar {suffix}");
        Db.Workspaces.Add(workspace);
        Db.WorkspaceMembers.Add(WorkspaceMember.CreateOwner(workspace.Id, user.Id));
        await Db.SaveChangesAsync();

        _workspaceId = workspace.Id;
        _userId = user.Id;
        return (workspace.Id, user.Id);
    }

    private async Task<Plot> SeedPlotAsync(string name)
    {
        var plot = Plot.Create(_workspaceId, name, PlotOwnershipTypes.Propia);
        Db.Plots.Add(plot);
        await Db.SaveChangesAsync();
        return plot;
    }

    private async Task<Season> SeedSeasonAsync(string name)
    {
        var season = Season.Create(_workspaceId, name, new DateOnly(2026, 1, 1), null);
        Db.Seasons.Add(season);
        await Db.SaveChangesAsync();
        return season;
    }

    private async Task<Worker> SeedWorkerAsync(string name)
    {
        var worker = Worker.Create(_workspaceId, name);
        Db.Workers.Add(worker);
        await Db.SaveChangesAsync();
        return worker;
    }

    private async Task<TaskItem> SeedTaskAsync(string name)
    {
        var task = TaskItem.Create(_workspaceId, name);
        Db.Tasks.Add(task);
        await Db.SaveChangesAsync();
        return task;
    }

    private async Task<Activity> SeedActivityAsync(Guid plotId, Guid seasonId, Guid workerId, Guid? taskId)
    {
        var activity = Activity.Create(
            _workspaceId, plotId, seasonId, workerId, Today, 4m, taskId,
            taskId is null ? "Labor escrita a mano" : null, 40m, null, _userId);
        Db.Activities.Add(activity);
        await Db.SaveChangesAsync();
        return activity;
    }

    private async Task<Harvest> SeedHarvestAsync(Guid plotId, Guid seasonId)
    {
        var harvest = Harvest.Create(
            _workspaceId, plotId, seasonId, Today, HarvestProducts.AceitunaOlivar, 1200m,
            HarvestDestinations.Desconocido, null, null, null, _userId);
        Db.Harvests.Add(harvest);
        await Db.SaveChangesAsync();
        return harvest;
    }

    private async Task<Purchase> SeedPurchaseAsync(Guid seasonId)
    {
        var purchase = Purchase.Create(_workspaceId, seasonId, Today, "Abono", 100m, 250m, _userId);
        Db.Purchases.Add(purchase);
        await Db.SaveChangesAsync();
        return purchase;
    }

    private async Task<PurchaseConsumption> SeedConsumptionAsync(Guid plotId, Guid seasonId)
    {
        var consumption = PurchaseConsumption.RegisterWithoutPurchase(
            _workspaceId, seasonId, plotId, Today, "Abono", 10m, _userId);
        Db.PurchaseConsumptions.Add(consumption);
        await Db.SaveChangesAsync();
        return consumption;
    }

    private MasterRepository Repository() => new(Db);

    // ── CA-2 · una prueba por tipo de referencia ────────────────────────────────────────────────
    //
    // Nueve, que son las nueve que declara `MasterReferenceMap`. Cada una monta el maestro con **una
    // sola** referencia de su tipo: si el recuento se dejara esa tabla, el test lo diría, y ninguno de
    // los otros ocho podría taparlo.

    [Fact]
    public async Task ElUsoDeUnTerreno_Deberia_ContarLasActividades()
    {
        await SeedWorkspaceAsync("-plot-act");
        var plot = await SeedPlotAsync("Bancal");
        var season = await SeedSeasonAsync("Campaña 2026");
        var worker = await SeedWorkerAsync("Juan");
        await SeedActivityAsync(plot.Id, season.Id, worker.Id, null);

        var usage = await Repository().CountUsageAsync(MasterKind.Plot, _workspaceId, plot.Id);

        usage.Total.Should().Be(1);
        usage.Describe().Should().Be("1 actividad");
    }

    [Fact]
    public async Task ElUsoDeUnTerreno_Deberia_ContarLasCosechas()
    {
        await SeedWorkspaceAsync("-plot-har");
        var plot = await SeedPlotAsync("Bancal");
        var season = await SeedSeasonAsync("Campaña 2026");
        await SeedHarvestAsync(plot.Id, season.Id);

        var usage = await Repository().CountUsageAsync(MasterKind.Plot, _workspaceId, plot.Id);

        usage.Describe().Should().Be("1 cosecha");
    }

    [Fact]
    public async Task ElUsoDeUnTerreno_Deberia_ContarLosConsumos()
    {
        // La referencia que el spec señala como fácil de olvidar: «los terrenos también desde los
        // consumos». Sin ella, este terreno pasaría por «nunca usado».
        await SeedWorkspaceAsync("-plot-con");
        var plot = await SeedPlotAsync("Bancal");
        var season = await SeedSeasonAsync("Campaña 2026");
        await SeedConsumptionAsync(plot.Id, season.Id);

        var usage = await Repository().CountUsageAsync(MasterKind.Plot, _workspaceId, plot.Id);

        usage.Describe().Should().Be("1 consumo");
    }

    [Fact]
    public async Task ElUsoDeUnaTemporada_Deberia_ContarLasActividades()
    {
        await SeedWorkspaceAsync("-sea-act");
        var plot = await SeedPlotAsync("Bancal");
        var season = await SeedSeasonAsync("Campaña 2026");
        var worker = await SeedWorkerAsync("Juan");
        await SeedActivityAsync(plot.Id, season.Id, worker.Id, null);

        var usage = await Repository().CountUsageAsync(MasterKind.Season, _workspaceId, season.Id);

        usage.Describe().Should().Be("1 actividad");
    }

    [Fact]
    public async Task ElUsoDeUnaTemporada_Deberia_ContarLasCosechas()
    {
        await SeedWorkspaceAsync("-sea-har");
        var plot = await SeedPlotAsync("Bancal");
        var season = await SeedSeasonAsync("Campaña 2026");
        await SeedHarvestAsync(plot.Id, season.Id);

        var usage = await Repository().CountUsageAsync(MasterKind.Season, _workspaceId, season.Id);

        usage.Describe().Should().Be("1 cosecha");
    }

    [Fact]
    public async Task ElUsoDeUnaTemporada_Deberia_ContarLasCompras()
    {
        // Las compras solo referencian a la temporada, no al terreno: es la única forma de uso que un
        // recuento copiado del de terrenos se dejaría entera.
        await SeedWorkspaceAsync("-sea-pur");
        var season = await SeedSeasonAsync("Campaña 2026");
        await SeedPurchaseAsync(season.Id);

        var usage = await Repository().CountUsageAsync(MasterKind.Season, _workspaceId, season.Id);

        usage.Describe().Should().Be("1 compra");
    }

    [Fact]
    public async Task ElUsoDeUnaTemporada_Deberia_ContarLosConsumos()
    {
        await SeedWorkspaceAsync("-sea-con");
        var plot = await SeedPlotAsync("Bancal");
        var season = await SeedSeasonAsync("Campaña 2026");
        await SeedConsumptionAsync(plot.Id, season.Id);

        var usage = await Repository().CountUsageAsync(MasterKind.Season, _workspaceId, season.Id);

        usage.Describe().Should().Be("1 consumo");
    }

    [Fact]
    public async Task ElUsoDeUnTrabajador_Deberia_ContarLasActividades()
    {
        await SeedWorkspaceAsync("-wor-act");
        var plot = await SeedPlotAsync("Bancal");
        var season = await SeedSeasonAsync("Campaña 2026");
        var worker = await SeedWorkerAsync("Juan");
        await SeedActivityAsync(plot.Id, season.Id, worker.Id, null);

        var usage = await Repository().CountUsageAsync(MasterKind.Worker, _workspaceId, worker.Id);

        usage.Describe().Should().Be("1 actividad");
    }

    [Fact]
    public async Task ElUsoDeUnaTarea_Deberia_ContarSoloLasActividadesQueLaEligieronDelCatalogo()
    {
        // RN-025 — la tarea puede venir del catálogo o en texto libre. La segunda no referencia a
        // ninguna fila, así que no puede impedir borrarla.
        await SeedWorkspaceAsync("-tas-act");
        var plot = await SeedPlotAsync("Bancal");
        var season = await SeedSeasonAsync("Campaña 2026");
        var worker = await SeedWorkerAsync("Juan");
        var task = await SeedTaskAsync("Poda");
        await SeedActivityAsync(plot.Id, season.Id, worker.Id, task.Id);
        await SeedActivityAsync(plot.Id, season.Id, worker.Id, null);

        var usage = await Repository().CountUsageAsync(MasterKind.Task, _workspaceId, task.Id);

        usage.Describe().Should().Be("1 actividad");
    }

    // ── El uso incluye lo eliminado lógicamente ─────────────────────────────────────────────────

    [Fact]
    public async Task ElUso_Deberia_ContarTambienLosRegistrosEliminadosLogicamente()
    {
        // Una actividad con `deleted_at` sigue teniendo su `plot_id` apuntando aquí (RN-037: el
        // borrado es lógico). Si el recuento filtrara por «vivos», el terreno pasaría por no usado y
        // el borrado físico chocaría con la FK RESTRICT: un 500 en vez de un 422 explicado.
        await SeedWorkspaceAsync("-borrado-logico");
        var plot = await SeedPlotAsync("Bancal");
        var season = await SeedSeasonAsync("Campaña 2026");
        var worker = await SeedWorkerAsync("Juan");
        var activity = await SeedActivityAsync(plot.Id, season.Id, worker.Id, null);

        activity.Delete(_userId);
        await Db.SaveChangesAsync();

        var usage = await Repository().CountUsageAsync(MasterKind.Plot, _workspaceId, plot.Id);

        usage.Total.Should().Be(1);
    }

    // ── El recuento por Workspace que alimenta el listado ───────────────────────────────────────

    [Fact]
    public async Task ElRecuentoPorWorkspace_Deberia_SumarLosTiposDeReferencia_Y_OmitirLoNoUsado()
    {
        await SeedWorkspaceAsync("-grouped");
        var used = await SeedPlotAsync("Bancal usado");
        var unused = await SeedPlotAsync("Bancal nuevo");
        var season = await SeedSeasonAsync("Campaña 2026");
        var worker = await SeedWorkerAsync("Juan");
        await SeedActivityAsync(used.Id, season.Id, worker.Id, null);
        await SeedActivityAsync(used.Id, season.Id, worker.Id, null);
        await SeedHarvestAsync(used.Id, season.Id);

        var counts = await Repository().CountUsageByWorkspaceAsync(MasterKind.Plot, _workspaceId);

        counts[used.Id].Should().Be(3);
        counts.Should().NotContainKey(unused.Id);
    }

    // ── CA-1 y CA-5 · borrado ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Borrar_Deberia_QuitarLaFichaDeLaBaseDeDatos()
    {
        await SeedWorkspaceAsync("-del-ok");
        var plot = await SeedPlotAsync("Creado por error");

        await Repository().DeleteAsync(MasterKind.Plot, _workspaceId, plot.Id);

        await using var fresh = NewDb();
        (await fresh.Plots.AnyAsync(p => p.Id == plot.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Borrar_Deberia_TraducirLaClaveAjenaA422_Cuando_AlguienRegistraEntreMedias()
    {
        // La carrera que la comprobación de uso no puede evitar por sí sola: la FK RESTRICT es la red
        // por debajo, igual que el índice único lo es de la guarda de nombres duplicados (MVP-207).
        await SeedWorkspaceAsync("-del-carrera");
        var plot = await SeedPlotAsync("Bancal");
        var season = await SeedSeasonAsync("Campaña 2026");
        var worker = await SeedWorkerAsync("Juan");
        await SeedActivityAsync(plot.Id, season.Id, worker.Id, null);

        var act = async () => await Repository().DeleteAsync(MasterKind.Plot, _workspaceId, plot.Id);

        await act.Should().ThrowAsync<MasterOperationException>();
    }

    [Fact]
    public async Task BorrarUnaTemporada_Deberia_DejarAlMiembroResolviendoElDefecto()
    {
        // La preferencia de temporada de trabajo (MVP-209) referencia a la temporada pero no es
        // histórico: su FK es `ON DELETE SET NULL`, así que borrarla no deja nada colgando.
        var (workspaceId, userId) = await SeedWorkspaceAsync("-del-working");
        var season = await SeedSeasonAsync("Campaña 2026");
        await Db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
            .ExecuteUpdateAsync(set => set.SetProperty(m => m.ActiveSeasonId, season.Id));

        await Repository().DeleteAsync(MasterKind.Season, workspaceId, season.Id);

        await using var fresh = NewDb();
        var member = await fresh.WorkspaceMembers
            .FirstAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
        member.ActiveSeasonId.Should().BeNull();
    }

    // ── CA-3 y CA-5 · fusión ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FusionarTerrenos_Deberia_ReapuntarLosTresTiposDeReferencia_Y_BorrarElAbsorbido()
    {
        // CA-3 contado como pide el criterio: los registros de los dos antes, y la suma en el
        // superviviente después.
        await SeedWorkspaceAsync("-merge-plot");
        var survivor = await SeedPlotAsync("Bancal de arriba");
        var absorbed = await SeedPlotAsync("Bancal de arriba (2)");
        var season = await SeedSeasonAsync("Campaña 2026");
        var worker = await SeedWorkerAsync("Juan");

        await SeedActivityAsync(survivor.Id, season.Id, worker.Id, null);
        await SeedActivityAsync(absorbed.Id, season.Id, worker.Id, null);
        await SeedActivityAsync(absorbed.Id, season.Id, worker.Id, null);
        await SeedHarvestAsync(absorbed.Id, season.Id);
        await SeedConsumptionAsync(absorbed.Id, season.Id);

        var repository = Repository();
        var before = (await repository.CountUsageAsync(MasterKind.Plot, _workspaceId, survivor.Id)).Total
                     + (await repository.CountUsageAsync(MasterKind.Plot, _workspaceId, absorbed.Id)).Total;
        before.Should().Be(5);

        var reassigned = await repository.MergeAsync(
            MasterKind.Plot, _workspaceId, survivor.Id, absorbed.Id, _userId);

        reassigned.Should().Be(4);

        await using var fresh = NewDb();
        var after = await new MasterRepository(fresh)
            .CountUsageAsync(MasterKind.Plot, _workspaceId, survivor.Id);
        after.Total.Should().Be(before);
        (await fresh.Plots.AnyAsync(p => p.Id == absorbed.Id)).Should().BeFalse();
        // CA-5 — ninguna clave ajena se queda sin resolver.
        (await fresh.Activities.AnyAsync(a => a.PlotId == absorbed.Id)).Should().BeFalse();
        (await fresh.Harvests.AnyAsync(h => h.PlotId == absorbed.Id)).Should().BeFalse();
        (await fresh.PurchaseConsumptions.AnyAsync(c => c.PlotId == absorbed.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task FusionarTemporadas_Deberia_ReapuntarLosCuatroTiposYLaTemporadaDeTrabajo()
    {
        var (workspaceId, userId) = await SeedWorkspaceAsync("-merge-season");
        var survivor = await SeedSeasonAsync("Campaña 2026");
        var absorbed = await SeedSeasonAsync("Campaña 2026 (2)");
        var plot = await SeedPlotAsync("Bancal");
        var worker = await SeedWorkerAsync("Juan");

        await SeedActivityAsync(plot.Id, absorbed.Id, worker.Id, null);
        await SeedHarvestAsync(plot.Id, absorbed.Id);
        await SeedPurchaseAsync(absorbed.Id);
        await SeedConsumptionAsync(plot.Id, absorbed.Id);
        await Db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
            .ExecuteUpdateAsync(set => set.SetProperty(m => m.ActiveSeasonId, absorbed.Id));

        var reassigned = await Repository().MergeAsync(
            MasterKind.Season, workspaceId, survivor.Id, absorbed.Id, userId);

        reassigned.Should().Be(4);

        await using var fresh = NewDb();
        (await new MasterRepository(fresh).CountUsageAsync(MasterKind.Season, workspaceId, survivor.Id))
            .Total.Should().Be(4);
        (await fresh.Seasons.AnyAsync(s => s.Id == absorbed.Id)).Should().BeFalse();
        // Quien la tenía fijada pasa a la superviviente, no al defecto: nadie pidió cambiar de campaña.
        var member = await fresh.WorkspaceMembers
            .FirstAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
        member.ActiveSeasonId.Should().Be(survivor.Id);
    }

    [Fact]
    public async Task FusionarTrabajadores_Deberia_MoverLaVersionDeLoQueReapunta()
    {
        // ADR-0005 — es lo que separa la fusión de un UPDATE masivo: el registro reapuntado sube de
        // versión, así que quien lo tuviera abierto recibe 409 al guardar en vez de reescribirlo con
        // el responsable que ya no existe.
        await SeedWorkspaceAsync("-merge-version");
        var survivor = await SeedWorkerAsync("Juan Pérez");
        var absorbed = await SeedWorkerAsync("Juan Pérez (2)");
        var plot = await SeedPlotAsync("Bancal");
        var season = await SeedSeasonAsync("Campaña 2026");
        var activity = await SeedActivityAsync(plot.Id, season.Id, absorbed.Id, null);
        activity.Version.Should().Be(1);

        await Repository().MergeAsync(MasterKind.Worker, _workspaceId, survivor.Id, absorbed.Id, _userId);

        await using var fresh = NewDb();
        var reassigned = await fresh.Activities.FirstAsync(a => a.Id == activity.Id);
        reassigned.WorkerId.Should().Be(survivor.Id);
        reassigned.Version.Should().Be(2);
        reassigned.UpdatedBy.Should().Be(_userId);
    }

    [Fact]
    public async Task FusionarTareas_Deberia_ReapuntarSoloLasActividadesDelCatalogo()
    {
        await SeedWorkspaceAsync("-merge-task");
        var survivor = await SeedTaskAsync("Poda");
        var absorbed = await SeedTaskAsync("Poda (2)");
        var plot = await SeedPlotAsync("Bancal");
        var season = await SeedSeasonAsync("Campaña 2026");
        var worker = await SeedWorkerAsync("Juan");
        await SeedActivityAsync(plot.Id, season.Id, worker.Id, absorbed.Id);
        var freeText = await SeedActivityAsync(plot.Id, season.Id, worker.Id, null);

        var reassigned = await Repository().MergeAsync(
            MasterKind.Task, _workspaceId, survivor.Id, absorbed.Id, _userId);

        reassigned.Should().Be(1);

        await using var fresh = NewDb();
        (await fresh.Tasks.AnyAsync(t => t.Id == absorbed.Id)).Should().BeFalse();
        // La actividad con tarea en texto libre no se toca: ni su versión sube.
        (await fresh.Activities.FirstAsync(a => a.Id == freeText.Id)).Version.Should().Be(1);
    }

    [Fact]
    public async Task Fusionar_Deberia_FallarEntera_Cuando_OtraPersonaEditaUnRegistroQueSeReapunta()
    {
        // ADR-0005 llevado a la fusión: si alguien tocó uno de los registros después de leerlo, no se
        // completa nada. Es preferible repetir la fusión a pisar en silencio la corrección de otro.
        await SeedWorkspaceAsync("-merge-conflict");
        var survivor = await SeedPlotAsync("Bancal de arriba");
        var absorbed = await SeedPlotAsync("Bancal de arriba (2)");
        var season = await SeedSeasonAsync("Campaña 2026");
        var worker = await SeedWorkerAsync("Juan");
        var activity = await SeedActivityAsync(absorbed.Id, season.Id, worker.Id, null);

        // Un contexto propio lee la actividad y la deja en versión 1 en memoria; entretanto, otra
        // sesión la corrige y la sube a 2.
        await using var mergingContext = NewDb();
        var repository = new MasterRepository(mergingContext);
        await mergingContext.Activities.FirstAsync(a => a.Id == activity.Id);

        await using (var other = NewDb())
        {
            var concurrent = await other.Activities.FirstAsync(a => a.Id == activity.Id);
            // Una corrección cualquiera: lo que importa es que suba la versión.
            concurrent.Update(
                absorbed.Id, season.Id, worker.Id, Today, 5m, null, "Corregida a mano", 50m, null, _userId);
            await other.SaveChangesAsync();
        }

        var act = async () => await repository.MergeAsync(
            MasterKind.Plot, _workspaceId, survivor.Id, absorbed.Id, _userId);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();

        await using var fresh = NewDb();
        (await fresh.Plots.AnyAsync(p => p.Id == absorbed.Id)).Should().BeTrue();
    }
}
