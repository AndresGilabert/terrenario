namespace Terrenario.Api.Application.Seasons.Commands;

/// <summary>
/// Datos de la temporada que el cliente necesita para el onboarding y la autoselección operativa
/// (MVP-201). El estado se expone con los booleanos canónicos; el maestro de temporadas (MVP-203)
/// construirá sobre ellos los estados planificada/activa/cerrada.
/// </summary>
public sealed record SeasonSummary(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive,
    bool IsClosed);

/// <summary>
/// Creación de la (primera) temporada activa del Workspace activo (MVP-201). El Workspace nunca
/// viaja como parámetro: se resuelve en servidor desde el contexto de scope (RN-034, MVP-105).
/// </summary>
public sealed record CreateSeasonCommand(
    Guid WorkspaceId,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate);
