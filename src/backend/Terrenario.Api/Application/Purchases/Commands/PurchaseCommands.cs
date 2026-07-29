using Terrenario.Api.Common;

namespace Terrenario.Api.Application.Purchases.Commands;

/// <summary>
/// Alta de compra (MVP-303, HU-1). El Workspace y el usuario se resuelven en servidor desde el
/// contexto de scope y el claim de la sesión (RN-034).
/// </summary>
public sealed record CreatePurchaseCommand(
    Guid WorkspaceId,
    Guid UserId,
    Guid SeasonId,
    DateOnly PurchaseDate,
    string Product,
    decimal TotalQuantity,
    decimal TotalCost);

/// <summary>
/// Edición parcial de compra (<c>PATCH</c>). Cada campo es un <see cref="FieldUpdate{T}"/>: ausente
/// conserva el valor actual. <see cref="ExpectedVersion"/> llega de <c>If-Match</c> (ADR-0005).
/// </summary>
public sealed record UpdatePurchaseCommand(
    Guid WorkspaceId,
    Guid UserId,
    Guid PurchaseId,
    long ExpectedVersion,
    FieldUpdate<Guid> SeasonId,
    FieldUpdate<DateOnly> PurchaseDate,
    FieldUpdate<string> Product,
    FieldUpdate<decimal> TotalQuantity,
    FieldUpdate<decimal> TotalCost);

/// <summary>Eliminación <b>lógica</b> de una compra (RN-037), con la versión vigente (ADR-0005).</summary>
public sealed record DeletePurchaseCommand(
    Guid WorkspaceId,
    Guid UserId,
    Guid PurchaseId,
    long ExpectedVersion);
