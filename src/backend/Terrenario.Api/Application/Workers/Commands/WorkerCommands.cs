using Terrenario.Api.Common;

namespace Terrenario.Api.Application.Workers.Commands;

/// <summary>
/// Vista de un trabajador para el maestro (MVP-204). La tarifa horaria viaja como referencia; el
/// coste operativo se sigue registrando a mano (RN-003).
/// </summary>
public sealed record WorkerSummary(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    decimal? HourlyRate,
    bool IsActive);

/// <summary>
/// Alta de trabajador (MVP-204). El Workspace nunca viaja como parámetro de negocio: se resuelve en
/// servidor desde el contexto de scope (RN-034, MVP-105).
/// </summary>
public sealed record CreateWorkerCommand(
    Guid WorkspaceId,
    string Name,
    decimal? HourlyRate);

/// <summary>
/// Edición parcial de trabajador (MVP-204, <c>PATCH</c>). Cada campo es un <see cref="FieldUpdate{T}"/>:
/// si no viene en la petición se mantiene el valor actual; si viene (incluido vacío) se asigna o
/// limpia. La inactivación (CA-3) es este mismo comando con <c>IsActive = Set(false)</c>.
/// </summary>
public sealed record UpdateWorkerCommand(
    Guid WorkspaceId,
    Guid WorkerId,
    FieldUpdate<string> Name,
    FieldUpdate<decimal?> HourlyRate,
    FieldUpdate<bool> IsActive);
