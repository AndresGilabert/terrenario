using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Harvests;

namespace Terrenario.Api.Tests.Harvests;

/// <summary>
/// MVP-707 — El único campo económico del MVP: precio por kilo opcional e importe <b>derivado</b>.
///
/// Lo que se fija aquí es la diferencia entre <b>cero</b> y <b>no se sabe</b>. Es la decisión que
/// sostiene el CA-2 y el CA-5: una partida sin precio no ha ingresado 0 €, y afirmar el cero sería
/// afirmar algo falso sobre la campaña.
/// </summary>
public sealed class HarvestEconomicsTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid PlotId = Guid.NewGuid();
    private static readonly Guid SeasonId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 12, 5);

    private static Harvest Create(decimal kgs = 1_000m, decimal? unitPrice = null)
        => Harvest.Create(
            WorkspaceId, PlotId, SeasonId, Date, HarvestProducts.AceitunaOlivar, kgs,
            HarvestDestinations.VentaAceituna, yield: null, liters: null, unitPrice: unitPrice, UserId);

    [Fact]
    public void Deberia_GuardarseSinPrecio_Y_NoTenerImporte()
    {
        // CA-2 — el precio es opcional y su ausencia **no** produce un importe de cero.
        var harvest = Create();

        harvest.UnitPrice.Should().BeNull();
        harvest.Amount.Should().BeNull();
    }

    [Fact]
    public void Deberia_DerivarElImporte_DeKilosPorPrecio()
    {
        // CA-1 — el importe se calcula, no se teclea.
        var harvest = Create(kgs: 1_250m, unitPrice: 0.62m);

        harvest.Amount.Should().Be(775m);
    }

    [Fact]
    public void Deberia_RecalcularElImporte_Cuando_CambianLosKilos()
    {
        // CA-3 — el importe no se persiste como dato independiente que pueda divergir de sus factores.
        var harvest = Create(kgs: 1_000m, unitPrice: 0.50m);
        harvest.Amount.Should().Be(500m);

        harvest.Update(
            PlotId, SeasonId, Date, HarvestProducts.AceitunaOlivar, 2_000m,
            HarvestDestinations.VentaAceituna, yield: null, liters: null, unitPrice: 0.50m, UserId);

        harvest.Amount.Should().Be(1_000m);
    }

    [Fact]
    public void Deberia_PoderRetirarseElPrecio()
    {
        // Si la venta se cae, la partida vuelve a «no se sabe», no a «cero».
        var harvest = Create(unitPrice: 0.55m);

        harvest.Update(
            PlotId, SeasonId, Date, HarvestProducts.AceitunaOlivar, 1_000m,
            HarvestDestinations.VentaAceituna, yield: null, liters: null, unitPrice: null, UserId);

        harvest.UnitPrice.Should().BeNull();
        harvest.Amount.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void NoDeberia_AdmitirUnPrecioQueNoEsUnPrecio(decimal unitPrice)
    {
        // Un cero explícito significaría «he ingresado nada por esta partida», que casi siempre es un
        // tecleo a medias. Quien no lo sepa deja el campo vacío.
        var act = () => Create(unitPrice: unitPrice);

        act.Should().Throw<HarvestValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationHarvestUnitPriceRange);
    }

    [Fact]
    public void Deberia_AdmitirPrecio_TambienEnDestinosQueNoSonDeVenta()
    {
        // El destino de venta es dónde la UI **ofrece** el campo, no dónde el dominio lo permite:
        // quien vende parte de una partida destinada a consumo propio también quiere apuntarlo.
        var harvest = Harvest.Create(
            WorkspaceId, PlotId, SeasonId, Date, HarvestProducts.AceitunaOlivar, 500m,
            HarvestDestinations.AceitePersonal, yield: null, liters: null, unitPrice: 0.40m, UserId);

        harvest.Amount.Should().Be(200m);
    }

    [Fact]
    public void Deberia_SaberQueDestinosSonDeVenta()
    {
        HarvestDestinations.IsSale(HarvestDestinations.VentaAceituna).Should().BeTrue();
        HarvestDestinations.IsSale(HarvestDestinations.AceiteParaVenta).Should().BeTrue();
        HarvestDestinations.IsSale(HarvestDestinations.AceitePersonal).Should().BeFalse();
        HarvestDestinations.IsSale(HarvestDestinations.Desconocido).Should().BeFalse();
    }
}
