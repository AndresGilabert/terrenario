using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Seasons;

/// <summary>
/// MVP-201 — Crea la (primera) temporada activa del Workspace activo. Es la contrapartida de la
/// oferta cancelable del frontend (al crear el Workspace o cuando el Workspace activo no tiene
/// temporada). Si ya existe una temporada activa, rechaza (RN-022): gestionar varias es MVP-203.
/// </summary>
public sealed class CreateSeasonHandler(ISeasonRepository seasonRepository)
{
    public async Task<SeasonSummary> HandleAsync(CreateSeasonCommand command, CancellationToken ct = default)
    {
        var existing = await seasonRepository.FindActiveByWorkspaceAsync(command.WorkspaceId, ct);
        if (existing is not null)
            throw new SeasonConflictException(
                ErrorCodes.BusinessRuleSeasonAlreadyActive,
                "El Workspace ya tiene una temporada activa.");

        var season = Season.Create(command.WorkspaceId, command.Name, command.StartDate, command.EndDate);
        await seasonRepository.AddAsync(season, ct);
        await seasonRepository.SaveChangesAsync(ct);

        return GetActiveSeasonHandler.ToSummary(season);
    }
}
