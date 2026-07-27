using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Seasons;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Tests.Seasons;

public class ActivateSeasonHandlerTests
{
    private readonly ISeasonRepository _seasonRepository = Substitute.For<ISeasonRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private ActivateSeasonHandler CreateSut() => new(_seasonRepository);

    [Fact]
    public async Task Deberia_ActivarLaTemporada_YPersistirlaComoUnicaActiva()
    {
        // Arrange — una temporada planificada (cerrada, para probar además la reapertura al activar).
        var season = Season.Create(WorkspaceId, "Campaña 2025", new DateOnly(2025, 1, 1), null);
        season.Close();
        var seasonId = season.Id;

        _seasonRepository.FindByIdAsync(WorkspaceId, seasonId, Arg.Any<CancellationToken>())
            .Returns(season);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(WorkspaceId, seasonId);

        // Assert
        result.Should().NotBeNull();
        result!.IsActive.Should().BeTrue();
        result.Status.Should().Be(SeasonStatus.Activa);
        await _seasonRepository.Received(1).ActivateExclusivelyAsync(
            season, false, Arg.Any<CancellationToken>());
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
        var result = await sut.HandleAsync(WorkspaceId, seasonId);

        // Assert — 404 en el borde de transporte; no se persiste nada.
        result.Should().BeNull();
        await _seasonRepository.DidNotReceive().ActivateExclusivelyAsync(
            Arg.Any<Season>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }
}
