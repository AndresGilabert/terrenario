using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Seasons;

/// <summary>
/// Crea una temporada del Workspace (MVP-201 · maestro MVP-203). Desde MVP-209, la nueva temporada pasa
/// a ser la temporada de <b>trabajo del creador</b> (P-017, ahora por usuario), <b>sin desbancar a
/// nadie</b>: cada usuario tiene la suya. Nace abierta o planificada según su fecha de inicio.
///
/// MVP-207 (CA-2) añade la guarda de nombre único por Workspace: dos campañas «2025/2026» son
/// indistinguibles en pantalla y en cualquier informe posterior.
/// </summary>
public sealed class CreateSeasonHandler(ISeasonRepository seasonRepository)
{
    public async Task<SeasonSummary> HandleAsync(
        Guid userId, CreateSeasonCommand command, CancellationToken ct = default)
    {
        // El dominio normaliza y valida el nombre; se construye primero para no comprobar duplicados
        // contra un texto sin normalizar.
        var season = Season.Create(command.WorkspaceId, command.Name, command.StartDate, command.EndDate);

        await EnsureNameIsFreeAsync(seasonRepository, command.WorkspaceId, season.Name, null, ct);

        // Se persiste la temporada antes de fijarla como de trabajo: la FK
        // workspace_members.active_season_id exige que exista.
        await seasonRepository.AddAsync(season, ct);
        await seasonRepository.SaveChangesAsync(ct);
        await seasonRepository.SetWorkingSeasonAsync(userId, command.WorkspaceId, season.Id, ct);

        return SeasonMapper.ToSummary(season, SeasonMapper.Today(), season.Id);
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
