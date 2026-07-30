using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Dashboard;
using Terrenario.Api.Domain.Harvests;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Tests.Dashboard;

/// <summary>
/// Tests del dashboard (MVP-403): resumen de temporada y kg por destino, con los valores por defecto de
/// RN-008, la unidad canónica de RN-013 y la taxonomía cerrada de RN-012.
/// </summary>
public class DashboardQueryServiceTests
{
    private readonly IHarvestRepository _harvests = Substitute.For<IHarvestRepository>();
    private readonly ISeasonRepository _seasons = Substitute.For<ISeasonRepository>();
    private readonly IPlotRepository _plots = Substitute.For<IPlotRepository>();

    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private readonly Season _active = Season.Create(
        WorkspaceId, "2026/2027", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28));

    private readonly Plot _alto = Plot.Create(WorkspaceId, "Olivar Alto", "propia");
    private readonly Plot _bajo = Plot.Create(WorkspaceId, "Olivar Bajo", "propia");
    private readonly Plot _retirado = Plot.Create(WorkspaceId, "Olivar Viejo", "cedida");

    public DashboardQueryServiceTests()
    {
        _active.Activate();
        _retirado.SetActive(false);

        _seasons.FindActiveByWorkspaceAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns(_active);
        _seasons.FindByIdAsync(WorkspaceId, _active.Id, Arg.Any<CancellationToken>()).Returns(_active);
        _seasons.ListByWorkspaceAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns([_active]);
        _plots.ListByWorkspaceAsync(WorkspaceId, null, null, Arg.Any<CancellationToken>())
            .Returns([_alto, _bajo, _retirado]);
    }

    private DashboardQueryService CreateSut()
        => new(_harvests, _seasons, new DashboardScopeResolver(_seasons, _plots));

    private HarvestAggregateRow Row(
        decimal kgs,
        decimal? yield = null,
        decimal? liters = null,
        string destination = HarvestDestinations.AceiteParaVenta,
        Guid? plotId = null,
        Guid? seasonId = null,
        DateOnly? date = null)
        => new(
            plotId ?? _alto.Id,
            seasonId ?? _active.Id,
            date ?? new DateOnly(2026, 10, 15),
            kgs,
            yield,
            liters,
            destination);

    private void Seed(params HarvestAggregateRow[] rows)
        => _harvests
            .ListAggregateRowsAsync(WorkspaceId, Arg.Any<HarvestAggregateFilter>(), Arg.Any<CancellationToken>())
            .Returns(rows);

    // ── Ámbito por defecto (RN-008) ─────────────────────────────────────────

    [Fact]
    public async Task Deberia_ResolverPorDefecto_LaTemporadaActivaYLosTerrenosActivos()
    {
        // RN-008 — al primer acceso, todos los terrenos y la temporada actual
        Seed();

        var summary = await CreateSut().GetSummaryAsync(WorkspaceId, new DashboardRequest());

        summary.Scope.Season!.Id.Should().Be(_active.Id);
        summary.Scope.Plots.Select(p => p.Id).Should().BeEquivalentTo([_alto.Id, _bajo.Id]);
        summary.Scope.Plots.Should().NotContain(_retirado);
    }

    [Fact]
    public async Task Deberia_AdmitirUnTerrenoInactivo_SiSePideExplicitamente()
    {
        // Inactivar deja de ofrecer para registros nuevos (MVP-202, CA-3), no borra el histórico:
        // excluir su producción al mirar una campaña pasada falsearía los totales.
        Seed();

        var summary = await CreateSut().GetSummaryAsync(
            WorkspaceId, new DashboardRequest(PlotIds: [_retirado.Id]));

        summary.Scope.Plots.Select(p => p.Id).Should().BeEquivalentTo([_retirado.Id]);
    }

    [Fact]
    public async Task Deberia_DescartarEnSilencio_UnTerrenoQueNoExiste()
    {
        // Es una lectura, no una escritura: quien llega con un filtro obsoleto debe ver el dashboard de
        // lo que sí existe, no una pantalla de error.
        Seed();

        var summary = await CreateSut().GetSummaryAsync(
            WorkspaceId, new DashboardRequest(PlotIds: [_alto.Id, Guid.NewGuid()]));

        summary.Scope.Plots.Select(p => p.Id).Should().BeEquivalentTo([_alto.Id]);
    }

    [Fact]
    public async Task NoDeberia_ConsultarCosechas_SinTemporadaResoluble()
    {
        // RN-021 — toda la producción va asociada a una campaña: sin temporada el ámbito es imposible
        _seasons.FindActiveByWorkspaceAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns((Season?)null);
        Seed();

        var summary = await CreateSut().GetSummaryAsync(WorkspaceId, new DashboardRequest());

        summary.Scope.IsResolvable.Should().BeFalse();
        summary.TotalKg.Should().Be(0m);
        await _harvests.DidNotReceive().ListAggregateRowsAsync(
            Arg.Any<Guid>(), Arg.Any<HarvestAggregateFilter>(), Arg.Any<CancellationToken>());
    }

    // ── Resumen de temporada (CA-1) ─────────────────────────────────────────

    [Fact]
    public async Task Resumen_Deberia_SumarKilos()
    {
        Seed(Row(1000m), Row(500.5m), Row(200m));

        var summary = await CreateSut().GetSummaryAsync(WorkspaceId, new DashboardRequest());

        summary.TotalKg.Should().Be(1700.5m);
        summary.Harvests.Should().Be(3);
    }

    [Fact]
    public async Task Resumen_Deberia_SumarLitros_DeclaradosYDerivados()
    {
        // RN-014 — «litros cuando exista dato»: los declarados y los que salen del rendimiento
        Seed(
            Row(1000m, liters: 200m),
            Row(1000m, yield: 20m)); // 20 L/100kg sobre 1.000 kg = 200 L

        var summary = await CreateSut().GetSummaryAsync(WorkspaceId, new DashboardRequest());

        summary.TotalLiters.Should().Be(400m);
    }

    [Fact]
    public async Task Resumen_Deberia_DejarLosLitrosEnNulo_SinDatoDeAceite()
    {
        // Desconocido no es cero: un resumen que dice «0 litros» afirma que no salió aceite
        Seed(Row(1000m), Row(500m));

        var summary = await CreateSut().GetSummaryAsync(WorkspaceId, new DashboardRequest());

        summary.TotalLiters.Should().BeNull();
        summary.AverageYield.Should().BeNull();
        summary.HarvestsWithOilData.Should().Be(0);
    }

    [Fact]
    public async Task Resumen_Deberia_PonderarElRendimientoPorKilos()
    {
        // Una media aritmética daría (10+20)/2 = 15. Ponderado por kilos: 100 L + 200 L sobre 2.000 kg
        // son 15 L/100kg... así que el caso se elige para que la diferencia se vea:
        // 1.000 kg al 10 % y 9.000 kg al 20 % dan 19 L/100kg, no 15.
        Seed(
            Row(1000m, yield: 10m),
            Row(9000m, yield: 20m));

        var summary = await CreateSut().GetSummaryAsync(WorkspaceId, new DashboardRequest());

        summary.AverageYield.Should().Be(19m);
    }

    [Fact]
    public async Task Resumen_Deberia_PromediarSoloLasPartidasConDatoDeAceite()
    {
        // Una partida sin dato no puede arrastrar la media a la baja: se dice sobre cuántas se promedia
        Seed(
            Row(1000m, yield: 20m),
            Row(4000m));

        var summary = await CreateSut().GetSummaryAsync(WorkspaceId, new DashboardRequest());

        summary.AverageYield.Should().Be(20m);
        summary.Harvests.Should().Be(2);
        summary.HarvestsWithOilData.Should().Be(1);
    }

    // ── Kg por destino (CA-2) ───────────────────────────────────────────────

    [Fact]
    public async Task Destinos_Deberia_AgruparYOrdenarPorKilosDescendentes()
    {
        Seed(
            Row(300m, destination: HarvestDestinations.AceitePersonal),
            Row(1000m, destination: HarvestDestinations.AceiteParaVenta),
            Row(200m, destination: HarvestDestinations.AceiteParaVenta),
            Row(500m, destination: HarvestDestinations.VentaAceituna));

        var (_, totals, totalKg) = await CreateSut().GetKgByDestinationAsync(
            WorkspaceId, new DashboardRequest());

        totals.Select(t => t.Destination).Should().ContainInOrder(
            HarvestDestinations.AceiteParaVenta, HarvestDestinations.VentaAceituna,
            HarvestDestinations.AceitePersonal);
        totals.First().Kg.Should().Be(1200m);
        totalKg.Should().Be(2000m);
    }

    [Fact]
    public async Task Destinos_Deberia_IncluirDesconocido_ComoCategoriaPropia()
    {
        // RN-012 — `desconocido` forma parte de la visualización, no se esconde ni se reparte
        Seed(
            Row(1000m, destination: HarvestDestinations.AceiteParaVenta),
            Row(400m, destination: HarvestDestinations.Desconocido));

        var (_, totals, _) = await CreateSut().GetKgByDestinationAsync(WorkspaceId, new DashboardRequest());

        totals.Should().Contain(t => t.Destination == HarvestDestinations.Desconocido && t.Kg == 400m);
    }

    [Fact]
    public async Task Destinos_NoDeberia_DevolverCategoriasSinKilos()
    {
        // La taxonomía cerrada garantiza que las claves salen del catálogo, no que haya que pintar las
        // cuatro: categorías a cero solo llenarían el widget de ruido
        Seed(Row(1000m, destination: HarvestDestinations.AceiteParaVenta));

        var (_, totals, _) = await CreateSut().GetKgByDestinationAsync(WorkspaceId, new DashboardRequest());

        totals.Should().ContainSingle().Which.Destination.Should().Be(HarvestDestinations.AceiteParaVenta);
    }

    [Fact]
    public async Task Resumen_Y_Destinos_Deberia_CuadrarElTotal()
    {
        // La KB exige que resumen y gráficos no se contradigan: los dos agregan el mismo conjunto
        Seed(
            Row(1000m, destination: HarvestDestinations.AceiteParaVenta),
            Row(400m, destination: HarvestDestinations.Desconocido),
            Row(150.25m, destination: HarvestDestinations.AceitePersonal));

        var sut = CreateSut();
        var summary = await sut.GetSummaryAsync(WorkspaceId, new DashboardRequest());
        var (_, totals, totalKg) = await sut.GetKgByDestinationAsync(WorkspaceId, new DashboardRequest());

        totals.Sum(t => t.Kg).Should().Be(summary.TotalKg);
        totalKg.Should().Be(summary.TotalKg);
    }

    // ── Producción por temporada (P-021) ────────────────────────────────────

    [Fact]
    public async Task Temporadas_Deberia_AgregarProduccionPorCampana()
    {
        // P-021 — el maestro de temporadas (MVP-203) omitió el dato porque no existía HARVEST
        var pasada = Season.Create(WorkspaceId, "2025/2026", new DateOnly(2025, 9, 1), new DateOnly(2026, 2, 28));
        _seasons.ListByWorkspaceAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns([_active, pasada]);
        Seed(
            Row(1000m, seasonId: _active.Id),
            Row(500m, seasonId: _active.Id),
            Row(2000m, seasonId: pasada.Id));

        var result = await CreateSut().GetKgBySeasonAsync(WorkspaceId);

        result.Single(s => s.SeasonId == _active.Id).TotalKg.Should().Be(1500m);
        result.Single(s => s.SeasonId == _active.Id).Harvests.Should().Be(2);
        result.Single(s => s.SeasonId == pasada.Id).TotalKg.Should().Be(2000m);
    }

    [Fact]
    public async Task Temporadas_Deberia_DevolverCeroEnLasCampanasSinCosechas()
    {
        // Una campaña sin cosechas es información («no se recolectó nada»), no ausencia de dato
        var vacia = Season.Create(WorkspaceId, "2024/2025", new DateOnly(2024, 9, 1), null);
        _seasons.ListByWorkspaceAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns([_active, vacia]);
        Seed(Row(1000m, seasonId: _active.Id));

        var result = await CreateSut().GetKgBySeasonAsync(WorkspaceId);

        result.Should().HaveCount(2);
        result.Single(s => s.SeasonId == vacia.Id).TotalKg.Should().Be(0m);
    }

    [Fact]
    public async Task Temporadas_NoDeberia_FiltrarPorTerreno()
    {
        // La tarjeta habla de la campaña completa, no de un subconjunto de parcelas
        _seasons.ListByWorkspaceAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns([_active]);
        Seed(Row(1000m));

        await CreateSut().GetKgBySeasonAsync(WorkspaceId);

        await _harvests.Received(1).ListAggregateRowsAsync(
            WorkspaceId,
            Arg.Is<HarvestAggregateFilter>(f => f.SeasonId == null && (f.PlotIds == null || f.PlotIds.Count == 0)),
            Arg.Any<CancellationToken>());
    }

    // ── Kg por terreno (MVP-404, CA-1 · RN-011) ─────────────────────────────

    [Fact]
    public async Task Terrenos_Deberia_OrdenarPorKilosDescendentes()
    {
        // RN-011 — orden fijo por kg descendente, sin orden manual
        Seed(
            Row(500m, plotId: _alto.Id),
            Row(1200m, plotId: _bajo.Id));

        var (_, totals, totalKg) = await CreateSut().GetKgByPlotAsync(WorkspaceId, new DashboardRequest());

        totals.Select(t => t.PlotId).Should().ContainInOrder(_bajo.Id, _alto.Id);
        totals.First().PlotName.Should().Be("Olivar Bajo");
        totalKg.Should().Be(1700m);
    }

    [Fact]
    public async Task Terrenos_Deberia_DesempatarAlfabeticamente()
    {
        // RN-011 — a igualdad de kg, alfabético por nombre («Alto» antes que «Bajo»)
        Seed(
            Row(1000m, plotId: _bajo.Id),
            Row(1000m, plotId: _alto.Id));

        var (_, totals, _) = await CreateSut().GetKgByPlotAsync(WorkspaceId, new DashboardRequest());

        totals.Select(t => t.PlotName).Should().ContainInOrder("Olivar Alto", "Olivar Bajo");
    }

    [Fact]
    public async Task Terrenos_NoDeberia_IncluirLosQueNoProdujeron()
    {
        // Un terreno del ámbito sin cosechas sería una barra a cero: ruido, como en kg por destino
        Seed(Row(1000m, plotId: _alto.Id));

        var (_, totals, _) = await CreateSut().GetKgByPlotAsync(WorkspaceId, new DashboardRequest());

        totals.Should().ContainSingle().Which.PlotId.Should().Be(_alto.Id);
    }

    [Fact]
    public async Task Terrenos_Deberia_CuadrarElTotalConElResumen()
    {
        Seed(
            Row(1000m, plotId: _alto.Id),
            Row(700.5m, plotId: _bajo.Id));

        var sut = CreateSut();
        var summary = await sut.GetSummaryAsync(WorkspaceId, new DashboardRequest());
        var (_, totals, totalKg) = await sut.GetKgByPlotAsync(WorkspaceId, new DashboardRequest());

        totals.Sum(t => t.Kg).Should().Be(summary.TotalKg);
        totalKg.Should().Be(summary.TotalKg);
    }

    // ── Evolución de rendimiento (MVP-404, CA-2 · RN-013/RN-015) ────────────

    [Fact]
    public async Task Evolucion_Deberia_AgruparPorMes_YPonderarPorKilos()
    {
        // RN-013 — L/100kg ponderado, por mes. Octubre: 1.000 kg al 18 % y 1.000 al 20 % → 19 L/100kg.
        Seed(
            Row(1000m, yield: 18m, date: new DateOnly(2026, 10, 5)),
            Row(1000m, yield: 20m, date: new DateOnly(2026, 10, 25)),
            Row(2000m, yield: 21m, date: new DateOnly(2026, 11, 10)));

        var evolution = await CreateSut().GetYieldEvolutionAsync(
            WorkspaceId, new DashboardRequest(), YieldGranularity.Month);

        evolution.Series.Select(p => p.Period).Should().ContainInOrder("2026-10", "2026-11");
        evolution.Series.Single(p => p.Period == "2026-10").Yield.Should().Be(19m);
        evolution.Series.Single(p => p.Period == "2026-11").Yield.Should().Be(21m);
    }

    [Fact]
    public async Task Evolucion_NoDeberia_DibujarUnPeriodoSinDatoDeAceite()
    {
        // Un mes con cosechas pero sin rendimiento no tiene punto: un cero fingiría una caída
        Seed(
            Row(1000m, yield: 18m, date: new DateOnly(2026, 10, 5)),
            Row(1000m, date: new DateOnly(2026, 11, 5)));

        var evolution = await CreateSut().GetYieldEvolutionAsync(
            WorkspaceId, new DashboardRequest(), YieldGranularity.Month);

        evolution.Series.Should().ContainSingle().Which.Period.Should().Be("2026-10");
    }

    [Fact]
    public async Task Evolucion_Deberia_AgruparPorSemanaISO()
    {
        Seed(
            Row(1000m, yield: 18m, date: new DateOnly(2026, 10, 5)),
            Row(1000m, yield: 20m, date: new DateOnly(2026, 10, 12)));

        var evolution = await CreateSut().GetYieldEvolutionAsync(
            WorkspaceId, new DashboardRequest(), YieldGranularity.Week);

        // 2026-10-05 es semana ISO 41; 2026-10-12, semana 42.
        evolution.Series.Select(p => p.Period).Should().ContainInOrder("2026-W41", "2026-W42");
    }

    [Fact]
    public async Task Evolucion_NoDeberia_TenerComparativa_SinHistorico()
    {
        // CA-2 — el histórico solo aparece cuando existe suficiente información. Con una sola temporada
        // no hay nada con qué comparar.
        _seasons.ListByWorkspaceAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns([_active]);
        Seed(Row(1000m, yield: 20m, date: new DateOnly(2026, 10, 5)));

        var evolution = await CreateSut().GetYieldEvolutionAsync(
            WorkspaceId, new DashboardRequest(), YieldGranularity.Month);

        evolution.History.Average.Should().BeNull();
        evolution.History.PriorSeasonsWithData.Should().Be(0);
    }

    [Fact]
    public async Task Evolucion_Deberia_CompararConLaMediaHistorica_DeTemporadasPrevias()
    {
        // RN-015 — promedio histórico desde la primera temporada disponible, sobre las anteriores a la
        // que se mira. La comparación es de los mismos terrenos en años distintos.
        var pasada = Season.Create(WorkspaceId, "2025/2026", new DateOnly(2025, 9, 1), new DateOnly(2026, 2, 28));
        pasada.Close();
        _seasons.ListByWorkspaceAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns([_active, pasada]);
        Seed(
            Row(1000m, yield: 22m, seasonId: _active.Id, date: new DateOnly(2026, 10, 5)),
            Row(1000m, yield: 16m, seasonId: pasada.Id, date: new DateOnly(2025, 10, 5)));

        var evolution = await CreateSut().GetYieldEvolutionAsync(
            WorkspaceId, new DashboardRequest(), YieldGranularity.Month);

        // La serie es de la temporada actual; el histórico, de la anterior.
        evolution.Series.Single().Yield.Should().Be(22m);
        evolution.History.Average.Should().Be(16m);
        evolution.History.PriorSeasonsWithData.Should().Be(1);
        // Sin 5 ni 10 temporadas previas, esas medias no aparecen.
        evolution.History.Average5Seasons.Should().BeNull();
        evolution.History.Average10Seasons.Should().BeNull();
    }

    [Fact]
    public async Task Evolucion_Deberia_RespetarElFiltroDeTerreno_TambienEnElHistorico()
    {
        // La comparación es de las **mismas parcelas** en años distintos: el filtro de terreno viaja al
        // histórico, no solo a la serie.
        Seed();

        await CreateSut().GetYieldEvolutionAsync(
            WorkspaceId, new DashboardRequest(PlotIds: [_alto.Id]), YieldGranularity.Month);

        // La consulta de evolución pide todas las temporadas (SeasonId null) pero solo el terreno pedido.
        await _harvests.Received(1).ListAggregateRowsAsync(
            WorkspaceId,
            Arg.Is<HarvestAggregateFilter>(f =>
                f.SeasonId == null && f.PlotIds != null && f.PlotIds.Count == 1 && f.PlotIds.Contains(_alto.Id)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Evolucion_NoDeberia_ConsultarNada_SinTemporada()
    {
        _seasons.FindActiveByWorkspaceAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns((Season?)null);
        Seed();

        var evolution = await CreateSut().GetYieldEvolutionAsync(
            WorkspaceId, new DashboardRequest(), YieldGranularity.Month);

        evolution.Series.Should().BeEmpty();
        evolution.History.Average.Should().BeNull();
        await _harvests.DidNotReceive().ListAggregateRowsAsync(
            Arg.Any<Guid>(), Arg.Any<HarvestAggregateFilter>(), Arg.Any<CancellationToken>());
    }
}
