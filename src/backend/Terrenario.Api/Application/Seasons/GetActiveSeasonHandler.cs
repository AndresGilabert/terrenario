using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Seasons;

/// <summary>
/// MVP-201 — Devuelve la temporada activa del Workspace (RN-021/RN-022). La usa el paso 2 del
/// onboarding para prellenar la propuesta y, más adelante, la autoselección operativa.
/// </summary>
public sealed class GetActiveSeasonHandler(ISeasonRepository seasonRepository)
{
    public async Task<SeasonSummary?> HandleAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var season = await seasonRepository.FindActiveByWorkspaceAsync(workspaceId, ct);
        return season is null ? null : ToSummary(season);
    }

    internal static SeasonSummary ToSummary(Season season) => new(
        season.Id,
        season.WorkspaceId,
        season.Name,
        season.StartDate,
        season.EndDate,
        season.IsActive,
        season.IsClosed,
        season.Status);
}
