using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Seasons;

/// <summary>
/// Crea una temporada del Workspace activo (MVP-201 · maestro MVP-203). La nueva temporada pasa a ser
/// la <b>activa</b> del Workspace, desbancando a la anterior si la hubiera (decisión de producto: crear
/// cambia la activa; RN-022 sigue garantizando una sola activa). La primera temporada de un Workspace
/// (sin ninguna activa) simplemente nace activa, preservando el flujo de onboarding de MVP-201.
///
/// El desbanque atómico de la activa anterior lo hace <see cref="ISeasonRepository.ActivateExclusivelyAsync"/>,
/// que ordena las descargas para no violar el índice único parcial.
/// </summary>
public sealed class CreateSeasonHandler(ISeasonRepository seasonRepository)
{
    public async Task<SeasonSummary> HandleAsync(CreateSeasonCommand command, CancellationToken ct = default)
    {
        var season = Season.Create(command.WorkspaceId, command.Name, command.StartDate, command.EndDate);
        await seasonRepository.ActivateExclusivelyAsync(season, isNew: true, ct);

        return GetActiveSeasonHandler.ToSummary(season);
    }
}
