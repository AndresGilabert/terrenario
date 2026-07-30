using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Seasons;
using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Tests.Seasons;

public class CreateSeasonHandlerTests
{
    private readonly ISeasonRepository _seasonRepository = Substitute.For<ISeasonRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private CreateSeasonHandler CreateSut() => new(_seasonRepository);

    private static CreateSeasonCommand CommandWith(string name, DateOnly start, DateOnly? end) =>
        new(WorkspaceId, name, start, end);

    [Fact]
    public async Task Deberia_CrearTemporada_YFijarlaComoLaDeTrabajoDelCreador()
    {
        // MVP-209 — crear la fija como la de trabajo del **creador**, sin desbancar a nadie.
        Season? persisted = null;
        await _seasonRepository.AddAsync(
            Arg.Do<Season>(s => persisted = s), Arg.Any<CancellationToken>());

        var result = await CreateSut().HandleAsync(
            UserId, CommandWith("Campaña Oliva 2026", new DateOnly(2026, 2, 1), new DateOnly(2026, 11, 30)));

        result.Name.Should().Be("Campaña Oliva 2026");
        result.IsWorking.Should().BeTrue();
        persisted.Should().NotBeNull();
        persisted!.WorkspaceId.Should().Be(WorkspaceId);
        // Se persiste la temporada y luego se fija en la membresía del creador.
        await _seasonRepository.Received(1).AddAsync(Arg.Any<Season>(), Arg.Any<CancellationToken>());
        await _seasonRepository.Received(1).SetWorkingSeasonAsync(
            UserId, WorkspaceId, persisted.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NoDeberia_FijarLaDeOtrosUsuarios()
    {
        Season? persisted = null;
        await _seasonRepository.AddAsync(
            Arg.Do<Season>(s => persisted = s), Arg.Any<CancellationToken>());

        await CreateSut().HandleAsync(UserId, CommandWith("Campaña 2026", new DateOnly(2026, 1, 1), null));

        // Solo se fija para el creador; ningún otro usuario se toca (CA-2).
        await _seasonRepository.Received(1).SetWorkingSeasonAsync(
            UserId, WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _seasonRepository.DidNotReceive().SetWorkingSeasonAsync(
            Arg.Is<Guid>(u => u != UserId), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_NoPersistir_Cuando_FechaFinEsAnteriorAInicio()
    {
        var act = async () => await CreateSut().HandleAsync(
            UserId, CommandWith("Campaña 2026", new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1)));

        // La validación del agregado corta antes de tocar el repositorio.
        await act.Should().ThrowAsync<SeasonValidationException>();
        await _seasonRepository.DidNotReceive().AddAsync(Arg.Any<Season>(), Arg.Any<CancellationToken>());
        await _seasonRepository.DidNotReceive().SetWorkingSeasonAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_ComprobarDuplicadosConElNombreYaNormalizado()
    {
        // MVP-207 (CA-2) — la guarda se consulta con el texto que se persistiría, no con el crudo.
        await CreateSut().HandleAsync(UserId, CommandWith("  Campaña 2026  ", new DateOnly(2026, 1, 1), null));

        await _seasonRepository.Received(1).ExistsWithNameAsync(
            WorkspaceId, "Campaña 2026", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RechazarNombreDuplicado_SinPersistir()
    {
        _seasonRepository.ExistsWithNameAsync(
                WorkspaceId, "2025/2026", null, Arg.Any<CancellationToken>())
            .Returns(true);

        var act = () => CreateSut().HandleAsync(UserId, CommandWith("2025/2026", new DateOnly(2025, 9, 1), null));

        (await act.Should().ThrowAsync<SeasonConflictException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ConflictSeasonNameDuplicate);
        await _seasonRepository.DidNotReceive().AddAsync(Arg.Any<Season>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RechazarNombreInvalido_AntesDeConsultarDuplicados()
    {
        var act = () => CreateSut().HandleAsync(UserId, CommandWith("   ", new DateOnly(2026, 1, 1), null));

        (await act.Should().ThrowAsync<SeasonValidationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredSeasonName);
        await _seasonRepository.DidNotReceive().ExistsWithNameAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }
}
