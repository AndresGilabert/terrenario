using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Operations;
using Terrenario.Api.Domain.Purchases;

namespace Terrenario.Api.Tests.Purchases;

/// <summary>
/// Tests de dominio de la compra (MVP-303): producto libre (RN-031), temporada obligatoria (RN-021,
/// <c>P-050</c>), rangos de cantidad y coste, precio unitario derivado y el patrón operativo de
/// concurrencia y baja lógica que hereda de MVP-301.
/// </summary>
public class PurchaseTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid SeasonId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 10, 5);

    private static Purchase CreateValid(
        string product = "Abono NPK",
        decimal quantity = 500m,
        decimal cost = 250m,
        Guid? seasonId = null)
        => Purchase.Create(WorkspaceId, seasonId ?? SeasonId, Date, product, quantity, cost, UserId);

    [Fact]
    public void Create_Deberia_RegistrarLaCompra_Y_DerivarElPrecioUnitario()
    {
        // CA-1 — producto libre, cantidad, coste y temporada
        var purchase = CreateValid(product: "  Abono NPK  ");

        purchase.Product.Should().Be("Abono NPK");
        purchase.SeasonId.Should().Be(SeasonId);
        purchase.TotalQuantity.Should().Be(500m);
        purchase.TotalCost.Should().Be(250m);
        purchase.UnitPrice.Should().Be(0.5m);
        purchase.Version.Should().Be(1);
        purchase.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_Deberia_RedondearElPrecioUnitarioA4Decimales()
    {
        // decimal(10,4): lo que se lee es lo que se guarda, y es la base del coste proporcional de
        // las imputaciones de MVP-304.
        var purchase = CreateValid(quantity: 3m, cost: 10m);

        purchase.UnitPrice.Should().Be(3.3333m);
    }

    [Fact]
    public void Create_Deberia_RechazarProductoVacio()
    {
        // RN-031 — el material es texto libre, pero obligatorio
        var act = () => CreateValid(product: "   ");

        act.Should().Throw<PurchaseValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationPurchaseRequiredProduct);
    }

    [Fact]
    public void Create_Deberia_RechazarProductoDemasiadoLargo()
    {
        var act = () => CreateValid(product: new string('a', Purchase.ProductMaxLength + 1));

        act.Should().Throw<PurchaseValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationPurchaseProductLength);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(-1, 100)]
    [InlineData(100, 0)]
    [InlineData(100, -1)]
    public void Create_Deberia_RechazarTotalesNoPositivos(decimal quantity, decimal cost)
    {
        // Una compra de 0 unidades o de 0 € no es una compra; además `quantity = 0` haría indefinido
        // el precio unitario.
        var act = () => CreateValid(quantity: quantity, cost: cost);

        act.Should().Throw<PurchaseValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationPurchaseTotalsRange);
    }

    [Fact]
    public void Create_Deberia_RechazarCompraSinTemporada()
    {
        // RN-021 / P-050 — toda compra queda asociada a una temporada
        var act = () => CreateValid(seasonId: Guid.Empty);

        act.Should().Throw<PurchaseValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationPurchaseRequiredFields);
    }

    [Fact]
    public void Update_Deberia_RecalcularElPrecioUnitario_Y_SubirLaVersion()
    {
        var purchase = CreateValid();

        purchase.Update(SeasonId, Date, "Abono NPK", 200m, 300m, UserId);

        purchase.UnitPrice.Should().Be(1.5m);
        purchase.Version.Should().Be(2);
    }

    [Fact]
    public void EnsureVersion_Deberia_RechazarLaVersionDesfasada()
    {
        // ADR-0005, mismo patrón que la actividad
        var purchase = CreateValid();
        purchase.Update(SeasonId, Date, "Abono NPK", 200m, 300m, UserId);

        purchase.Invoking(p => p.EnsureVersion(2)).Should().NotThrow();
        purchase.Invoking(p => p.EnsureVersion(1))
            .Should().Throw<ConcurrencyConflictException>()
            .Which.CurrentVersion.Should().Be(2);
    }

    [Fact]
    public void Delete_Deberia_SerLogico_Y_Idempotente()
    {
        // RN-037 — la fila permanece; solo se marca
        var purchase = CreateValid();

        purchase.Delete(UserId);
        purchase.IsDeleted.Should().BeTrue();
        purchase.Version.Should().Be(2);

        purchase.Delete(UserId);
        purchase.Version.Should().Be(2);
    }
}
