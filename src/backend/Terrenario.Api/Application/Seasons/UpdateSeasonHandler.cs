using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Seasons;

/// <summary>
/// MVP-203 — Edita una temporada del Workspace activo (HU-1/CA-2): nombre, fechas y cierre/reapertura
/// (RN-024). El cambio de temporada de trabajo NO se hace aquí (es <see cref="ActivateSeasonHandler"/>).
/// La temporada se busca acotada al Workspace: si no existe en él, devuelve <c>null</c> y el borde de
/// transporte responde 404.
///
/// MVP-207 (CA-2): renombrar tampoco puede dejar dos temporadas con el mismo nombre.
/// Recibe <paramref name="userId"/> solo para marcar <c>is_working</c> en la respuesta (MVP-209).
/// </summary>
public sealed class UpdateSeasonHandler(ISeasonRepository seasonRepository)
{
    public async Task<SeasonSummary?> HandleAsync(
        Guid userId, UpdateSeasonCommand command, CancellationToken ct = default)
    {
        var season = await seasonRepository.FindByIdAsync(command.WorkspaceId, command.SeasonId, ct);
        if (season is null) return null;

        // El nombre se normaliza y valida primero (400) y solo después se comprueba el duplicado
        // (409), sin tocar el agregado hasta que ambas guardas pasan. Se excluye la propia temporada:
        // cambiar solo las mayúsculas de su nombre no es un conflicto consigo misma.
        if (command.Name.Present)
        {
            var normalized = Season.NormalizeName(command.Name.Value!);
            await CreateSeasonHandler.EnsureNameIsFreeAsync(
                seasonRepository, command.WorkspaceId, normalized, season.Id, ct);
        }

        // Edición parcial: los campos ausentes conservan su valor actual.
        season.UpdateDetails(
            command.Name.Or(season.Name)!,
            command.StartDate.Or(season.StartDate),
            command.EndDate.Or(season.EndDate));

        // Cierre/reapertura (RN-024, informativo). No toca la temporada de trabajo de nadie (MVP-209):
        // cerrar es «ya no espero registros aquí», reabrir la devuelve a abierta/planificada por fechas.
        if (command.IsClosed.Present)
        {
            if (command.IsClosed.Value) season.Close();
            else season.Reopen();
        }

        await seasonRepository.SaveChangesAsync(ct);

        var working = await seasonRepository.FindWorkingSeasonAsync(userId, command.WorkspaceId, ct);
        return SeasonMapper.ToSummary(season, SeasonMapper.Today(), working?.Id);
    }
}
