using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Tests.Seasons;

public class SeasonTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly DateOnly Start = new(2026, 1, 15);

    [Fact]
    public void Create_Deberia_DejarTemporadaActivaAbierta()
    {
        var season = Season.Create(WorkspaceId, "Campaña Oliva 2026", Start, new DateOnly(2026, 12, 30));

        season.WorkspaceId.Should().Be(WorkspaceId);
        season.Name.Should().Be("Campaña Oliva 2026");
        season.StartDate.Should().Be(Start);
        season.EndDate.Should().Be(new DateOnly(2026, 12, 30));
        season.IsActive.Should().BeTrue();
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
}
