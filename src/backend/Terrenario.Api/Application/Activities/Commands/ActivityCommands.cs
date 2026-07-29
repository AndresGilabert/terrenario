using Terrenario.Api.Application.Tasks;
using Terrenario.Api.Common;
using Terrenario.Api.Domain.Activities;

namespace Terrenario.Api.Application.Activities.Commands;

/// <summary>
/// Resultado de guardar una actividad. Además de la actividad, informa de qué pasó con el catálogo
/// cuando se pidió guardar en él la tarea escrita a mano (MVP-302): <c>null</c> si no se pidió.
/// </summary>
public sealed record ActivitySaveResult(
    ActivityView Activity,
    TaskCatalogOutcome? TaskCatalogOutcome);

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
    string? Description,
    /// <summary>
    /// MVP-302 — Guardar además en el catálogo del Workspace la tarea escrita en
    /// <see cref="TaskText"/>, para poder reutilizarla en registros futuros (RN-026). La actividad
    /// pasa entonces a referenciarla por <c>task_id</c>.
    /// </summary>
    bool SaveTaskToCatalog = false);

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
    FieldUpdate<string> Description,
    /// <summary>
    /// MVP-302 — Guardar en el catálogo la tarea libre de esta actividad. Si no viene
    /// <c>task_text</c> en la petición se usa el que ya tiene la actividad, que es como se promociona
    /// una labor <b>ya registrada</b> sin volver a escribirla (CA-3).
    /// </summary>
    bool SaveTaskToCatalog = false);

/// <summary>
/// Eliminación <b>lógica</b> de una actividad (RN-037). La confirmación explícita es responsabilidad
/// de la UI (MVP-305); aquí llega ya confirmada, pero con la versión vigente (ADR-0005).
/// </summary>
public sealed record DeleteActivityCommand(
    Guid WorkspaceId,
    Guid UserId,
    Guid ActivityId,
    long ExpectedVersion);
