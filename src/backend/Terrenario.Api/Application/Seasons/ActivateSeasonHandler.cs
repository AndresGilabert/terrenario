using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Seasons;

/// <summary>
/// MVP-209 — Fija la temporada de <b>trabajo del usuario</b> (HU-2/CA-2): la indicada pasa a ser sobre
/// la que ese usuario registra por defecto, <b>sin afectar a otros miembros</b> y <b>sin reabrirla</b>
/// si estaba cerrada (CA-4: reabrir es una acción explícita del maestro). La temporada se busca acotada
/// al Workspace: si no existe, devuelve <c>null</c> y el borde de transporte responde 404.
/// </summary>
public sealed class ActivateSeasonHandler(ISeasonRepository seasonRepository)
{
    public async Task<SeasonSummary?> HandleAsync(
        Guid userId, Guid workspaceId, Guid seasonId, CancellationToken ct = default)
    {
        var season = await seasonRepository.FindByIdAsync(workspaceId, seasonId, ct);
        if (season is null) return null;

        await seasonRepository.SetWorkingSeasonAsync(userId, workspaceId, season.Id, ct);

        return SeasonMapper.ToSummary(season, SeasonMapper.Today(), season.Id);
    }
}
