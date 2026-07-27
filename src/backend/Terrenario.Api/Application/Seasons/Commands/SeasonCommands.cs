using Terrenario.Api.Common;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Seasons.Commands;

/// <summary>
/// Vista de una temporada para el maestro (MVP-203). Incluye los booleanos canónicos y el estado
/// derivado (<see cref="Status"/>: planificada/activa/cerrada) que la UI usa para las etiquetas y las
/// acciones disponibles.
/// </summary>
public sealed record SeasonSummary(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive,
    bool IsClosed,
    SeasonStatus Status);

/// <summary>
/// Creación de una temporada del Workspace activo. En MVP-203 la nueva temporada pasa a ser la activa
/// (decisión de producto: crear cambia la activa), desbancando a la anterior; la primera temporada de
/// un Workspace (sin ninguna activa) simplemente nace activa (preserva el onboarding de MVP-201). El
/// Workspace nunca viaja como parámetro: se resuelve en servidor desde el contexto de scope (RN-034).
/// </summary>
public sealed record CreateSeasonCommand(
    Guid WorkspaceId,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate);

/// <summary>
/// Edición de los datos descriptivos de una temporada y/o su cierre/reapertura (MVP-203, HU-1). El
/// cambio de temporada activa NO va aquí: es una acción propia (<c>POST /seasons/{id}/activate</c>)
/// por el desbanque de la activa anterior. Los campos ausentes conservan su valor actual.
/// </summary>
public sealed record UpdateSeasonCommand(
    Guid WorkspaceId,
    Guid SeasonId,
    FieldUpdate<string> Name,
    FieldUpdate<DateOnly> StartDate,
    FieldUpdate<DateOnly?> EndDate,
    FieldUpdate<bool> IsClosed);
