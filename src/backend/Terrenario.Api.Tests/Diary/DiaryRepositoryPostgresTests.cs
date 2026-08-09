using FluentAssertions;
using Terrenario.Api.Application.Diary;
using Terrenario.Api.Domain.Activities;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Diary;
using Terrenario.Api.Domain.Harvests;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Purchases;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Tasks;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workers;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Tests.Integration;

namespace Terrenario.Api.Tests.Diary;

/// <summary>
/// MVP-506 — El diario unificado contra <b>PostgreSQL real</b>.
///
/// Sustituye a <c>DiaryQueryServiceTests</c>, que mezclaba los cuatro puertos en memoria con
/// repositorios doblados. Esa lógica ya no existe: la mezcla, el orden, la paginación, la búsqueda y
/// los totales los resuelve SQL (`P-051`/`P-052`/`P-056`), así que probarlos con mocks sería probar
/// una simulación de lo que hace la base de datos. Se conservan **todos** los comportamientos que
/// aquellos tests fijaban, y se añaden los que la historia trae.
/// </summary>
public sealed class DiaryRepositoryPostgresTests : RepositoryTestBase
{
    private DiaryRepository Repository => new(Db);

    private Guid _workspaceId;
    private Guid _userId;
    private Guid _hoyaId;
    private Guid _cerroId;
    private Guid _antonioId;
    private Guid _luciaId;
    private Guid _seasonId;
    private Guid _otherSeasonId;
    private Guid _podaTaskId;

    /// <summary>
    /// Siembra un Workspace con dos terrenos, dos responsables y dos temporadas. Se hace por test —no
    /// en el constructor— porque la base la prepara <c>InitializeAsync</c>.
    /// </summary>
    private async Task SeedMastersAsync()
    {
        var user = User.Create("google-sub", "Andrés", "andres@ejemplo.com");
        Db.Users.Add(user);
        _userId = user.Id;

        var workspace = Workspace.Create(user.Id, "Finca El Olivar");
        Db.Workspaces.Add(workspace);
        _workspaceId = workspace.Id;

        var hoya = Plot.Create(_workspaceId, "La Hoya", PlotOwnershipTypes.Propia);
        var cerro = Plot.Create(_workspaceId, "El Cerro", PlotOwnershipTypes.Propia);
        Db.Plots.AddRange(hoya, cerro);
        _hoyaId = hoya.Id;
        _cerroId = cerro.Id;

        var antonio = Worker.Create(_workspaceId, "Antonio Ruiz", null);
        var lucia = Worker.Create(_workspaceId, "Lucía Pérez", null);
        Db.Workers.AddRange(antonio, lucia);
        _antonioId = antonio.Id;
        _luciaId = lucia.Id;

        var season = Season.Create(_workspaceId, "Campaña 2025/26", new DateOnly(2025, 10, 1), new DateOnly(2026, 3, 31));
        var other = Season.Create(_workspaceId, "Campaña 2024/25", new DateOnly(2024, 10, 1), new DateOnly(2025, 3, 31));
        Db.Seasons.AddRange(season, other);
        _seasonId = season.Id;
        _otherSeasonId = other.Id;

        var poda = TaskItem.Create(_workspaceId, "Poda");
        Db.Tasks.Add(poda);
        _podaTaskId = poda.Id;

        await Db.SaveChangesAsync();
    }

    private Activity NewActivity(
        DateOnly date,
        Guid? plotId = null,
        Guid? workerId = null,
        Guid? taskId = null,
        string? taskText = null,
        decimal cost = 100m,
        string? description = null,
        Guid? seasonId = null)
        => Activity.Create(
            _workspaceId,
            plotId ?? _hoyaId,
            seasonId ?? _seasonId,
            workerId ?? _antonioId,
            date,
            hours: 4m,
            taskId: taskId,
            taskText: taskId is null ? taskText ?? "Riego" : null,
            manualCost: cost,
            description: description,
            userId: _userId);

    private Harvest NewHarvest(DateOnly date, decimal kgs = 1000m, Guid? plotId = null)
        => Harvest.Create(
            _workspaceId, plotId ?? _hoyaId, _seasonId, date,
            HarvestProducts.AceitunaOlivar, kgs, HarvestDestinations.AceiteParaVenta,
            yield: null, liters: null, unitPrice: null, userId: _userId);

