using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Seasons;
using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Tests.Seasons;

public class CreateSeasonHandlerTests
{
    private readonly ISeasonRepository _seasonRepository = Substitute.For<ISeasonRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private CreateSeasonHandler CreateSut() => new(_seasonRepository);

    private static CreateSeasonCommand CommandWith(string name, DateOnly start, DateOnly? end) =>
        new(WorkspaceId, name, start, end);

    [Fact]
    public async Task Deberia_CrearTemporadaActiva_Cuando_ElWorkspaceNoTieneNinguna()
    {
        // Arrange
        _seasonRepository.FindActiveByWorkspaceAsync(WorkspaceId, Arg.Any<CancellationToken>())
            .Returns((Season?)null);

        Season? persisted = null;
        await _seasonRepository.AddAsync(
            Arg.Do<Season>(s => persisted = s), Arg.Any<CancellationToken>());

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(
            CommandWith("Campaña Oliva 2026", new DateOnly(2026, 2, 1), new DateOnly(2026, 11, 30)));

        // Assert
        result.Name.Should().Be("Campaña Oliva 2026");
        result.IsActive.Should().BeTrue();
        persisted.Should().NotBeNull();
        persisted!.WorkspaceId.Should().Be(WorkspaceId);
        await _seasonRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RechazarConConflicto_Cuando_YaExisteTemporadaActiva()
    {
        // Arrange — RN-022: gestionar varias es alcance de MVP-203
        _seasonRepository.FindActiveByWorkspaceAsync(WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(Season.Create(WorkspaceId, "Existente", new DateOnly(2026, 1, 1), null));

        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(
            CommandWith("Campaña 2026", new DateOnly(2026, 1, 1), null));

        // Assert
        await act.Should().ThrowAsync<SeasonConflictException>();
        await _seasonRepository.DidNotReceive().AddAsync(Arg.Any<Season>(), Arg.Any<CancellationToken>());
        await _seasonRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_NoPersistir_Cuando_FechaFinEsAnteriorAInicio()
    {
        // Arrange
        _seasonRepository.FindActiveByWorkspaceAsync(WorkspaceId, Arg.Any<CancellationToken>())
            .Returns((Season?)null);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(
            CommandWith("Campaña 2026", new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1)));

        // Assert
        await act.Should().ThrowAsync<SeasonValidationException>();
        await _seasonRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
