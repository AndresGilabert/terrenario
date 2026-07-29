using Terrenario.Api.Common;

namespace Terrenario.Api.Application.Consumptions.Commands;

/// <summary>
/// Imputación de una compra a un terreno (MVP-304, HU-1). El producto, la temporada y el precio
/// unitario <b>no viajan</b>: se heredan de la compra.
/// </summary>
public sealed record ImputePurchaseCommand(
    Guid WorkspaceId,
    Guid UserId,
    Guid PurchaseId,
    Guid PlotId,
    DateOnly Date,
    decimal Quantity);

/// <summary>
/// Consumo <b>sin compra previa</b> (MVP-304, HU-2, RN-032). Aquí sí hacen falta producto y
/// temporada, porque no hay compra de la que heredarlos, y el coste imputado será <c>0</c>.
/// </summary>
public sealed record RegisterConsumptionCommand(
    Guid WorkspaceId,
    Guid UserId,
    Guid SeasonId,
    Guid PlotId,
    DateOnly Date,
    string Product,
    decimal Quantity);

/// <summary>
/// Edición parcial de un consumo (<c>PATCH</c>). El precio unitario no es editable: es el que se
/// congeló al imputar (RN-032). <see cref="ExpectedVersion"/> llega de <c>If-Match</c> (ADR-0005).
/// </summary>
public sealed record UpdateConsumptionCommand(
    Guid WorkspaceId,
    Guid UserId,
    Guid ConsumptionId,
    long ExpectedVersion,
    FieldUpdate<Guid> SeasonId,
    FieldUpdate<Guid> PlotId,
    FieldUpdate<DateOnly> Date,
    FieldUpdate<string> Product,
    FieldUpdate<decimal> Quantity);

/// <summary>Eliminación <b>lógica</b> de un consumo (RN-037), con la versión vigente (ADR-0005).</summary>
public sealed record DeleteConsumptionCommand(
    Guid WorkspaceId,
    Guid UserId,
    Guid ConsumptionId,
    long ExpectedVersion);
