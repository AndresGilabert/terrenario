using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Harvests;
using Terrenario.Api.Application.Harvests.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Harvests;
using Terrenario.Api.Domain.Operations;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Tests.Harvests;

/// <summary>
/// Tests de los casos de uso de cosecha (MVP-401) con repositorios mockeados: aislamiento por
/// Workspace (404), guarda de vínculos (<c>FOREIGN_KEY_WORKSPACE_MISMATCH</c>), edición parcial del
/// par excluyente rendimiento/litros y concurrencia optimista (CA-5). La traducción a SQL se cubre
/// aparte contra SQLite real (P-014).
/// </summary>
public class HarvestHandlersTests
{
    private readonly IHarvestRepository _harvests = Substitute.For<IHarvestRepository>();
    private readonly IPlotRepository _plots = Substitute.For<IPlotRepository>();
    private readonly ISeasonRepository _seasons = Substitute.For<ISeasonRepository>();

    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PlotId = Guid.NewGuid();
    private static readonly Guid SeasonId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 10, 20);

    public HarvestHandlersTests()
    {
        // Por defecto todos los vínculos existen en el Workspace activo.
        _plots.FindByIdAsync(WorkspaceId, PlotId, Arg.Any<CancellationToken>())
            .Returns(Plot.Create(WorkspaceId, "Olivar Alto", "propia"));
        _seasons.FindByIdAsync(WorkspaceId, SeasonId, Arg.Any<CancellationToken>())
            .Returns(Season.Create(WorkspaceId, "2026/2027", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28)));
    }

    private HarvestLinkResolver Resolver() => new(_plots, _seasons);

    private CreateHarvestHandler CreateSut() => new(_harvests, Resolver());

    private UpdateHarvestHandler UpdateSut() => new(_harvests, Resolver());

    private DeleteHarvestHandler DeleteSut() => new(_harvests);

    private static CreateHarvestCommand ValidCreate(decimal? yield = 18.5m, decimal? liters = null)
        => new(WorkspaceId, UserId, Date, PlotId, SeasonId,
            "aceituna_olivar", 1200m, "aceite_para_venta", yield, liters);

    private static Harvest Existing(long version = 1, decimal? yield = 18.5m, decimal? liters = null)
    {
        var harvest = Harvest.Create(
            WorkspaceId, PlotId, SeasonId, Date, "aceituna_olivar", 1200m, "aceite_para_venta",
            yield, liters, null, UserId);

        for (var i = 1; i < version; i++)
            harvest.Update(PlotId, SeasonId, Date, "aceituna_olivar", 1200m, "aceite_para_venta",
                yield, liters, null, UserId);

        return harvest;
    }

    private static HarvestView ViewOf(Harvest harvest) => new(
        harvest.Id, WorkspaceId, PlotId, "Olivar Alto", SeasonId, "2026/2027",
        new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28), harvest.Date, harvest.Product,
        harvest.Kgs, harvest.Yield, harvest.Liters, harvest.Destination, harvest.UnitPrice,
        harvest.Version, harvest.CreatedAt, harvest.UpdatedAt);

    private static FieldUpdate<T> Absent<T>() => FieldUpdate<T>.Absent;

    private static UpdateHarvestCommand PatchOf(
        Guid harvestId,
        long expectedVersion,
        FieldUpdate<decimal>? kgs = null,
        FieldUpdate<string>? destination = null,
        FieldUpdate<decimal?>? yield = null,
        FieldUpdate<decimal?>? liters = null)
        => new(WorkspaceId, UserId, harvestId, expectedVersion,
            Absent<DateOnly>(), Absent<Guid>(), Absent<Guid>(), Absent<string>(),
            kgs ?? Absent<decimal>(), destination ?? Absent<string>(),
            yield ?? Absent<decimal?>(), liters ?? Absent<decimal?>());

    // ── Alta ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Deberia_PersistirLaCosecha()
    {
        Harvest? added = null;
        await _harvests.AddAsync(Arg.Do<Harvest>(h => added = h), Arg.Any<CancellationToken>());
        _harvests.GetViewAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(added!));

        var view = await CreateSut().HandleAsync(ValidCreate());

        view.Kgs.Should().Be(1200m);
        view.PlotName.Should().Be("Olivar Alto");
        await _harvests.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_NoDeberia_AdmitirTerrenoDeOtroWorkspace()
    {
        // FOREIGN_KEY_WORKSPACE_MISMATCH: 400 con mensaje útil, no un 500 por clave ajena rota
        var ajeno = Guid.NewGuid();

        var act = () => CreateSut().HandleAsync(ValidCreate() with { PlotId = ajeno });

        (await act.Should().ThrowAsync<HarvestValidationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ForeignKeyWorkspaceMismatch);
    }

    [Fact]
    public async Task Create_NoDeberia_ConsultarLosMaestros_SiElDominioYaRechaza()
    {
        // El orden importa: una petición mal formada no debe gastar consultas a los maestros
        var act = () => CreateSut().HandleAsync(ValidCreate(yield: 18.5m, liters: 220m));

        await act.Should().ThrowAsync<HarvestValidationException>();
        await _plots.DidNotReceive().FindByIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── Edición ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Deberia_Devolver404_SiLaCosechaNoEsDelWorkspace()
    {
        _harvests.FindByIdAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Harvest?)null);

        var result = await UpdateSut().HandleAsync(PatchOf(Guid.NewGuid(), 1));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_Deberia_ConservarLosCamposAusentes()
    {
        var harvest = Existing();
        _harvests.FindByIdAsync(WorkspaceId, harvest.Id, Arg.Any<CancellationToken>()).Returns(harvest);
        _harvests.GetViewAsync(WorkspaceId, harvest.Id, Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(harvest));

        await UpdateSut().HandleAsync(
            PatchOf(harvest.Id, 1, kgs: FieldUpdate<decimal>.Set(1500m)));

        harvest.Kgs.Should().Be(1500m);
        harvest.Destination.Should().Be("aceite_para_venta");
        harvest.Yield.Should().Be(18.5m);
    }

    [Fact]
    public async Task Update_Deberia_SustituirLaParejaCompleta_AlCambiarARendimientoOLitros()
    {
        // RN-004 — enviar solo `liters` sobre una cosecha que ya tenía `yield` debe **sustituir** el
        // par, no dejar los dos informados: si no, el dominio rechazaría una petición razonable.
        var harvest = Existing(yield: 18.5m);
        _harvests.FindByIdAsync(WorkspaceId, harvest.Id, Arg.Any<CancellationToken>()).Returns(harvest);
        _harvests.GetViewAsync(WorkspaceId, harvest.Id, Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(harvest));

        await UpdateSut().HandleAsync(
            PatchOf(harvest.Id, 1, liters: FieldUpdate<decimal?>.Set(220m)));

        harvest.Liters.Should().Be(220m);
        harvest.Yield.Should().BeNull();
    }

    [Fact]
    public async Task Update_Deberia_PermitirRetirarElRendimiento_ConNullExplicito()
    {
        var harvest = Existing(yield: 18.5m);
        _harvests.FindByIdAsync(WorkspaceId, harvest.Id, Arg.Any<CancellationToken>()).Returns(harvest);
        _harvests.GetViewAsync(WorkspaceId, harvest.Id, Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(harvest));

        await UpdateSut().HandleAsync(
            PatchOf(harvest.Id, 1, yield: FieldUpdate<decimal?>.Set(null)));

        harvest.Yield.Should().BeNull();
        harvest.Liters.Should().BeNull();
    }

    [Fact]
    public async Task Update_Deberia_Lanzar409_ConVersionDesfasada()
    {
        // CA-5 — el bloqueo optimista se comprueba antes de tocar nada
        var harvest = Existing(version: 3);
        _harvests.FindByIdAsync(WorkspaceId, harvest.Id, Arg.Any<CancellationToken>()).Returns(harvest);

        var act = () => UpdateSut().HandleAsync(
            PatchOf(harvest.Id, 1, kgs: FieldUpdate<decimal>.Set(1500m)));

        (await act.Should().ThrowAsync<ConcurrencyConflictException>())
            .Which.CurrentVersion.Should().Be(3);
        harvest.Kgs.Should().Be(1200m);
    }

    // ── Borrado ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Deberia_SerLogico()
    {
        // RN-037 — la fila no se borra: deja de aparecer en listado, diario y dashboard
        var harvest = Existing();
        _harvests.FindByIdAsync(WorkspaceId, harvest.Id, Arg.Any<CancellationToken>()).Returns(harvest);

        var deleted = await DeleteSut().HandleAsync(
            new DeleteHarvestCommand(WorkspaceId, UserId, harvest.Id, 1));

        deleted.Should().BeTrue();
        harvest.IsDeleted.Should().BeTrue();
        await _harvests.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Deberia_Devolver404_SiYaEstabaEliminada()
    {
        // El puerto no devuelve lo eliminado, así que no se puede borrar dos veces lo mismo
        _harvests.FindByIdAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Harvest?)null);

        var deleted = await DeleteSut().HandleAsync(
            new DeleteHarvestCommand(WorkspaceId, UserId, Guid.NewGuid(), 1));

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_Deberia_Lanzar409_ConVersionDesfasada()
    {
        var harvest = Existing(version: 2);
        _harvests.FindByIdAsync(WorkspaceId, harvest.Id, Arg.Any<CancellationToken>()).Returns(harvest);

        var act = () => DeleteSut().HandleAsync(
            new DeleteHarvestCommand(WorkspaceId, UserId, harvest.Id, 1));

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
        harvest.IsDeleted.Should().BeFalse();
    }
}
