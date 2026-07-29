using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Diary;
using Terrenario.Api.Domain.Activities;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Purchases;

namespace Terrenario.Api.Tests.Diary;

/// <summary>
/// Tests del diario unificado (MVP-305): que mezcla los tres tipos, que ordena por **fecha de
/// negocio** y no por la de captura (RN-033), y que filtrar por tipo o por terreno consulta solo lo
/// que corresponde en vez de traerlo todo y esconderlo después.
/// </summary>
public class DiaryQueryServiceTests
{
    private readonly IActivityRepository _activities = Substitute.For<IActivityRepository>();
    private readonly IPurchaseRepository _purchases = Substitute.For<IPurchaseRepository>();
    private readonly IConsumptionRepository _consumptions = Substitute.For<IConsumptionRepository>();

    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid PlotId = Guid.NewGuid();
    private static readonly Guid SeasonId = Guid.NewGuid();
    private static readonly DateOnly SeasonStart = new(2026, 9, 1);
    private static readonly DateOnly SeasonEnd = new(2027, 2, 28);

    private DiaryQueryService CreateSut() => new(_activities, _purchases, _consumptions);

    private static ActivityView Activity(DateOnly date, DateTimeOffset createdAt, string task = "Poda")
        => new(Guid.NewGuid(), WorkspaceId, PlotId, "Olivar Alto", SeasonId, "2026/2027",
            SeasonStart, SeasonEnd, Guid.NewGuid(), "Antonio", date, 4m, null, null, task, 70m,
            "Sector norte", 1, createdAt, createdAt);

    private static PurchaseView Purchase(DateOnly date, DateTimeOffset createdAt)
        => new(Guid.NewGuid(), WorkspaceId, SeasonId, "2026/2027", SeasonStart, SeasonEnd,
            date, "Abono NPK", 500m, 250m, 0.5m, 1, createdAt, createdAt);

    private static ConsumptionView Consumption(
        DateOnly date, DateTimeOffset createdAt, Guid? purchaseId = null)
        => new(Guid.NewGuid(), WorkspaceId, purchaseId, PlotId, "Olivar Alto", SeasonId, "2026/2027",
            SeasonStart, SeasonEnd, date, "Abono NPK", 20m, purchaseId is null ? 0m : 0.5m,
            purchaseId is null ? 0m : 10m, 1, createdAt, createdAt);

    private void Seed(
        IReadOnlyList<ActivityView>? activities = null,
        IReadOnlyList<PurchaseView>? purchases = null,
        IReadOnlyList<ConsumptionView>? consumptions = null)
    {
        _activities.ListAsync(WorkspaceId, Arg.Any<ActivityFilter>(), Arg.Any<CancellationToken>())
            .Returns(activities ?? []);
        _purchases.ListAsync(WorkspaceId, Arg.Any<PurchaseFilter>(), Arg.Any<CancellationToken>())
            .Returns(purchases ?? []);
        _consumptions.ListAsync(WorkspaceId, Arg.Any<ConsumptionFilter>(), Arg.Any<CancellationToken>())
            .Returns(consumptions ?? []);
    }

