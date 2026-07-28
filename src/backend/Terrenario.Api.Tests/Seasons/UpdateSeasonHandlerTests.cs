using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Seasons;
using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Tests.Seasons;

public class UpdateSeasonHandlerTests
{
    private readonly ISeasonRepository _seasonRepository = Substitute.For<ISeasonRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private UpdateSeasonHandler CreateSut() => new(_seasonRepository);

    private void Seed(Season season) =>
        _seasonRepository.FindByIdAsync(WorkspaceId, season.Id, Arg.Any<CancellationToken>())
            .Returns(season);

    [Fact]
    public async Task Deberia_EditarSoloLosCamposPresentes_YConservarElResto()
    {
        // Arrange
        var season = Season.Create(WorkspaceId, "Campaña 2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        Seed(season);
        var sut = CreateSut();

        // Act — solo cambia el nombre; fechas y estado se conservan.
        var result = await sut.HandleAsync(new UpdateSeasonCommand(
            WorkspaceId, season.Id,
            FieldUpdate<string>.Set("Campaña Oliva 2026"),
            FieldUpdate<DateOnly>.Absent,
            FieldUpdate<DateOnly?>.Absent,
            FieldUpdate<bool>.Absent));

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Campaña Oliva 2026");
        result.StartDate.Should().Be(new DateOnly(2026, 1, 1));
        result.EndDate.Should().Be(new DateOnly(2026, 12, 31));
        result.Status.Should().Be(SeasonStatus.Activa);
        await _seasonRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_CerrarLaTemporada_Cuando_IsClosedEsTrue()
    {
        // Arrange
        var season = Season.Create(WorkspaceId, "Campaña 2026", new DateOnly(2026, 1, 1), null);
        Seed(season);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(new UpdateSeasonCommand(
            WorkspaceId, season.Id,
            FieldUpdate<string>.Absent,
            FieldUpdate<DateOnly>.Absent,
            FieldUpdate<DateOnly?>.Absent,
            FieldUpdate<bool>.Set(true)));

        // Assert — cerrar la activa la desactiva (RN-024 informativo, libera el hueco de activa).
        result!.IsClosed.Should().BeTrue();
        result.IsActive.Should().BeFalse();
        result.Status.Should().Be(SeasonStatus.Cerrada);
    }

    [Fact]
    public async Task Deberia_DevolverNull_Cuando_LaTemporadaNoExisteEnElWorkspace()
    {
        // Arrange
        var seasonId = Guid.NewGuid();
        _seasonRepository.FindByIdAsync(WorkspaceId, seasonId, Arg.Any<CancellationToken>())
            .Returns((Season?)null);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(new UpdateSeasonCommand(
            WorkspaceId, seasonId,
            FieldUpdate<string>.Set("X"),
            FieldUpdate<DateOnly>.Absent,
            FieldUpdate<DateOnly?>.Absent,
            FieldUpdate<bool>.Absent));

        // Assert
        result.Should().BeNull();
        await _seasonRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RechazarRenombrado_Cuando_ElNombreYaExiste_DejandoLaTemporadaIntacta()
    {
        // MVP-207 (CA-2) — renombrar tampoco puede dejar dos campañas con el mismo nombre.
        var season = Season.Create(WorkspaceId, "Campaña 2026", new DateOnly(2026, 1, 1), null);
        Seed(season);
        _seasonRepository.ExistsWithNameAsync(
                WorkspaceId, "2025/2026", season.Id, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        var act = () => sut.HandleAsync(new UpdateSeasonCommand(
            WorkspaceId, season.Id,
            FieldUpdate<string>.Set("2025/2026"),
            FieldUpdate<DateOnly>.Absent,
            FieldUpdate<DateOnly?>.Absent,
            FieldUpdate<bool>.Absent));

        (await act.Should().ThrowAsync<SeasonConflictException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ConflictSeasonNameDuplicate);
        season.Name.Should().Be("Campaña 2026");
        await _seasonRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_ExcluirLaPropiaTemporada_AlComprobarDuplicados()
    {
        // Cambiar solo las mayúsculas del propio nombre no es un conflicto consigo misma.
        var season = Season.Create(WorkspaceId, "Campaña 2026", new DateOnly(2026, 1, 1), null);
        Seed(season);
        var sut = CreateSut();

        await sut.HandleAsync(new UpdateSeasonCommand(
            WorkspaceId, season.Id,
            FieldUpdate<string>.Set("CAMPAÑA 2026"),
            FieldUpdate<DateOnly>.Absent,
            FieldUpdate<DateOnly?>.Absent,
            FieldUpdate<bool>.Absent));

        await _seasonRepository.Received(1).ExistsWithNameAsync(
            WorkspaceId, "CAMPAÑA 2026", season.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_NoConsultarDuplicados_Cuando_ElPatchNoTraeNombre()
    {
        // Regresión del PATCH parcial: cerrar una temporada no es un renombrado.
        var season = Season.Create(WorkspaceId, "Campaña 2026", new DateOnly(2026, 1, 1), null);
        Seed(season);
        var sut = CreateSut();

        await sut.HandleAsync(new UpdateSeasonCommand(
            WorkspaceId, season.Id,
            FieldUpdate<string>.Absent,
            FieldUpdate<DateOnly>.Absent,
            FieldUpdate<DateOnly?>.Absent,
            FieldUpdate<bool>.Set(true)));

        await _seasonRepository.DidNotReceive().ExistsWithNameAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }
}
