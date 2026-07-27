using Terrenario.Api.Domain.Plots;

namespace Terrenario.Api.Application.Plots.Commands;

/// <summary>
/// Vista de un terreno que el cliente necesita para el maestro (MVP-202). Incluye el estado de
/// actividad y la señal <see cref="HasTreeCount"/> para que la UI marque el dato incompleto de número
/// de árboles (RN-010/RN-028) sin bloquear nada.
/// </summary>
public sealed record PlotSummary(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string OwnershipType,
    string? Alias,
    string? OwnerName,
    string? CadastralReference,
    string? Location,
    int? TreeCount,
    bool IsActive)
{
    public bool HasTreeCount => TreeCount is not null;
}

/// <summary>
/// Alta de terreno (MVP-202). El Workspace nunca viaja como parámetro de negocio: se resuelve en
/// servidor desde el contexto de scope (RN-034, MVP-105).
/// </summary>
public sealed record CreatePlotCommand(
    Guid WorkspaceId,
    string Name,
    string OwnershipType,
    string? Alias,
    string? OwnerName,
    string? CadastralReference,
    string? Location,
    int? TreeCount);

/// <summary>
/// Edición parcial de terreno (MVP-202, <c>PATCH</c>). Cada campo es un <see cref="FieldUpdate{T}"/>:
/// si no viene en la petición se mantiene el valor actual; si viene (incluido vacío) se asigna o
/// limpia. La inactivación (CA-3) es este mismo comando con <c>IsActive = Set(false)</c>, sin
/// necesidad de reenviar el resto de campos (por eso el PATCH parcial evita perder datos).
/// </summary>
public sealed record UpdatePlotCommand(
    Guid WorkspaceId,
    Guid PlotId,
    FieldUpdate<string> Name,
    FieldUpdate<string> OwnershipType,
    FieldUpdate<string?> Alias,
    FieldUpdate<string?> OwnerName,
    FieldUpdate<string?> CadastralReference,
    FieldUpdate<string?> Location,
    FieldUpdate<int?> TreeCount,
    FieldUpdate<bool> IsActive);
