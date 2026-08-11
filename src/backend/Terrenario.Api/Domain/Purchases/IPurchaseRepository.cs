using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Domain.Purchases;

/// <summary>
/// Puerto de persistencia de compras (MVP-303). Como en actividades, <b>todas</b> las lecturas
/// excluyen las eliminadas lógicamente (RN-037): el filtro de baja lógica vive en el puerto, no en un
/// filtro global de EF.
/// </summary>
public interface IPurchaseRepository
{
    Task AddAsync(Purchase purchase, CancellationToken ct = default);

    /// <summary>
    /// Compra viva por id dentro del Workspace activo. <c>null</c> si no existe, es de otro Workspace
    /// o ya está eliminada (404 en los tres casos).
    /// </summary>
    Task<Purchase?> FindByIdAsync(Guid workspaceId, Guid purchaseId, CancellationToken ct = default);

    /// <summary>
    /// Compras vivas del Workspace con el nombre y el rango de su temporada ya resueltos. Orden: fecha
    /// de compra descendente (RN-033) y, a igualdad, fecha de captura descendente.
    /// </summary>
    Task<IReadOnlyList<PurchaseView>> ListAsync(
        Guid workspaceId,
        PurchaseFilter filter,
        CancellationToken ct = default);

    /// <summary>Misma proyección que el listado, para un único registro (respuestas de alta y edición).</summary>
    Task<PurchaseView?> GetViewAsync(Guid workspaceId, Guid purchaseId, CancellationToken ct = default);

    // MVP-708 (`P-057`) — Las sugerencias de material ya no cuelgan de este puerto: se aprenden de
    // compras **y** de consumos sin compra previa, así que viven en `IMaterialRepository`.

    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Filtros del listado de compras (<c>product</c>, <c>season_id</c> y rango de fechas).</summary>
public sealed record PurchaseFilter(
    string? Product = null,
    Guid? SeasonId = null,
    DateOnly? From = null,
    DateOnly? To = null);

/// <summary>
/// Vista de lectura de una compra con el nombre de su temporada y el aviso de fecha fuera de rango
/// resueltos, para que el libro de compras y el diario no tengan que pedir el maestro.
///
/// MVP-804 — Resuelve también <b>quién</b> apuntó el gasto y quién lo corrigió por última vez
/// (<see cref="IAuthoredRecord"/>). La compra no tiene lectura por id: el modal de corrección se abre
/// con la fila del listado, así que la autoría tiene que venir en esta proyección o no llega.
/// </summary>
public sealed record PurchaseView(
    Guid Id,
    Guid WorkspaceId,
    Guid SeasonId,
    string SeasonName,
    DateOnly SeasonStartDate,
    DateOnly? SeasonEndDate,
    DateOnly PurchaseDate,
    string Product,
    decimal TotalQuantity,
    decimal TotalCost,
    decimal UnitPrice,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    /// <summary>MVP-804 — Nombre de quien apuntó la compra; <c>null</c> si su cuenta ya no nombra a nadie.</summary>
    string? CreatedByAccountName = null,
    /// <summary>MVP-804 — Nombre de quien hizo la última corrección, con el mismo criterio.</summary>
    string? UpdatedByAccountName = null) : IAuthoredRecord
{
    /// <summary>RN-023 — aviso no bloqueante de fecha fuera del rango de la temporada, igual que en la actividad.</summary>
    public bool IsOutOfSeasonRange =>
        PurchaseDate < SeasonStartDate || (SeasonEndDate is { } end && PurchaseDate > end);
}
