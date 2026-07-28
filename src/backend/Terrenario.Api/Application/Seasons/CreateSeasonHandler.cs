using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Common.Errors;
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
///
/// MVP-207 (CA-2) añade la guarda de nombre único por Workspace: dos campañas «2025/2026» son
/// indistinguibles en pantalla y en cualquier informe posterior.
/// </summary>
public sealed class CreateSeasonHandler(ISeasonRepository seasonRepository)
{
    public async Task<SeasonSummary> HandleAsync(CreateSeasonCommand command, CancellationToken ct = default)
    {
        // El dominio normaliza y valida el nombre; se construye primero para no comprobar duplicados
        // contra un texto sin normalizar.
        var season = Season.Create(command.WorkspaceId, command.Name, command.StartDate, command.EndDate);

        await EnsureNameIsFreeAsync(seasonRepository, command.WorkspaceId, season.Name, null, ct);

        await seasonRepository.ActivateExclusivelyAsync(season, isNew: true, ct);

        return GetActiveSeasonHandler.ToSummary(season);
    }

    /// <summary>
    /// Guarda de duplicados del maestro, compartida con la edición. Lanza
    /// <see cref="SeasonConflictException"/> (409) si el nombre ya existe en el Workspace.
    /// </summary>
    internal static async Task EnsureNameIsFreeAsync(
        ISeasonRepository seasonRepository,
        Guid workspaceId,
        string normalizedName,
        Guid? excludeSeasonId,
        CancellationToken ct)
    {
        var exists = await seasonRepository.ExistsWithNameAsync(workspaceId, normalizedName, excludeSeasonId, ct);
        if (exists)
            throw new SeasonConflictException(
                ErrorCodes.ConflictSeasonNameDuplicate,
                $"Ya existe una temporada «{normalizedName}» en este Workspace.");
    }
}