    private Purchase NewPurchase(DateOnly date, string product = "Abono foliar", decimal cost = 400m)
        => Purchase.Create(_workspaceId, _seasonId, date, product, totalQuantity: 200m, totalCost: cost, userId: _userId);

    private static DiaryPageRequest FirstPage(int limit = 50) => new(1, limit);

    private Task<IReadOnlyList<DiaryRow>> PageAsync(DiaryFilter? filter = null, DiaryPageRequest? page = null)
        => Repository.ListPageAsync(_workspaceId, filter ?? new DiaryFilter(), page ?? FirstPage());

    private Task<DiaryTotals> TotalsAsync(DiaryFilter? filter = null)
        => Repository.GetTotalsAsync(_workspaceId, filter ?? new DiaryFilter());

    // ── Mezcla y orden (comportamiento heredado de MVP-305/401) ─────────────────

    [Fact]
    public async Task Deberia_MezclarLosCuatroTipos_Y_OrdenarPorFechaDeNegocio()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 12)));
        Db.Harvests.Add(NewHarvest(new DateOnly(2025, 12, 5)));
        Db.Purchases.Add(NewPurchase(new DateOnly(2025, 11, 2)));
        Db.PurchaseConsumptions.Add(PurchaseConsumption.RegisterWithoutPurchase(
            _workspaceId, _seasonId, _cerroId, new DateOnly(2025, 11, 20), "Cal", 10m, _userId));
        await Db.SaveChangesAsync();

        var rows = await PageAsync();

        rows.Select(r => r.Type).Should().BeEquivalentTo(
            [DiaryEntryTypes.Activity, DiaryEntryTypes.Harvest, DiaryEntryTypes.Purchase, DiaryEntryTypes.Consumption]);
        rows.Select(r => r.Date).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Deberia_DesempatarPorFechaDeCaptura_Descendente()
    {
        await SeedMastersAsync();
        var misma = new DateOnly(2025, 11, 12);

        // Se guardan por separado para que la fecha de captura difiera de verdad.
        var primera = NewActivity(misma, taskText: "Primera");
        Db.Activities.Add(primera);
        await Db.SaveChangesAsync();

        var segunda = NewActivity(misma, taskText: "Segunda");
        Db.Activities.Add(segunda);
        await Db.SaveChangesAsync();

        var rows = await PageAsync();

        // A igualdad de fecha de negocio, lo capturado más tarde va primero: es el orden en que la
        // persona recuerda haberlo apuntado (RN-033).
        rows.Select(r => r.Title).Should().ContainInOrder("Segunda", "Primera");
    }

    [Fact]
    public async Task Deberia_ResolverLaTareaDelCatalogo_O_ElTextoLibre()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 12), taskId: _podaTaskId));
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 11), taskText: "Recoger piedras"));
        await Db.SaveChangesAsync();

        var rows = await PageAsync();

        // RN-025 — para quien lee, una labor es una labor: el diario no distingue el origen.
        rows.Select(r => r.Title).Should().ContainInOrder("Poda", "Recoger piedras");
        rows.Single(r => r.Title == "Poda").TaskId.Should().Be(_podaTaskId);
        rows.Single(r => r.Title == "Recoger piedras").TaskId.Should().BeNull();
    }

    [Fact]
    public async Task Deberia_ProyectarLaCosecha_ConKilosYDestino_YSinCoste()
    {
        await SeedMastersAsync();
        Db.Harvests.Add(NewHarvest(new DateOnly(2025, 12, 5), kgs: 4200m));
        await Db.SaveChangesAsync();

        var cosecha = (await PageAsync()).Single();

        cosecha.Kgs.Should().Be(4200m);
        cosecha.Destination.Should().Be(HarvestDestinations.AceiteParaVenta);
        // RN-029 — una cosecha no tiene coste: no es «gratis», es que la magnitud no aplica.
        cosecha.Cost.Should().Be(0m);
    }

    [Fact]
    public async Task Deberia_DerivarElRendimiento_Cuando_SoloHayLitros()
    {
        await SeedMastersAsync();
        Db.Harvests.Add(Harvest.Create(
            _workspaceId, _hoyaId, _seasonId, new DateOnly(2025, 12, 5),
            HarvestProducts.AceitunaOlivar, 4200m, HarvestDestinations.AceiteParaVenta,
            yield: null, liters: 840m, unitPrice: null, userId: _userId));
        await Db.SaveChangesAsync();

        // RN-014 — 840 L sobre 4.200 kg ⇒ 20 L/100kg. Para quien lee es el mismo dato que si se
        // hubiera declarado.
        (await PageAsync()).Single().Yield.Should().Be(20m);
    }

    [Fact]
    public async Task Deberia_AvisarDeLaFechaFueraDeTemporada_Cuando_SeSaleDelRango()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 12), taskText: "Dentro"));
        Db.Activities.Add(NewActivity(new DateOnly(2026, 6, 1), taskText: "Fuera"));
        await Db.SaveChangesAsync();

        var service = new DiaryQueryService(Repository);
        var result = await service.HandleAsync(_workspaceId, new DiaryFilter(), FirstPage());

        // RN-023 — es un aviso, no un bloqueo, y se deriva en lectura.
        result.Entries.Single(e => e.Title == "Fuera").IsOutOfSeasonRange.Should().BeTrue();
        result.Entries.Single(e => e.Title == "Dentro").IsOutOfSeasonRange.Should().BeFalse();
    }

    [Fact]
    public async Task Deberia_ExcluirLoEliminadoLogicamente_Cuando_SeLeeElDiario()
    {
        await SeedMastersAsync();
        var actividad = NewActivity(new DateOnly(2025, 11, 12));
        Db.Activities.Add(actividad);
        await Db.SaveChangesAsync();

        actividad.Delete(_userId);
        await Db.SaveChangesAsync();

        // RN-037 — el borrado es lógico: desaparece del diario, no de la base de datos.
        (await PageAsync()).Should().BeEmpty();
        (await TotalsAsync()).Total.Should().Be(0);
    }

    // ── Totales de cabecera ────────────────────────────────────────────────────

    [Fact]
    public async Task Deberia_ResumirElDiarioCompleto_ParaLaCabecera()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 12), cost: 75m));
        Db.Harvests.Add(NewHarvest(new DateOnly(2025, 12, 5), kgs: 4200m));
        Db.Purchases.Add(NewPurchase(new DateOnly(2025, 11, 2), cost: 400m));
        Db.PurchaseConsumptions.Add(PurchaseConsumption.RegisterWithoutPurchase(
            _workspaceId, _seasonId, _cerroId, new DateOnly(2025, 11, 20), "Cal", 10m, _userId));
        await Db.SaveChangesAsync();

        var totals = await TotalsAsync();

        totals.Total.Should().Be(4);
        totals.Activities.Should().Be(1);
        totals.Harvests.Should().Be(1);
        totals.Purchases.Should().Be(1);
        totals.Consumptions.Should().Be(1);
        totals.TotalKg.Should().Be(4200m);
        // RN-032 — un consumo sin compra vale 0 porque se desconoce, no porque fuera gratis.
        totals.ConsumptionsWithoutPurchase.Should().Be(1);
        totals.TotalCost.Should().Be(475m);
    }

    [Fact]
    public async Task NoDeberia_ContarDosVeces_ElDineroDeUnaCompraYaRepartida()
    {
        await SeedMastersAsync();
        var compra = NewPurchase(new DateOnly(2025, 11, 2), cost: 400m);
        Db.Purchases.Add(compra);
        await Db.SaveChangesAsync();

        Db.PurchaseConsumptions.Add(PurchaseConsumption.ImputeFromPurchase(
            _workspaceId, compra.Id, _seasonId, compra.Product, compra.UnitPrice,
            _hoyaId, new DateOnly(2025, 11, 8), 50m, _userId));
        await Db.SaveChangesAsync();

        var totals = await TotalsAsync();

        // `R-01` de MVP-399 — la imputación reparte dinero que la compra ya aportó: sumarla contaría
        // el mismo gasto dos veces, y era la cifra de cabecera de la vista principal.
        totals.TotalCost.Should().Be(400m);
        totals.ImputedCost.Should().Be(100m);
        totals.ConsumptionsWithoutPurchase.Should().Be(0);
    }

    [Fact]
    public async Task Deberia_ResumirElConjuntoFiltrado_Y_NoLaPagina()
    {
        await SeedMastersAsync();
        for (var day = 1; day <= 10; day++)
            Db.Activities.Add(NewActivity(new DateOnly(2025, 11, day), cost: 10m));
        await Db.SaveChangesAsync();

        var page = await PageAsync(page: new DiaryPageRequest(1, 3));
        var totals = await TotalsAsync();

        page.Should().HaveCount(3);
        // La cabecera es del diario entero: si contara la página, cambiaría en cada avance.
        totals.Total.Should().Be(10);
        totals.TotalCost.Should().Be(100m);
    }

    // ── Paginación (P-051) ─────────────────────────────────────────────────────

    [Fact]
    public async Task Deberia_RecorrerElDiarioSinRepetirNiPerderEntradas_Cuando_SePaginaEntero()
    {
        await SeedMastersAsync();
        for (var day = 1; day <= 25; day++)
            Db.Activities.Add(NewActivity(new DateOnly(2025, 11, day), taskText: $"Labor {day:00}"));
        await Db.SaveChangesAsync();

        var recogidas = new List<Guid>();
        for (var page = 1; page <= 3; page++)
            recogidas.AddRange((await PageAsync(page: new DiaryPageRequest(page, 10))).Select(r => r.Id));

        recogidas.Should().HaveCount(25);
        // La prueba de que la paginación es real: ninguna entrada repetida ni perdida entre páginas.
        recogidas.Distinct().Should().HaveCount(25);
    }

    [Fact]
    public async Task Deberia_SerEstable_Cuando_VariasEntradasCompartenFechaYCaptura()
    {
        await SeedMastersAsync();
        var misma = new DateOnly(2025, 11, 12);
        // Alta en lote: las diez comparten fecha de negocio y prácticamente la de captura. Sin un
        // desempate determinista, paginar aquí repetiría unas y perdería otras.
        for (var i = 0; i < 10; i++) Db.Activities.Add(NewActivity(misma, taskText: $"Labor {i}"));
        await Db.SaveChangesAsync();

        var primera = await PageAsync(page: new DiaryPageRequest(1, 5));
        var segunda = await PageAsync(page: new DiaryPageRequest(2, 5));

        primera.Select(r => r.Id).Should().NotIntersectWith(segunda.Select(r => r.Id));
        primera.Concat(segunda).Select(r => r.Id).Distinct().Should().HaveCount(10);
    }

    [Fact]
    public async Task Deberia_DevolverVacio_Cuando_SePideUnaPaginaMasAllaDelFinal()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 12)));
        await Db.SaveChangesAsync();

        (await PageAsync(page: new DiaryPageRequest(5, 20))).Should().BeEmpty();
    }

    // ── Filtros en servidor ────────────────────────────────────────────────────

    [Fact]
    public async Task Deberia_DejarFueraLasCompras_Y_ConservarLasCosechas_Cuando_SeFiltraPorTerreno()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 12), plotId: _hoyaId));
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 13), plotId: _cerroId));
        Db.Harvests.Add(NewHarvest(new DateOnly(2025, 12, 5), plotId: _hoyaId));
        Db.Purchases.Add(NewPurchase(new DateOnly(2025, 11, 2)));
        await Db.SaveChangesAsync();

        var rows = await PageAsync(new DiaryFilter(PlotId: _hoyaId));

        // Una compra es del Workspace, no de un terreno: queda fuera por definición, no por olvido.
        rows.Select(r => r.Type).Should().NotContain(DiaryEntryTypes.Purchase);
        // La cosecha sí es de un terreno concreto (RN-001), así que el filtro la conserva.
        rows.Select(r => r.Type).Should().Contain(DiaryEntryTypes.Harvest);
        rows.Should().OnlyContain(r => r.PlotId == _hoyaId);
    }

    [Fact]
    public async Task Deberia_DevolverSoloEsosTipos_Cuando_SeFiltraPorTipo()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 12)));
        Db.Harvests.Add(NewHarvest(new DateOnly(2025, 12, 5)));
        Db.Purchases.Add(NewPurchase(new DateOnly(2025, 11, 2)));
        await Db.SaveChangesAsync();

        var rows = await PageAsync(new DiaryFilter(Types: [DiaryEntryTypes.Harvest]));

        rows.Should().ContainSingle().Which.Type.Should().Be(DiaryEntryTypes.Harvest);
    }

    [Fact]
    public async Task Deberia_AcotarPorFechaDeNegocio_Y_PorTemporada()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 12)));
        Db.Activities.Add(NewActivity(new DateOnly(2025, 12, 20)));
        Db.Activities.Add(NewActivity(new DateOnly(2024, 11, 5), seasonId: _otherSeasonId));
        await Db.SaveChangesAsync();

        (await PageAsync(new DiaryFilter(From: new DateOnly(2025, 12, 1)))).Should().ContainSingle();
        (await PageAsync(new DiaryFilter(SeasonId: _otherSeasonId))).Should().ContainSingle();
    }

    // ── Filtro por responsable (P-056) ─────────────────────────────────────────

    [Fact]
    public async Task Deberia_DevolverSoloSusLabores_Cuando_SeFiltraPorResponsable()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 12), workerId: _antonioId, taskText: "Poda de Antonio"));
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 13), workerId: _luciaId, taskText: "Riego de Lucía"));
        await Db.SaveChangesAsync();

        var rows = await PageAsync(new DiaryFilter(WorkerId: _antonioId));

        rows.Should().ContainSingle().Which.WorkerName.Should().Be("Antonio Ruiz");
    }

    [Fact]
    public async Task Deberia_DejarFueraLoQueNoTieneResponsable_Cuando_SeFiltraPorResponsable()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 12), workerId: _antonioId));
        Db.Harvests.Add(NewHarvest(new DateOnly(2025, 12, 5)));
        Db.Purchases.Add(NewPurchase(new DateOnly(2025, 11, 2)));
        Db.PurchaseConsumptions.Add(PurchaseConsumption.RegisterWithoutPurchase(
            _workspaceId, _seasonId, _cerroId, new DateOnly(2025, 11, 20), "Cal", 10m, _userId));
        await Db.SaveChangesAsync();

        var rows = await PageAsync(new DiaryFilter(WorkerId: _antonioId));
        var totals = await TotalsAsync(new DiaryFilter(WorkerId: _antonioId));

        // Solo la labor tiene responsable: los otros tres tipos quedan fuera por definición, igual
        // que la compra al filtrar por terreno.
        rows.Should().ContainSingle().Which.Type.Should().Be(DiaryEntryTypes.Activity);
        totals.Total.Should().Be(1);
    }

    [Fact]
    public async Task Deberia_QuedarseVacio_Cuando_SeCombinanResponsableYUnTipoSinResponsable()
    {
        await SeedMastersAsync();
        Db.Purchases.Add(NewPurchase(new DateOnly(2025, 11, 2)));
        await Db.SaveChangesAsync();

        var filtro = new DiaryFilter(Types: [DiaryEntryTypes.Purchase], WorkerId: _antonioId);

        // No queda ninguna fuente: devolver vacío es más honesto que inventar un resultado.
        (await PageAsync(filtro)).Should().BeEmpty();
        (await TotalsAsync(filtro)).Total.Should().Be(0);
    }

    // ── Búsqueda en servidor (P-052) ───────────────────────────────────────────

    [Fact]
    public async Task Deberia_BuscarPorTitular_Cuando_SeBuscaEnServidor()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 12), taskText: "Poda de formación"));
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 13), taskText: "Riego por goteo"));
        await Db.SaveChangesAsync();

        var rows = await PageAsync(new DiaryFilter(Search: "poda"));

        rows.Should().ContainSingle().Which.Title.Should().Be("Poda de formación");
    }

    [Fact]
    public async Task Deberia_BuscarTambienPorTerrenoResponsableYDescripcion()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(
            new DateOnly(2025, 11, 12), plotId: _cerroId, workerId: _luciaId,
            taskText: "Riego", description: "Con la cuba nueva"));
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 13), taskText: "Poda"));
        await Db.SaveChangesAsync();

        (await PageAsync(new DiaryFilter(Search: "cerro"))).Should().ContainSingle();
        (await PageAsync(new DiaryFilter(Search: "lucía"))).Should().ContainSingle();
        (await PageAsync(new DiaryFilter(Search: "cuba"))).Should().ContainSingle();
    }

    [Fact]
    public async Task Deberia_BuscarSobreElDiarioCompleto_Y_NoSobreLaPagina()
    {
        await SeedMastersAsync();
        // La aguja se siembra la **última** en fecha de negocio, así que cae fuera de la primera
        // página. Es el caso que `P-052` describe: buscar sobre una página no es buscar.
        for (var day = 5; day <= 25; day++)
            Db.Activities.Add(NewActivity(new DateOnly(2025, 11, day), taskText: $"Labor {day:00}"));
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 1), taskText: "Sulfatado excepcional"));
        await Db.SaveChangesAsync();

        var rows = await PageAsync(new DiaryFilter(Search: "sulfatado"), new DiaryPageRequest(1, 10));

        rows.Should().ContainSingle().Which.Title.Should().Be("Sulfatado excepcional");
    }

    [Fact]
    public async Task Deberia_BuscarSinDistinguirMayusculas_Y_EnLosCuatroTipos()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 12), taskText: "Aplicar Abono líquido"));
        Db.Purchases.Add(NewPurchase(new DateOnly(2025, 11, 2), product: "ABONO foliar"));
        Db.PurchaseConsumptions.Add(PurchaseConsumption.RegisterWithoutPurchase(
            _workspaceId, _seasonId, _cerroId, new DateOnly(2025, 11, 20), "abono de fondo", 10m, _userId));
        await Db.SaveChangesAsync();

        var rows = await PageAsync(new DiaryFilter(Search: "AbOnO"));

        rows.Select(r => r.Type).Should().BeEquivalentTo(
            [DiaryEntryTypes.Activity, DiaryEntryTypes.Purchase, DiaryEntryTypes.Consumption]);
    }

    [Fact]
    public async Task Deberia_ContarSoloLoEncontrado_Cuando_HayBusqueda()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 12), taskText: "Poda", cost: 50m));
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 13), taskText: "Riego", cost: 30m));
        await Db.SaveChangesAsync();

        var totals = await TotalsAsync(new DiaryFilter(Search: "poda"));

        // La cabecera resume lo que la búsqueda deja, no el diario entero: si no, el usuario vería un
        // gasto que no corresponde a lo que tiene delante.
        totals.Total.Should().Be(1);
        totals.TotalCost.Should().Be(50m);
    }

    [Fact]
    public async Task Deberia_AislarPorWorkspace_Cuando_ConvivenDos()
    {
        await SeedMastersAsync();
        Db.Activities.Add(NewActivity(new DateOnly(2025, 11, 12)));
        await Db.SaveChangesAsync();

        var otroUser = User.Create("google-sub-otro", "Lucía", "lucia@ejemplo.com");
        Db.Users.Add(otroUser);
        var otro = Workspace.Create(otroUser.Id, "Cortijo del Río");
        Db.Workspaces.Add(otro);
        await Db.SaveChangesAsync();

        (await Repository.ListPageAsync(otro.Id, new DiaryFilter(), FirstPage())).Should().BeEmpty();
        (await Repository.GetTotalsAsync(otro.Id, new DiaryFilter())).Total.Should().Be(0);
    }
}

/// <summary>Catálogo cerrado <c>diary_entry_type</c>: vocabulario de dominio, no texto de UI.</summary>
public sealed class DiaryEntryTypesTests
{
    [Fact]
    public void Deberia_AdmitirLosCuatroTipos_Y_RechazarElResto()
    {
        DiaryEntryTypes.Supported.Should().BeEquivalentTo(["actividad", "compra", "consumo", "cosecha"]);
        DiaryEntryTypes.IsSupported("cosecha").Should().BeTrue();
        DiaryEntryTypes.IsSupported("harvest").Should().BeFalse();
        DiaryEntryTypes.IsSupported(null).Should().BeFalse();
    }
}
