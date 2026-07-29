using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Purchases;
using Terrenario.Api.Application.Purchases.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Operations;
using Terrenario.Api.Domain.Purchases;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Tests.Purchases;

/// <summary>
/// Tests de los casos de uso de compra (MVP-303): guarda de temporada
/// (<c>FOREIGN_KEY_WORKSPACE_MISMATCH</c>), aislamiento por Workspace, edición parcial y concurrencia
/// optimista. La traducción a SQL se cubre aparte contra SQLite real (P-014).
/// </summary>
public class PurchaseHandlersTests
{
    private readonly IPurchaseRepository _purchases = Substitute.For<IPurchaseRepository>();
    private readonly ISeasonRepository _seasons = Substitute.For<ISeasonRepository>();
    private readonly IConsumptionRepository _consumptions = Substitute.For<IConsumptionRepository>();

    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid SeasonId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 10, 5);

    public PurchaseHandlersTests()
    {
        _seasons.FindByIdAsync(WorkspaceId, SeasonId, Arg.Any<CancellationToken>())
            .Returns(Season.Create(WorkspaceId, "2026/2027", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28)));
    }

    private PurchaseSeasonResolver Resolver() => new(_seasons);

    private CreatePurchaseHandler CreateSut() => new(_purchases, Resolver());

    private UpdatePurchaseHandler UpdateSut() => new(_purchases, Resolver());

    private DeletePurchaseHandler DeleteSut() => new(_purchases, _consumptions);

    private static CreatePurchaseCommand ValidCreate()
        => new(WorkspaceId, UserId, SeasonId, Date, "Abono NPK", 500m, 250m);

    private static Purchase Existing(long version = 1)
    {
        var purchase = Purchase.Create(WorkspaceId, SeasonId, Date, "Abono NPK", 500m, 250m, UserId);
        for (var i = 1; i < version; i++)
            purchase.Update(SeasonId, Date, "Abono NPK", 500m, 250m, UserId);
        return purchase;
    }

    private static PurchaseView ViewOf(Purchase purchase) => new(
        purchase.Id, WorkspaceId, SeasonId, "2026/2027",
        new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28),
        purchase.PurchaseDate, purchase.Product, purchase.TotalQuantity, purchase.TotalCost,
        purchase.UnitPrice, purchase.Version, purchase.CreatedAt, purchase.UpdatedAt);

    [Fact]
    public async Task Create_Deberia_PersistirLaCompra()
    {
        Purchase? added = null;
        await _purchases.AddAsync(Arg.Do<Purchase>(p => added = p), Arg.Any<CancellationToken>());
        _purchases.GetViewAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(added!));

        var result = await CreateSut().HandleAsync(ValidCreate());

        result.Should().NotBeNull();
        added!.WorkspaceId.Should().Be(WorkspaceId);
        added.UnitPrice.Should().Be(0.5m);
        await _purchases.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Deberia_RechazarTemporadaDeOtroWorkspace_SinPersistir()
    {
        // RN-021 / P-050 — la temporada tiene que ser del Workspace activo
        _seasons.FindByIdAsync(WorkspaceId, SeasonId, Arg.Any<CancellationToken>()).Returns((Season?)null);

        var act = () => CreateSut().HandleAsync(ValidCreate());

        (await act.Should().ThrowAsync<PurchaseValidationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ForeignKeyWorkspaceMismatch);
        await _purchases.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Deberia_ValidarElDominio_AntesDeConsultarLaTemporada()
    {
        var act = () => CreateSut().HandleAsync(ValidCreate() with { TotalQuantity = 0m });

        await act.Should().ThrowAsync<PurchaseValidationException>();
        await _seasons.DidNotReceive().FindByIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_Deberia_Devolver404_SiNoEstaEnElWorkspace()
    {
        _purchases.FindByIdAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Purchase?)null);

        var result = await UpdateSut().HandleAsync(UpdateCommand(Guid.NewGuid(), 1));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_Deberia_Rechazar409_SiLaVersionEstaDesfasada()
    {
        var purchase = Existing(version: 3);
        _purchases.FindByIdAsync(WorkspaceId, purchase.Id, Arg.Any<CancellationToken>()).Returns(purchase);

        var act = () => UpdateSut().HandleAsync(UpdateCommand(purchase.Id, expectedVersion: 2));

        (await act.Should().ThrowAsync<ConcurrencyConflictException>())
            .Which.CurrentVersion.Should().Be(3);
        await _purchases.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_Deberia_ConservarLosCamposAusentes_Y_RecalcularElPrecioUnitario()
    {
        // Regresión de PATCH parcial: cambiar solo el coste no toca el producto ni la cantidad, pero
        // sí el precio unitario, que es derivado.
        var purchase = Existing();
        _purchases.FindByIdAsync(WorkspaceId, purchase.Id, Arg.Any<CancellationToken>()).Returns(purchase);
        _purchases.GetViewAsync(WorkspaceId, purchase.Id, Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(purchase));

        await UpdateSut().HandleAsync(
            UpdateCommand(purchase.Id, 1) with { TotalCost = FieldUpdate<decimal>.Set(1000m) });

        purchase.Product.Should().Be("Abono NPK");
        purchase.TotalQuantity.Should().Be(500m);
        purchase.TotalCost.Should().Be(1000m);
        purchase.UnitPrice.Should().Be(2m);
        purchase.Version.Should().Be(2);
    }

    [Fact]
    public async Task Delete_Deberia_MarcarBajaLogica()
    {
        var purchase = Existing();
        _purchases.FindByIdAsync(WorkspaceId, purchase.Id, Arg.Any<CancellationToken>()).Returns(purchase);

        var deleted = await DeleteSut().HandleAsync(
            new DeletePurchaseCommand(WorkspaceId, UserId, purchase.Id, 1));

        deleted.Should().BeTrue();
        purchase.IsDeleted.Should().BeTrue();
        await _purchases.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Deberia_Rechazar409_SiLaVersionEstaDesfasada()
    {
        var purchase = Existing(version: 2);
        _purchases.FindByIdAsync(WorkspaceId, purchase.Id, Arg.Any<CancellationToken>()).Returns(purchase);

        var act = () => DeleteSut().HandleAsync(
            new DeletePurchaseCommand(WorkspaceId, UserId, purchase.Id, 1));

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
        purchase.IsDeleted.Should().BeFalse();
    }

    private static UpdatePurchaseCommand UpdateCommand(Guid purchaseId, long expectedVersion) => new(
        WorkspaceId,
        UserId,
        purchaseId,
        expectedVersion,
        FieldUpdate<Guid>.Absent,
        FieldUpdate<DateOnly>.Absent,
        FieldUpdate<string>.Absent,
        FieldUpdate<decimal>.Absent,
        FieldUpdate<decimal>.Absent);
}
