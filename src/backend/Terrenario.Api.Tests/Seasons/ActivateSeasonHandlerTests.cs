using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Seasons;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Tests.Seasons;

public class ActivateSeasonHandlerTests
{
    private readonly ISeasonRepository _seasonRepository = Substitute.For<ISeasonRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private ActivateSeasonHandler CreateSut() => new(_seasonRepository);

    [Fact]
    public async Task Deberia_FijarLaTemporadaDeTrabajoDelUsuario_SinReabrirla()
    {
        // MVP-209 (CA-2/CA-4) — activar = fijar mi temporada de trabajo, sin reabrir una cerrada.
        var season = Season.Create(WorkspaceId, "Campaña 2025", new DateOnly(2025, 1, 1), null);
        season.Close();
        var seasonId = season.Id;

        _seasonRepository.FindByIdAsync(WorkspaceId, seasonId, Arg.Any<CancellationToken>())
            .Returns(season);

        var result = await CreateSut().HandleAsync(UserId, WorkspaceId, seasonId);

        result.Should().NotBeNull();
        result!.IsWorking.Should().BeTrue();
        // No se reabre: sigue cerrada (reabrir es una acción explícita del maestro).
        result.Status.Should().Be(SeasonStatus.Cerrada);
        await _seasonRepository.Received(1).SetWorkingSeasonAsync(
            UserId, WorkspaceId, seasonId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NoDeberia_TocarLaDeOtrosUsuarios()
    {
        // CA-2 — fijar la mía no cambia la de otro miembro.
        var season = Season.Create(WorkspaceId, "Campaña 2026", new DateOnly(2026, 9, 1), null);
        _seasonRepository.FindByIdAsync(WorkspaceId, season.Id, Arg.Any<CancellationToken>()).Returns(season);

        await CreateSut().HandleAsync(UserId, WorkspaceId, season.Id);

        await _seasonRepository.DidNotReceive().SetWorkingSeasonAsync(
            Arg.Is<Guid>(u => u != UserId), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_DevolverNull_Cuando_LaTemporadaNoExisteEnElWorkspace()
    {
        var seasonId = Guid.NewGuid();
        _seasonRepository.FindByIdAsync(WorkspaceId, seasonId, Arg.Any<CancellationToken>())
            .Returns((Season?)null);

        var result = await CreateSut().HandleAsync(UserId, WorkspaceId, seasonId);

        // 404 en el borde de transporte; no se fija nada.
        result.Should().BeNull();
        await _seasonRepository.DidNotReceive().SetWorkingSeasonAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
