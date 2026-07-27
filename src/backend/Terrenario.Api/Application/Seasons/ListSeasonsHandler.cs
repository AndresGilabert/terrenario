using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Seasons;

/// <summary>
/// MVP-203 — Lista las temporadas del Workspace activo (HU-1). Devuelve la activa primero y luego el
/// histórico por fecha, para el maestro de temporadas.
/// </summary>
public sealed class ListSeasonsHandler(ISeasonRepository seasonRepository)
{
    public async Task<IReadOnlyList<SeasonSummary>> HandleAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var seasons = await seasonRepository.ListByWorkspaceAsync(workspaceId, ct);
        return seasons.Select(GetActiveSeasonHandler.ToSummary).ToList();
    }
}
