using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Tests.Seasons;

public class SeasonTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly DateOnly Start = new(2026, 1, 15);

    private static readonly DateOnly Today = new(2026, 6, 1);

    [Fact]
    public void Create_Deberia_DejarTemporadaAbierta()
    {
        var season = Season.Create(WorkspaceId, "Campaña Oliva 2026", Start, new DateOnly(2026, 12, 30));

        season.WorkspaceId.Should().Be(WorkspaceId);
        season.Name.Should().Be("Campaña Oliva 2026");
        season.StartDate.Should().Be(Start);
        season.EndDate.Should().Be(new DateOnly(2026, 12, 30));
        season.IsClosed.Should().BeFalse();
    }

    [Fact]
    public void Create_Deberia_NormalizarNombreYPermitirFinNulo()
    {
        var season = Season.Create(WorkspaceId, "  Campaña 2026  ", Start, null);

        season.Name.Should().Be("Campaña 2026");
        season.EndDate.Should().BeNull();
    }

    [Fact]
    public void Create_Deberia_Rechazar_Cuando_WorkspaceEsVacio()
    {
        var act = () => Season.Create(Guid.Empty, "Campaña 2026", Start, null);

        act.Should().Throw<SeasonValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredSeasonWorkspace);
    }

    [Fact]
    public void Create_Deberia_Rechazar_Cuando_NombreEsVacio()
    {
        var act = () => Season.Create(WorkspaceId, "   ", Start, null);

        act.Should().Throw<SeasonValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredSeasonName);
    }

    [Fact]
    public void Create_Deberia_Rechazar_Cuando_NombreEsDemasiadoLargo()
    {
        var act = () => Season.Create(WorkspaceId, new string('a', Season.NameMaxLength + 1), Start, null);

        act.Should().Throw<SeasonValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationSeasonNameLength);
    }

    [Fact]
    public void Create_Deberia_Rechazar_Cuando_FinEsAnteriorAInicio()
    {
        var act = () => Season.Create(WorkspaceId, "Campaña 2026", new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 30));

        act.Should().Throw<SeasonValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationSeasonDateRange);
    }

    // ── Estado derivado por fechas (MVP-209) ────────────────────────────────────

    [Fact]
    public void StatusOn_Deberia_SerAbierta_SiYaInicioYNoEstaCerrada()
    {
        // Start = 15-ene-2026; hoy 1-jun-2026 → ya iniciada, no cerrada → abierta.
        var season = Season.Create(WorkspaceId, "Campaña 2026", Start, null);

        season.StatusOn(Today).Should().Be(SeasonStatus.Abierta);
    }

    [Fact]
    public void StatusOn_Deberia_SerAbierta_AunqueLaCampanaYaHayaPasado()
    {
        // Una campaña pasada no cerrada sigue abierta a registros tardíos (RN-024): NO es «planificada».
        var pasada = Season.Create(WorkspaceId, "Campaña 2024", new DateOnly(2024, 9, 1), new DateOnly(2025, 2, 28));

        pasada.StatusOn(Today).Should().Be(SeasonStatus.Abierta);
    }

    [Fact]
    public void StatusOn_Deberia_SerPlanificada_SiAunNoHaIniciado()
    {
        var futura = Season.Create(WorkspaceId, "Campaña 2027", new DateOnly(2027, 9, 1), null);

        futura.StatusOn(Today).Should().Be(SeasonStatus.Planificada);
    }

    [Fact]
    public void Close_Deberia_DejarlaCerrada_SinTocarLaDeTrabajo()
    {
        var season = Season.Create(WorkspaceId, "Campaña 2026", Start, null);

        season.Close();

        season.IsClosed.Should().BeTrue();
        season.StatusOn(Today).Should().Be(SeasonStatus.Cerrada);
    }

    [Fact]
    public void Reopen_Deberia_DevolverlaAlEstadoPorFechas()
    {
        var season = Season.Create(WorkspaceId, "Campaña 2026", Start, null);
        season.Close();

        season.Reopen();

        season.IsClosed.Should().BeFalse();
        // Ya iniciada → abierta (no reintroduce el concepto de «activa», que ya no existe).
        season.StatusOn(Today).Should().Be(SeasonStatus.Abierta);
    }

    [Fact]
    public void UpdateDetails_Deberia_CambiarNombreYFechas_NormalizandoNombre()
    {
        var season = Season.Create(WorkspaceId, "Campaña 2026", Start, null);

        season.UpdateDetails("  Campaña Oliva 2027  ", new DateOnly(2027, 2, 1), new DateOnly(2027, 11, 30));

        season.Name.Should().Be("Campaña Oliva 2027");
        season.StartDate.Should().Be(new DateOnly(2027, 2, 1));
        season.EndDate.Should().Be(new DateOnly(2027, 11, 30));
        // Editar la fecha de inicio puede mover el estado: 1-feb-2027 aún no ha llegado el 1-jun-2026.
        season.StatusOn(Today).Should().Be(SeasonStatus.Planificada);
    }

    [Fact]
    public void UpdateDetails_Deberia_Rechazar_Cuando_FinEsAnteriorAInicio()
    {
        var season = Season.Create(WorkspaceId, "Campaña 2026", Start, null);

        var act = () => season.UpdateDetails("Campaña 2026", new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 30));

        act.Should().Throw<SeasonValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationSeasonDateRange);
    }
}
