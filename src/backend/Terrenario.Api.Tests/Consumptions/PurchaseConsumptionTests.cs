using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Tests.Consumptions;

/// <summary>
/// Tests de dominio del consumo (MVP-304). Lo importante aquí es que **una sola entidad** cubre los
/// dos casos —imputación de una compra y consumo sin compra previa (RN-032)— y que el precio unitario
/// queda **congelado**, que es lo que hace verdadero el CA-3 por estructura.
/// </summary>
public class PurchaseConsumptionTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid PurchaseId = Guid.NewGuid();
    private static readonly Guid PlotId = Guid.NewGuid();
    private static readonly Guid SeasonId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 10, 12);

    private static PurchaseConsumption Imputed(decimal quantity = 100m, decimal unitPrice = 0.5m)
        => PurchaseConsumption.ImputeFromPurchase(
            WorkspaceId, PurchaseId, SeasonId, "Abono NPK", unitPrice, PlotId, Date, quantity, UserId);

    private static PurchaseConsumption WithoutPurchase(string product = "Abono de la nave", decimal quantity = 20m)
        => PurchaseConsumption.RegisterWithoutPurchase(
            WorkspaceId, SeasonId, PlotId, Date, product, quantity, UserId);

    [Fact]
    public void ImputeFromPurchase_Deberia_HeredarDeLaCompra_Y_CalcularElCosteProporcional()
    {
        // CA-1 — cantidad aproximada y coste proporcional
        var consumption = Imputed(quantity: 120m, unitPrice: 0.75m);

        consumption.PurchaseId.Should().Be(PurchaseId);
        consumption.HasPurchase.Should().BeTrue();
        consumption.Product.Should().Be("Abono NPK");
        consumption.SeasonId.Should().Be(SeasonId);
        consumption.UnitPrice.Should().Be(0.75m);
        consumption.ConsumedQuantity.Should().Be(120m);
        consumption.ProportionalCost.Should().Be(90m);
        consumption.Version.Should().Be(1);
    }

    [Fact]
    public void RegisterWithoutPurchase_Deberia_DejarCoste0_Y_SenalarQueNoHayCompra()
    {
        // CA-2 / RN-032 — la ausencia de compra nunca bloquea; el coste es 0 y se avisa
        var consumption = WithoutPurchase();

        consumption.PurchaseId.Should().BeNull();
        consumption.HasPurchase.Should().BeFalse();
        consumption.UnitPrice.Should().Be(0m);
        consumption.ProportionalCost.Should().Be(0m);
        consumption.Product.Should().Be("Abono de la nave");
        // G-3 — fecha de negocio propia y temporada, que es lo que lo hace ordenable en el diario
        consumption.Date.Should().Be(Date);
        consumption.SeasonId.Should().Be(SeasonId);
    }

    [Fact]
    public void Update_Deberia_RecalcularElCoste_ConElPrecioCongelado()
    {
        // CA-3 — el precio unitario es el de cuando se imputó, no el que tenga hoy la compra
        var consumption = Imputed(quantity: 100m, unitPrice: 0.5m);

        consumption.Update(SeasonId, PlotId, Date, "Abono NPK", 200m, UserId);

        consumption.UnitPrice.Should().Be(0.5m);
        consumption.ProportionalCost.Should().Be(100m);
        consumption.Version.Should().Be(2);
    }

    [Fact]
    public void Update_Deberia_MantenerElCosteACero_EnUnConsumoSinCompra()
    {
        // Aunque después exista una compra del mismo producto, este registro no gana coste (CA-3)
        var consumption = WithoutPurchase();

        consumption.Update(SeasonId, PlotId, Date, "Abono de la nave", 50m, UserId);

        consumption.ProportionalCost.Should().Be(0m);
        consumption.HasPurchase.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Deberia_RechazarCantidadNoPositiva(decimal quantity)
    {
        var act = () => WithoutPurchase(quantity: quantity);

        act.Should().Throw<ConsumptionValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationConsumptionQuantityRange);
    }

    [Fact]
    public void Deberia_RechazarConsumoSinProducto()
    {
        // Sin compra hay que informarlo: no hay compra de la que heredarlo (RN-031)
        var act = () => WithoutPurchase(product: "   ");

        act.Should().Throw<ConsumptionValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationConsumptionRequiredProduct);
    }

    [Fact]
    public void Deberia_RechazarConsumoSinTerreno()
    {
        // RN-001 — todo registro operativo va asociado a un terreno
        var act = () => PurchaseConsumption.RegisterWithoutPurchase(
            WorkspaceId, SeasonId, Guid.Empty, Date, "Abono", 10m, UserId);

        act.Should().Throw<ConsumptionValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationConsumptionRequiredFields);
    }

    [Fact]
    public void EnsureVersion_Deberia_RechazarLaVersionDesfasada()
    {
        var consumption = Imputed();
        consumption.Update(SeasonId, PlotId, Date, "Abono NPK", 50m, UserId);

        consumption.Invoking(c => c.EnsureVersion(2)).Should().NotThrow();
        consumption.Invoking(c => c.EnsureVersion(1))
            .Should().Throw<ConcurrencyConflictException>()
            .Which.CurrentVersion.Should().Be(2);
    }

    [Fact]
    public void Delete_Deberia_SerLogico_Y_Idempotente()
    {
        var consumption = Imputed();

        consumption.Delete(UserId);
        consumption.IsDeleted.Should().BeTrue();
        consumption.Version.Should().Be(2);

        consumption.Delete(UserId);
        consumption.Version.Should().Be(2);
    }
}
