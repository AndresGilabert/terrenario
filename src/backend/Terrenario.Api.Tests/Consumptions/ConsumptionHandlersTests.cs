using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Consumptions;
using Terrenario.Api.Application.Consumptions.Commands;
using Terrenario.Api.Application.Purchases;
using Terrenario.Api.Application.Purchases.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Operations;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Purchases;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Tests.Consumptions;

/// <summary>
/// Tests de los casos de uso de consumo (MVP-304): imputación con herencia de la compra, guarda de
/// sobre-imputación (CA-1), consumo sin compra previa (CA-2) y la guarda que impide dar de baja una
/// compra con imputaciones vivas.
/// </summary>
public class ConsumptionHandlersTests
{
    private readonly IConsumptionRepository _consumptions = Substitute.For<IConsumptionRepository>();
    private readonly IPurchaseRepository _purchases = Substitute.For<IPurchaseRepository>();
    private readonly IPlotRepository _plots = Substitute.For<IPlotRepository>();
    private readonly ISeasonRepository _seasons = Substitute.For<ISeasonRepository>();

    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PlotId = Guid.NewGuid();
    private static readonly Guid SeasonId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 10, 12);

    private readonly Purchase _purchase;

    public ConsumptionHandlersTests()
    {
        _plots.FindByIdAsync(WorkspaceId, PlotId, Arg.Any<CancellationToken>())
            .Returns(Plot.Create(WorkspaceId, "Olivar Alto", "propia"));
        _seasons.FindByIdAsync(WorkspaceId, SeasonId, Arg.Any<CancellationToken>())
            .Returns(Season.Create(WorkspaceId, "2026/2027", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28)));

        // Compra de 500 unidades a 0,50 €/ud.
        _purchase = Purchase.Create(
            WorkspaceId, SeasonId, new DateOnly(2026, 10, 1), "Abono NPK", 500m, 250m, UserId);
        _purchases.FindByIdAsync(WorkspaceId, _purchase.Id, Arg.Any<CancellationToken>()).Returns(_purchase);
    }

    private ConsumptionLinkResolver Resolver() => new(_plots, _seasons);

    private PurchaseImputationGuard Guard() => new(_consumptions);

    private ImputePurchaseHandler ImputeSut() => new(_consumptions, _purchases, Resolver(), Guard());

    private RegisterConsumptionHandler RegisterSut() => new(_consumptions, Resolver());

    private UpdateConsumptionHandler UpdateSut() => new(_consumptions, _purchases, Resolver(), Guard());

    private static ConsumptionView ViewOf(PurchaseConsumption consumption) => new(
        consumption.Id, WorkspaceId, consumption.PurchaseId, PlotId, "Olivar Alto",
        SeasonId, "2026/2027", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28),
        consumption.Date, consumption.Product, consumption.ConsumedQuantity, consumption.UnitPrice,
        consumption.ProportionalCost, consumption.Version, consumption.CreatedAt, consumption.UpdatedAt);

    private void CaptureAdded(Action<PurchaseConsumption> capture)
        => _consumptions.AddAsync(Arg.Do<PurchaseConsumption>(capture), Arg.Any<CancellationToken>());

    // ── Imputación (HU-1, CA-1) ─────────────────────────────────────────────

    [Fact]
    public async Task Impute_Deberia_HeredarDeLaCompra_Y_CalcularElCosteProporcional()
    {
        PurchaseConsumption? added = null;
        CaptureAdded(c => added = c);
        _consumptions.GetViewAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(added!));

        var result = await ImputeSut().HandleAsync(
            new ImputePurchaseCommand(WorkspaceId, UserId, _purchase.Id, PlotId, Date, 120m));

        result.Should().NotBeNull();
        added!.Product.Should().Be("Abono NPK");
        added.SeasonId.Should().Be(SeasonId);
        added.UnitPrice.Should().Be(0.5m);
        added.ProportionalCost.Should().Be(60m);
        await _consumptions.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Impute_Deberia_Devolver404_SiLaCompraNoEstaEnElWorkspace()
    {
        _purchases.FindByIdAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Purchase?)null);

        var result = await ImputeSut().HandleAsync(
            new ImputePurchaseCommand(WorkspaceId, UserId, Guid.NewGuid(), PlotId, Date, 10m));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Impute_Deberia_RechazarSobreImputacion_SinPersistir()
    {
        // CA-1 — no se puede repartir más material del que se compró
        _consumptions.SumImputedQuantityAsync(
                WorkspaceId, _purchase.Id, null, Arg.Any<CancellationToken>())
            .Returns(450m);

        var act = () => ImputeSut().HandleAsync(
            new ImputePurchaseCommand(WorkspaceId, UserId, _purchase.Id, PlotId, Date, 100m));

        var ex = (await act.Should().ThrowAsync<ConsumptionValidationException>()).Which;
        ex.ErrorCode.Should().Be(ErrorCodes.ValidationConsumptionOverflow);
        // El mensaje dice cuánto queda: un error que no explica el margen no es accionable.
        ex.Message.Should().Contain("50");
        await _consumptions.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Impute_Deberia_AdmitirRepartoExacto()
    {
        // El límite es «no más de lo comprado», no «menos de lo comprado»
        _consumptions.SumImputedQuantityAsync(
                WorkspaceId, _purchase.Id, null, Arg.Any<CancellationToken>())
            .Returns(400m);
        PurchaseConsumption? added = null;
        CaptureAdded(c => added = c);
        _consumptions.GetViewAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(added!));

        var result = await ImputeSut().HandleAsync(
            new ImputePurchaseCommand(WorkspaceId, UserId, _purchase.Id, PlotId, Date, 100m));

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Impute_Deberia_RechazarTerrenoDeOtroWorkspace()
    {
        _plots.FindByIdAsync(WorkspaceId, PlotId, Arg.Any<CancellationToken>()).Returns((Plot?)null);

        var act = () => ImputeSut().HandleAsync(
            new ImputePurchaseCommand(WorkspaceId, UserId, _purchase.Id, PlotId, Date, 10m));

        (await act.Should().ThrowAsync<ConsumptionValidationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ForeignKeyWorkspaceMismatch);
    }

    // ── Consumo sin compra previa (HU-2, CA-2) ──────────────────────────────

    [Fact]
    public async Task Register_Deberia_GuardarConCoste0_SinConsultarNingunaCompra()
    {
        // RN-032 — la ausencia de compra nunca bloquea el registro
        PurchaseConsumption? added = null;
        CaptureAdded(c => added = c);
        _consumptions.GetViewAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(added!));

        var result = await RegisterSut().HandleAsync(
            new RegisterConsumptionCommand(
                WorkspaceId, UserId, SeasonId, PlotId, Date, "Abono de la nave", 20m));

        result.HasPurchase.Should().BeFalse();
        result.ProportionalCost.Should().Be(0m);
        added!.PurchaseId.Should().BeNull();
        await _purchases.DidNotReceive().FindByIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_Deberia_RechazarTemporadaDeOtroWorkspace()
    {
        _seasons.FindByIdAsync(WorkspaceId, SeasonId, Arg.Any<CancellationToken>()).Returns((Season?)null);

        var act = () => RegisterSut().HandleAsync(
            new RegisterConsumptionCommand(WorkspaceId, UserId, SeasonId, PlotId, Date, "Abono", 20m));

        (await act.Should().ThrowAsync<ConsumptionValidationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ForeignKeyWorkspaceMismatch);
    }

    // ── Edición ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Deberia_ExcluirLaPropiaImputacion_DeLaGuardaDeSobreImputacion()
    {
        // Subir de 100 a 150 sobre una compra de 500 con 100 ya imputados (los suyos) debe caber:
        // si la fila contase dos veces, corregir al alza sería imposible.
        var consumption = PurchaseConsumption.ImputeFromPurchase(
            WorkspaceId, _purchase.Id, SeasonId, "Abono NPK", 0.5m, PlotId, Date, 100m, UserId);
        _consumptions.FindByIdAsync(WorkspaceId, consumption.Id, Arg.Any<CancellationToken>())
            .Returns(consumption);
        _consumptions.SumImputedQuantityAsync(
                WorkspaceId, _purchase.Id, consumption.Id, Arg.Any<CancellationToken>())
            .Returns(0m);
        _consumptions.GetViewAsync(WorkspaceId, consumption.Id, Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(consumption));

        await UpdateSut().HandleAsync(
            UpdateCommand(consumption.Id, 1) with { Quantity = FieldUpdate<decimal>.Set(150m) });

        consumption.ConsumedQuantity.Should().Be(150m);
        consumption.ProportionalCost.Should().Be(75m);
    }

    [Fact]
    public async Task Update_Deberia_Rechazar409_SiLaVersionEstaDesfasada()
    {
        var consumption = PurchaseConsumption.ImputeFromPurchase(
            WorkspaceId, _purchase.Id, SeasonId, "Abono NPK", 0.5m, PlotId, Date, 100m, UserId);
        consumption.Update(SeasonId, PlotId, Date, "Abono NPK", 100m, UserId);
        _consumptions.FindByIdAsync(WorkspaceId, consumption.Id, Arg.Any<CancellationToken>())
            .Returns(consumption);

        var act = () => UpdateSut().HandleAsync(UpdateCommand(consumption.Id, 1));

        (await act.Should().ThrowAsync<ConcurrencyConflictException>())
            .Which.CurrentVersion.Should().Be(2);
    }

    // ── Guarda al dar de baja la compra ─────────────────────────────────────

    [Fact]
    public async Task DeletePurchase_Deberia_Rechazar422_SiTieneImputacionesVivas()
    {
        // Llevárselas en cascada borraría registros operativos que están en el diario
        _consumptions.CountLiveByPurchaseAsync(WorkspaceId, _purchase.Id, Arg.Any<CancellationToken>())
            .Returns(2);

        var act = () => new DeletePurchaseHandler(_purchases, _consumptions).HandleAsync(
            new DeletePurchaseCommand(WorkspaceId, UserId, _purchase.Id, 1));

        var ex = (await act.Should().ThrowAsync<PurchaseBusinessRuleException>()).Which;
        ex.ErrorCode.Should().Be(ErrorCodes.BusinessRulePurchaseHasConsumptions);
        ex.Message.Should().Contain("2");
        _purchase.IsDeleted.Should().BeFalse();
        await _purchases.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletePurchase_Deberia_Permitirlo_SiNoTieneImputacionesVivas()
    {
        _consumptions.CountLiveByPurchaseAsync(WorkspaceId, _purchase.Id, Arg.Any<CancellationToken>())
            .Returns(0);

        var deleted = await new DeletePurchaseHandler(_purchases, _consumptions).HandleAsync(
            new DeletePurchaseCommand(WorkspaceId, UserId, _purchase.Id, 1));

        deleted.Should().BeTrue();
        _purchase.IsDeleted.Should().BeTrue();
    }

    private static UpdateConsumptionCommand UpdateCommand(Guid consumptionId, long expectedVersion) => new(
        WorkspaceId,
        UserId,
        consumptionId,
        expectedVersion,
        FieldUpdate<Guid>.Absent,
        FieldUpdate<Guid>.Absent,
        FieldUpdate<DateOnly>.Absent,
        FieldUpdate<string>.Absent,
        FieldUpdate<decimal>.Absent);
}
