using Terrenario.Api.Common;

namespace Terrenario.Api.Application.Tasks.Commands;

/// <summary>Vista de una tarea del catálogo del Workspace (MVP-205).</summary>
public sealed record TaskSummary(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    bool IsActive);

/// <summary>
/// Alta de tarea en el catálogo (MVP-205). El Workspace nunca viaja como parámetro de negocio: se
/// resuelve en servidor desde el contexto de scope (RN-034, MVP-105).
/// </summary>
public sealed record CreateTaskCommand(
    Guid WorkspaceId,
    string Name,
    bool? IsActive);

/// <summary>
/// Edición parcial de tarea (MVP-205, <c>PATCH</c>). Cada campo es un <see cref="FieldUpdate{T}"/>:
/// si no viene en la petición se mantiene el valor actual; si viene, se asigna. La inactivación
/// (CA-3) es este mismo comando con <c>IsActive = Set(false)</c>.
/// </summary>
public sealed record UpdateTaskCommand(
    Guid WorkspaceId,
    Guid TaskId,
    FieldUpdate<string> Name,
    FieldUpdate<bool> IsActive);
