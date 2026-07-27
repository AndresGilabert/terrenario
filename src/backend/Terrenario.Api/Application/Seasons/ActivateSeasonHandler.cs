using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Seasons;

/// <summary>
/// MVP-203 — Cambia la temporada activa del Workspace (HU-2/CA-1): activa la temporada indicada y
/// desbanca a la anterior, garantizando una sola activa (RN-022). Si la temporada estaba cerrada, se
/// reabre al activarla. La temporada se busca acotada al Workspace: si no existe, devuelve <c>null</c>
/// y el borde de transporte responde 404.
/// </summary>
public sealed class ActivateSeasonHandler(ISeasonRepository seasonRepository)
{
    public async Task<SeasonSummary?> HandleAsync(Guid workspaceId, Guid seasonId, CancellationToken ct = default)
    {
        var season = await seasonRepository.FindByIdAsync(workspaceId, seasonId, ct);
        if (season is null) return null;

        season.Activate();
        await seasonRepository.ActivateExclusivelyAsync(season, isNew: false, ct);

        return GetActiveSeasonHandler.ToSummary(season);
    }
}
