using Terrenario.Api.Common;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Seasons.Commands;

/// <summary>
/// Vista de una temporada para el maestro. <see cref="Status"/> es el estado informativo derivado
/// (planificada/abierta/cerrada, MVP-209), independiente de la de trabajo; <see cref="IsWorking"/>
/// indica si es la temporada de trabajo <b>del usuario que consulta</b>.
/// </summary>
public sealed record SeasonSummary(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsClosed,
    bool IsWorking,
    SeasonStatus Status);

/// <summary>
/// Creación de una temporada del Workspace activo. Desde MVP-209 la nueva temporada pasa a ser la
/// temporada de <b>trabajo del creador</b> (P-017, ahora por usuario), sin desbancar a nadie. El
/// Workspace nunca viaja como parámetro: se resuelve en servidor desde el contexto de scope (RN-034).
/// </summary>
public sealed record CreateSeasonCommand(
    Guid WorkspaceId,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate);

/// <summary>
/// Edición de los datos descriptivos de una temporada y/o su cierre/reapertura (MVP-203, HU-1). El
/// cambio de temporada de trabajo NO va aquí: es una acción propia
/// (<c>POST /seasons/{id}/activate</c>). Los campos ausentes conservan su valor actual.
/// </summary>
public sealed record UpdateSeasonCommand(
    Guid WorkspaceId,
    Guid SeasonId,
    FieldUpdate<string> Name,
    FieldUpdate<DateOnly> StartDate,
    FieldUpdate<DateOnly?> EndDate,
    FieldUpdate<bool> IsClosed);
