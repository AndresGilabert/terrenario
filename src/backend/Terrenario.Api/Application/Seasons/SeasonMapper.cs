using Terrenario.Api.Application.Seasons.Commands;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Seasons;

/// <summary>
/// Construye la vista de temporada (MVP-209). Necesita dos cosas que no viven en el agregado: la fecha
/// de referencia («hoy») para el estado derivado y el id de la temporada de <b>trabajo del usuario</b>
/// que consulta, para marcar cuál es la suya.
/// </summary>
public static class SeasonMapper
{
    public static SeasonSummary ToSummary(Season season, DateOnly today, Guid? workingSeasonId) => new(
        season.Id,
        season.WorkspaceId,
        season.Name,
        season.StartDate,
        season.EndDate,
        season.IsClosed,
        season.Id == workingSeasonId,
        season.StatusOn(today));

    /// <summary>Fecha de referencia para el estado derivado: hoy en UTC.</summary>
    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);
}