    [Fact]
    public async Task Deberia_MezclarLosTresTipos_Y_OrdenarPorFechaDeNegocio()
    {
        // CA-1/CA-2 y RN-033: una sola secuencia cronológica. La compra es la más antigua aunque se
        // haya capturado la última, así que queda abajo.
        var captura = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
        Seed(
            activities: [Activity(new DateOnly(2026, 10, 15), captura)],
            purchases: [Purchase(new DateOnly(2026, 10, 1), captura.AddDays(5))],
            consumptions: [Consumption(new DateOnly(2026, 10, 20), captura)]);

        var result = await CreateSut().HandleAsync(WorkspaceId, new DiaryFilter());

        result.Entries.Select(e => e.Type).Should().ContainInOrder(
            DiaryEntryTypes.Consumption, DiaryEntryTypes.Activity, DiaryEntryTypes.Purchase);
        result.Entries.Select(e => e.Date).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Deberia_DesempatarPorFechaDeCaptura_Descendente()
    {
        // A igualdad de fecha de negocio, lo apuntado más tarde arriba: es como se recuerda
        var dia = new DateOnly(2026, 10, 15);
        var primera = Activity(dia, new DateTimeOffset(2026, 10, 15, 8, 0, 0, TimeSpan.Zero), "Poda");
        var segunda = Activity(dia, new DateTimeOffset(2026, 10, 15, 18, 0, 0, TimeSpan.Zero), "Riego");
        Seed(activities: [primera, segunda]);

        var result = await CreateSut().HandleAsync(WorkspaceId, new DiaryFilter());

        result.Entries.Select(e => e.Title).Should().ContainInOrder("Riego", "Poda");
    }

    [Fact]
    public async Task Deberia_ResumirElDiario_ParaLaCabecera()
    {
        var captura = DateTimeOffset.UtcNow;
        Seed(
            activities: [Activity(new DateOnly(2026, 10, 15), captura)],
            purchases: [Purchase(new DateOnly(2026, 10, 1), captura)],
            consumptions:
            [
                Consumption(new DateOnly(2026, 10, 20), captura, purchaseId: Guid.NewGuid()),
                Consumption(new DateOnly(2026, 10, 21), captura)
            ]);

        var result = await CreateSut().HandleAsync(WorkspaceId, new DiaryFilter());

        result.TotalActivities.Should().Be(1);
        result.TotalPurchases.Should().Be(1);
        result.TotalConsumptions.Should().Be(2);
        // R-01 (MVP-399) — el gasto **no** suma la imputación: sus 10 € ya están dentro de los 250 €
        // de la compra, así que contarlos sería contar el mismo dinero dos veces.
        result.TotalCost.Should().Be(320m); // 70 actividad + 250 compra + 0 consumo sin compra
        result.ImputedCost.Should().Be(10m);
        // RN-032 — el impacto en la calidad del dato queda visible
        result.ConsumptionsWithoutPurchase.Should().Be(1);
    }

    [Fact]
    public async Task NoDeberia_ContarDosVeces_ElDineroDeUnaCompraYaRepartida()
    {
        // R-01 (MVP-399) — el hallazgo tal cual: una compra de 250 € repartida entera entre dos
        // terrenos son 250 € de gasto, no 500. La imputación no es gasto nuevo.
        var captura = DateTimeOffset.UtcNow;
        var purchaseId = Guid.NewGuid();
        Seed(
            purchases: [Purchase(new DateOnly(2026, 10, 1), captura)],
            consumptions:
            [
                Consumption(new DateOnly(2026, 10, 5), captura, purchaseId),
                Consumption(new DateOnly(2026, 10, 6), captura, purchaseId)
            ]);

        var result = await CreateSut().HandleAsync(WorkspaceId, new DiaryFilter());

        result.TotalCost.Should().Be(250m);
        result.ImputedCost.Should().Be(20m);
        // Las tarjetas siguen mostrando su coste proporcional: lo que cambia es el resumen.
        result.Entries.Where(e => e.Type == DiaryEntryTypes.Consumption)
            .Should().OnlyContain(e => e.Cost == 10m);
    }

    [Fact]
    public async Task Deberia_ConsultarSolo_LosTiposPedidos()
    {
        // Filtrar por tipo tiene que ahorrar trabajo, no solo ocultar el resultado
        Seed();

        await CreateSut().HandleAsync(
            WorkspaceId, new DiaryFilter(Types: [DiaryEntryTypes.Activity]));

        await _activities.Received(1).ListAsync(
            WorkspaceId, Arg.Any<ActivityFilter>(), Arg.Any<CancellationToken>());
        await _purchases.DidNotReceive().ListAsync(
            Arg.Any<Guid>(), Arg.Any<PurchaseFilter>(), Arg.Any<CancellationToken>());
        await _consumptions.DidNotReceive().ListAsync(
            Arg.Any<Guid>(), Arg.Any<ConsumptionFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_DejarFueraLasCompras_AlFiltrarPorTerreno()
    {
        // Una compra es del Workspace, no de un terreno: el reparto por terrenos es el consumo
        // (MVP-304). Filtrar el diario por terreno la excluye por definición.
        Seed();

        await CreateSut().HandleAsync(WorkspaceId, new DiaryFilter(PlotId: PlotId));

        await _purchases.DidNotReceive().ListAsync(
            Arg.Any<Guid>(), Arg.Any<PurchaseFilter>(), Arg.Any<CancellationToken>());
        await _consumptions.Received(1).ListAsync(
            WorkspaceId, Arg.Any<ConsumptionFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_PropagarLosFiltros_ALosTresPuertos()
    {
        Seed();
        var from = new DateOnly(2026, 10, 1);
        var to = new DateOnly(2026, 10, 31);

        await CreateSut().HandleAsync(WorkspaceId, new DiaryFilter(from, to, null, SeasonId));

        await _activities.Received(1).ListAsync(
            WorkspaceId,
            Arg.Is<ActivityFilter>(f => f.From == from && f.To == to && f.SeasonId == SeasonId),
            Arg.Any<CancellationToken>());
        await _purchases.Received(1).ListAsync(
            WorkspaceId,
            Arg.Is<PurchaseFilter>(f => f.From == from && f.To == to && f.SeasonId == SeasonId),
            Arg.Any<CancellationToken>());
        await _consumptions.Received(1).ListAsync(
            WorkspaceId,
            Arg.Is<ConsumptionFilter>(f => f.From == from && f.To == to && f.SeasonId == SeasonId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_ProyectarCadaTipo_ConLoQueLeAplica()
    {
        var captura = DateTimeOffset.UtcNow;
        Seed(
            activities: [Activity(new DateOnly(2026, 10, 15), captura)],
            purchases: [Purchase(new DateOnly(2026, 10, 14), captura)],
            consumptions: [Consumption(new DateOnly(2026, 10, 13), captura)]);

        var result = await CreateSut().HandleAsync(WorkspaceId, new DiaryFilter());

        var actividad = result.Entries.Single(e => e.Type == DiaryEntryTypes.Activity);
        actividad.WorkerName.Should().Be("Antonio");
        actividad.Hours.Should().Be(4m);
        actividad.PlotName.Should().Be("Olivar Alto");
        actividad.Quantity.Should().BeNull();

        var compra = result.Entries.Single(e => e.Type == DiaryEntryTypes.Purchase);
        compra.Quantity.Should().Be(500m);
        // La compra no cuelga de un terreno: el muro no debe inventarle uno
        compra.PlotId.Should().BeNull();
        compra.PlotName.Should().BeNull();
        compra.WorkerName.Should().BeNull();

        var consumo = result.Entries.Single(e => e.Type == DiaryEntryTypes.Consumption);
        consumo.HasPurchase.Should().BeFalse();
        consumo.Cost.Should().Be(0m);
        consumo.PlotName.Should().Be("Olivar Alto");
    }

    [Fact]
    public void DiaryEntryTypes_NoDeberia_AdmitirTodavia_LaCosecha()
    {
        // G-4 — HARVEST no existe hasta MVP-004; el valor está reservado pero no se emite
        DiaryEntryTypes.IsSupported(DiaryEntryTypes.Harvest).Should().BeFalse();
        DiaryEntryTypes.Supported.Should().BeEquivalentTo(["actividad", "compra", "consumo"]);
    }
}
