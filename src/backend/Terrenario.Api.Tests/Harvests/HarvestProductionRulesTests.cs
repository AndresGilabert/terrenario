using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Harvests;
using Terrenario.Api.Application.Harvests.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Harvests;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Tests.Harvests;

/// <summary>
/// Tests de las reglas de producción que cierra MVP-402: catálogo global fijo de producto (RN-030),
/// taxonomía cerrada de destino con `desconocido` (RN-012), exclusión rendimiento/litros (RN-004) y
/// unidad canónica L/100kg con sus entradas equivalentes (RN-013/RN-014/RN-016).
/// </summary>
public class HarvestProductionRulesTests
{
    private readonly IHarvestRepository _harvests = Substitute.For<IHarvestRepository>();
    private readonly IPlotRepository _plots = Substitute.For<IPlotRepository>();
    private readonly ISeasonRepository _seasons = Substitute.For<ISeasonRepository>();

    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PlotId = Guid.NewGuid();
    private static readonly Guid SeasonId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 10, 20);

    public HarvestProductionRulesTests()
    {
        _plots.FindByIdAsync(WorkspaceId, PlotId, Arg.Any<CancellationToken>())
            .Returns(Plot.Create(WorkspaceId, "Olivar Alto", "propia"));
        _seasons.FindByIdAsync(WorkspaceId, SeasonId, Arg.Any<CancellationToken>())
            .Returns(Season.Create(WorkspaceId, "2026/2027", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28)));
    }

    private CreateHarvestHandler CreateSut() => new(_harvests, new HarvestLinkResolver(_plots, _seasons));

    private UpdateHarvestHandler UpdateSut() => new(_harvests, new HarvestLinkResolver(_plots, _seasons));

    private static Harvest Sample(
        string product = HarvestProducts.AceitunaOlivar,
        string destination = HarvestDestinations.AceiteParaVenta,
        decimal kgs = 1000m,
        decimal? yield = null,
        decimal? liters = null)
        => Harvest.Create(WorkspaceId, PlotId, SeasonId, Date, product, kgs, destination, yield, liters, null, UserId);

    private static HarvestView ViewOf(Harvest harvest) => new(
        harvest.Id, WorkspaceId, PlotId, "Olivar Alto", SeasonId, "2026/2027",
        new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28), harvest.Date, harvest.Product,
        harvest.Kgs, harvest.Yield, harvest.Liters, harvest.Destination, harvest.UnitPrice,
        harvest.Version, harvest.CreatedAt, harvest.UpdatedAt);

    // ── Catálogo de producto (RN-030, CA-1) ─────────────────────────────────

    [Fact]
    public void Producto_Deberia_PertenecerAlCatalogoGlobalFijo()
    {
        // RN-030 — no es texto libre como el material de compra (RN-031): es un código de sistema
        var act = () => Sample(product: "aceituna picual de la vega");

        act.Should().Throw<HarvestValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationProductInvalid);
    }

    [Fact]
    public void Producto_Deberia_AdmitirElValorDelCatalogo()
    {
        Sample(product: HarvestProducts.AceitunaOlivar).Product.Should().Be("aceituna_olivar");
    }

    [Fact]
    public void CatalogoDeProducto_Deberia_TenerUnSoloValorEnElMvp()
    {
        // Decisión del PO (2026-07-29): la variedad pertenece al terreno y el producto al Workspace
        // (MVP-999, P-059/P-060). Mientras tanto el MVP está ligado al olivar.
        HarvestProducts.Supported.Should().BeEquivalentTo(["aceituna_olivar"]);
    }

    // ── Taxonomía de destino (RN-012, CA-1/CA-3) ────────────────────────────

    [Theory]
    [InlineData("venta_aceituna")]
    [InlineData("aceite_para_venta")]
    [InlineData("aceite_personal")]
    [InlineData("desconocido")]
    public void Destino_Deberia_AdmitirLosCuatroValoresCanonicos(string destination)
    {
        Sample(destination: destination).Destination.Should().Be(destination);
    }

    [Fact]
    public void Destino_NoDeberia_AdmitirValoresFueraDeLaTaxonomia()
    {
        // «Sin destino» es un **alias visual** (RN-012); el canon en base de datos es `desconocido`
        var act = () => Sample(destination: "Sin destino");

        act.Should().Throw<HarvestValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationDestinationInvalid);
    }

    [Fact]
    public void Destino_Desconocido_NoDeberia_DegradarElRestoDelRegistro()
    {
        // CA-3 — no conocer el cierre comercial no puede costar información: la partida se guarda
        // entera y sigue contando sus kilos y su rendimiento
        var harvest = Sample(destination: HarvestDestinations.Desconocido, kgs: 1000m, yield: 18.5m);

        harvest.Kgs.Should().Be(1000m);
        harvest.Yield.Should().Be(18.5m);
    }

    // ── Unidad canónica y entradas equivalentes (RN-013/RN-014/RN-016) ──────

    [Fact]
    public async Task Create_Deberia_ConvertirKgPor100kg_ALaUnidadCanonica()
    {
        // RN-014 (2) + RN-016 — 20 kg de aceite por 100 kg con densidad 0,92 kg/L son 21,7391 L/100kg
        Harvest? added = null;
        await _harvests.AddAsync(Arg.Do<Harvest>(h => added = h), Arg.Any<CancellationToken>());
        _harvests.GetViewAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(added!));

        await CreateSut().HandleAsync(new CreateHarvestCommand(
            WorkspaceId, UserId, Date, PlotId, SeasonId, HarvestProducts.AceitunaOlivar, 1000m,
            HarvestDestinations.AceiteParaVenta, 20m, null, HarvestYieldUnits.Kg100Kg));

        added!.Yield.Should().Be(21.7391m);
    }

    [Fact]
    public async Task Create_Deberia_GuardarTalCual_LaUnidadCanonica()
    {
        // RN-014 (1) — informado ya en L/100kg: no se toca. Ausencia de unidad ⇒ canónica.
        Harvest? added = null;
        await _harvests.AddAsync(Arg.Do<Harvest>(h => added = h), Arg.Any<CancellationToken>());
        _harvests.GetViewAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(added!));

        await CreateSut().HandleAsync(new CreateHarvestCommand(
            WorkspaceId, UserId, Date, PlotId, SeasonId, HarvestProducts.AceitunaOlivar, 1000m,
            HarvestDestinations.AceiteParaVenta, 21.5m, null, null));

        added!.Yield.Should().Be(21.5m);
    }

    [Fact]
    public async Task Create_NoDeberia_AdmitirUnaUnidadDesconocida()
    {
        var act = () => CreateSut().HandleAsync(new CreateHarvestCommand(
            WorkspaceId, UserId, Date, PlotId, SeasonId, HarvestProducts.AceitunaOlivar, 1000m,
            HarvestDestinations.AceiteParaVenta, 20m, null, "porcentaje"));

        (await act.Should().ThrowAsync<HarvestValidationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationHarvestYieldUnitInvalid);
    }

    [Fact]
    public async Task Update_NoDeberia_ReconvertirLoYaPersistido()
    {
        // La unidad aplica al valor que llega en **esta** petición. Lo guardado ya está en la canónica,
        // así que un PATCH que no toca el rendimiento no puede volver a dividirlo por la densidad.
        var harvest = Sample(yield: 21.7391m);
        _harvests.FindByIdAsync(WorkspaceId, harvest.Id, Arg.Any<CancellationToken>()).Returns(harvest);
        _harvests.GetViewAsync(WorkspaceId, harvest.Id, Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(harvest));

        await UpdateSut().HandleAsync(new UpdateHarvestCommand(
            WorkspaceId, UserId, harvest.Id, 1,
            FieldUpdate<DateOnly>.Absent, FieldUpdate<Guid>.Absent, FieldUpdate<Guid>.Absent,
            FieldUpdate<string>.Absent, FieldUpdate<decimal>.Set(1500m), FieldUpdate<string>.Absent,
            FieldUpdate<decimal?>.Absent, FieldUpdate<decimal?>.Absent, HarvestYieldUnits.Kg100Kg));

        harvest.Yield.Should().Be(21.7391m);
    }

    // ── Rendimiento efectivo (RN-014, tercer origen) ────────────────────────

    [Fact]
    public void RendimientoEfectivo_Deberia_DerivarseDeLosLitros()
    {
        // RN-014 (3) — 220 L de 1.000 kg son 22 L/100kg. RN-004 impide guardar los dos, pero eso no
        // puede costar información: el dashboard necesita poder promediar esta partida.
        var view = ViewOf(Sample(kgs: 1000m, liters: 220m));

        view.EffectiveYield.Should().Be(22m);
        view.YieldSource.Should().Be("calculado");
    }

    [Fact]
    public void RendimientoEfectivo_Deberia_PreferirElInformado()
    {
        var view = ViewOf(Sample(kgs: 1000m, yield: 18.5m));

        view.EffectiveYield.Should().Be(18.5m);
        view.YieldSource.Should().Be("informado");
    }

    [Fact]
    public void RendimientoEfectivo_Deberia_SerNulo_SinDatoDeAceite()
    {
        // No se inventa un rendimiento a partir de datos incompletos
        var view = ViewOf(Sample(kgs: 1000m));

        view.EffectiveYield.Should().BeNull();
        view.YieldSource.Should().BeNull();
    }

    [Fact]
    public void Conversion_Deberia_ExponerLaDensidadDeRn016()
    {
        // RN-016 — la densidad vive en un único sitio, para que el día que se parametrice por almazara
        // (MVP-999, P-061) cambie de origen y no de fórmula
        HarvestYieldConversion.DefaultOilDensityKgPerLitre.Should().Be(0.92m);
    }
}
