using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Harvests;
using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Tests.Harvests;

/// <summary>
/// Tests del agregado <see cref="Harvest"/> (MVP-401): las reglas que el registro de producción no
/// puede incumplir aunque el cliente insista.
/// </summary>
public class HarvestTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid PlotId = Guid.NewGuid();
    private static readonly Guid SeasonId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 10, 20);

    private static Harvest Create(
        decimal kgs = 1200m,
        decimal? yield = 18.5m,
        decimal? liters = null,
        string product = "aceituna_olivar",
        string destination = "aceite_para_venta")
        => Harvest.Create(
            WorkspaceId, PlotId, SeasonId, Date, product, kgs, destination, yield, liters, UserId);

    [Fact]
    public void Create_Deberia_RegistrarLaCosecha_ConLosCamposMinimos()
    {
        // CA-1 — fecha, terreno, temporada, producto, kilos y destino
        var harvest = Create();

        harvest.PlotId.Should().Be(PlotId);
        harvest.SeasonId.Should().Be(SeasonId);
        harvest.Date.Should().Be(Date);
        harvest.Product.Should().Be("aceituna_olivar");
        harvest.Kgs.Should().Be(1200m);
        harvest.Destination.Should().Be("aceite_para_venta");
        harvest.Yield.Should().Be(18.5m);
        harvest.Liters.Should().BeNull();
        harvest.Version.Should().Be(1);
        harvest.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_Deberia_AdmitirCosechaSinRendimientoNiLitros()
    {
        // RN-004 — los dos son **opcionales**: quien todavía no ha molturado registra solo los kilos
        var harvest = Create(yield: null, liters: null);

        harvest.Yield.Should().BeNull();
        harvest.Liters.Should().BeNull();
    }

    [Fact]
    public void Create_NoDeberia_AdmitirRendimientoYLitrosALaVez()
    {
        // RN-004 — son dos formas de medir lo mismo: guardarlas juntas permitiría que se contradijeran
        var act = () => Create(yield: 18.5m, liters: 220m);

        act.Should().Throw<HarvestValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationHarvestXorYieldLiters);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_NoDeberia_AdmitirKilosNoPositivos(decimal kgs)
    {
        // RN-004 — sin kilos no hay cosecha que medir
        var act = () => Create(kgs: kgs);

        act.Should().Throw<HarvestValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationHarvestKgsRequired);
    }

    [Fact]
    public void Create_NoDeberia_AdmitirProductoVacio()
    {
        // RN-030 — el producto es obligatorio en toda cosecha
        var act = () => Create(product: "   ");

        act.Should().Throw<HarvestValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationProductInvalid);
    }

    [Fact]
    public void Create_NoDeberia_AdmitirDestinoVacio()
    {
        // RN-012 — `desconocido` es un destino válido; dejar el campo en blanco no lo es
        var act = () => Create(destination: "");

        act.Should().Throw<HarvestValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationDestinationInvalid);
    }

    [Fact]
    public void Create_Deberia_AdmitirDestinoDesconocido()
    {
        // HU-2 de MVP-402, ya soportado por el agregado: no conocer el cierre comercial no bloquea
        var harvest = Create(destination: "desconocido");

        harvest.Destination.Should().Be("desconocido");
    }

    [Fact]
    public void Create_NoDeberia_AdmitirRendimientoImposible()
    {
        // No puede salir más aceite que fruto: por encima de 100 L/100kg siempre es un error de tecleo
        var act = () => Create(yield: 150m);

        act.Should().Throw<HarvestValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationHarvestYieldRange);
    }

    [Fact]
    public void Create_NoDeberia_ValidarElRangoDeLaTemporada()
    {
        // RN-023 — la fecha fuera de rango **avisa**, no bloquea: el aviso lo calcula la lectura
        var act = () => Harvest.Create(
            WorkspaceId, PlotId, SeasonId, new DateOnly(2019, 1, 1),
            "aceituna_olivar", 1200m, "desconocido", null, null, UserId);

        act.Should().NotThrow();
    }

    [Fact]
    public void Update_Deberia_SubirLaVersion()
    {
        // ADR-0005 — cada mutación mueve la versión: es lo que hace útil el If-Match
        var harvest = Create();

        harvest.Update(PlotId, SeasonId, Date, "aceituna_olivar", 1500m, "venta_aceituna", 19m, null, UserId);

        harvest.Kgs.Should().Be(1500m);
        harvest.Version.Should().Be(2);
    }

    [Fact]
    public void EnsureVersion_Deberia_LanzarConflicto_ConVersionDesfasada()
    {
        // CA-5 — dos personas corrigiendo la misma cosecha no pueden pisarse en silencio
        var harvest = Create();
        harvest.Update(PlotId, SeasonId, Date, "aceituna_olivar", 1500m, "venta_aceituna", null, null, UserId);

        var act = () => harvest.EnsureVersion(1);

        act.Should().Throw<ConcurrencyConflictException>()
            .Which.CurrentVersion.Should().Be(2);
    }

    [Fact]
    public void Delete_Deberia_SerLogico_Y_Idempotente()
    {
        // RN-037 — la fila permanece: un borrado accidental no destruye producción ya capturada
        var harvest = Create();

        harvest.Delete(UserId);
        var deletedAt = harvest.DeletedAt;
        harvest.Delete(UserId);

        harvest.IsDeleted.Should().BeTrue();
        harvest.DeletedAt.Should().Be(deletedAt);
        harvest.Version.Should().Be(2);
    }

    [Fact]
    public void Create_Deberia_RedondearALaPrecisionPersistida()
    {
        // Lo leído coincide con lo escrito, igual que en actividades y compras
        var harvest = Create(kgs: 1200.567m, yield: 18.54321m);

        harvest.Kgs.Should().Be(1200.57m);
        harvest.Yield.Should().Be(18.5432m);
    }
}
