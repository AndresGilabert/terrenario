using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Seasons;

/// <summary>
/// MVP-209 — Devuelve la temporada de <b>trabajo del usuario</b> en el Workspace (RN-021): la fijada en
/// su membresía, o el defecto de <see cref="WorkingSeasonPolicy"/>. La usan el onboarding, la cabecera y
/// la autoselección operativa (a través del frontend). <c>null</c> solo si el Workspace no tiene ninguna
/// temporada.
/// </summary>
public sealed class GetActiveSeasonHandler(ISeasonRepository seasonRepository)
{
    public async Task<SeasonSummary?> HandleAsync(
        Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        var season = await seasonRepository.FindWorkingSeasonAsync(userId, workspaceId, ct);
        // La resuelta ES la de trabajo, así que se marca como tal.
        return season is null ? null : SeasonMapper.ToSummary(season, SeasonMapper.Today(), season.Id);
    }
}
