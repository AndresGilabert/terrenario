using Terrenario.Api.Common;

namespace Terrenario.Api.Application.Activities.Commands;

/// <summary>
/// Alta de actividad (MVP-301, HU-1). El Workspace y el usuario nunca viajan como parámetros de
/// negocio: se resuelven en servidor desde el contexto de scope y el claim de la sesión (RN-034).
/// </summary>
public sealed record CreateActivityCommand(
    Guid WorkspaceId,
    Guid UserId,
    DateOnly Date,
    Guid PlotId,
    Guid SeasonId,
    Guid WorkerId,
    Guid? TaskId,
    string? TaskText,
    decimal Hours,
    decimal ManualCost,
    string? Description);

/// <summary>
/// Edición parcial de actividad (MVP-301, HU-2, <c>PATCH</c>). Cada campo es un
/// <see cref="FieldUpdate{T}"/>: ausente conserva el valor actual.
///
/// <b>La tarea es un par excluyente</b> (RN-025): si viene <b>cualquiera</b> de
/// <see cref="TaskId"/>/<see cref="TaskText"/>, se sustituye la pareja completa y el miembro ausente
/// se interpreta como «sin valor». De lo contrario, enviar solo <c>task_id</c> sobre una actividad
/// con texto libre dejaría los dos informados y el dominio lo rechazaría, sin que el cliente pudiera
/// hacer nada razonable.
///
/// <see cref="ExpectedVersion"/> llega de la cabecera <c>If-Match</c> (ADR-0005).
/// </summary>
public sealed record UpdateActivityCommand(
    Guid WorkspaceId,
    Guid UserId,
    Guid ActivityId,
    long ExpectedVersion,
    FieldUpdate<DateOnly> Date,
    FieldUpdate<Guid> PlotId,
    FieldUpdate<Guid> SeasonId,
    FieldUpdate<Guid> WorkerId,
    FieldUpdate<Guid?> TaskId,
    FieldUpdate<string> TaskText,
    FieldUpdate<decimal> Hours,
    FieldUpdate<decimal> ManualCost,
    FieldUpdate<string> Description);

/// <summary>
/// Eliminación <b>lógica</b> de una actividad (RN-037). La confirmación explícita es responsabilidad
/// de la UI (MVP-305); aquí llega ya confirmada, pero con la versión vigente (ADR-0005).
/// </summary>
public sealed record DeleteActivityCommand(
    Guid WorkspaceId,
    Guid UserId,
    Guid ActivityId,
    long ExpectedVersion);
