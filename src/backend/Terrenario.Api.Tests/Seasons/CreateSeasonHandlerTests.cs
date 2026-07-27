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
    public async Task Deberia_CrearTemporadaActiva_YPersistirlaComoUnicaActiva()
    {
        // Arrange — crear cambia la activa (MVP-203): se persiste con ActivateExclusivelyAsync(isNew:true),
        // que desbanca a la anterior si la hubiera.
        Season? persisted = null;
        await _seasonRepository.ActivateExclusivelyAsync(
            Arg.Do<Season>(s => persisted = s), true, Arg.Any<CancellationToken>());

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(
            CommandWith("Campaña Oliva 2026", new DateOnly(2026, 2, 1), new DateOnly(2026, 11, 30)));

        // Assert
        result.Name.Should().Be("Campaña Oliva 2026");
        result.IsActive.Should().BeTrue();
        result.Status.Should().Be(SeasonStatus.Activa);
        persisted.Should().NotBeNull();
        persisted!.WorkspaceId.Should().Be(WorkspaceId);
        await _seasonRepository.Received(1).ActivateExclusivelyAsync(
            Arg.Any<Season>(), true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_NoPersistir_Cuando_FechaFinEsAnteriorAInicio()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(
            CommandWith("Campaña 2026", new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1)));

        // Assert — la validación del agregado corta antes de tocar el repositorio.
        await act.Should().ThrowAsync<SeasonValidationException>();
        await _seasonRepository.DidNotReceive().ActivateExclusivelyAsync(
            Arg.Any<Season>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }
}
