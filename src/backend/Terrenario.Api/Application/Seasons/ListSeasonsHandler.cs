using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Seasons;

/// <summary>
/// MVP-203 · MVP-209 — Lista las temporadas del Workspace para el maestro (HU-1), marcando cuál es la
/// temporada de trabajo <b>del usuario que consulta</b> (<c>is_working</c>). El estado (planificada/
/// abierta/cerrada) es derivado e independiente de esa marca.
/// </summary>
public sealed class ListSeasonsHandler(ISeasonRepository seasonRepository)
{
    public async Task<IReadOnlyList<SeasonSummary>> HandleAsync(
        Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var seasons = await seasonRepository.ListByWorkspaceAsync(workspaceId, ct);
        var working = await seasonRepository.FindWorkingSeasonAsync(userId, workspaceId, ct);
        var today = SeasonMapper.Today();
        return seasons.Select(s => SeasonMapper.ToSummary(s, today, working?.Id)).ToList();
    }
}
