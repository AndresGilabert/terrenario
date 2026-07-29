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

    /// <summary>
    /// Productos ya usados en el Workspace, para las sugerencias de captura (RN-031, HU-2). Devuelve
    /// los más frecuentes primero, filtrando opcionalmente por un fragmento de texto. No es un
    /// catálogo: es vocabulario aprendido del histórico y el usuario puede ignorarlo.
    /// </summary>
    Task<IReadOnlyList<ProductSuggestion>> ListProductSuggestionsAsync(
        Guid workspaceId,
        string? search,
        int limit,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Filtros del listado de compras (<c>product</c>, <c>season_id</c> y rango de fechas).</summary>
public sealed record PurchaseFilter(
    string? Product = null,
    Guid? SeasonId = null,
    DateOnly? From = null,
    DateOnly? To = null);

/// <summary>Producto del histórico con cuántas veces se ha comprado (RN-031).</summary>
public sealed record ProductSuggestion(string Product, int TimesUsed);

/// <summary>
/// Vista de lectura de una compra con el nombre de su temporada y el aviso de fecha fuera de rango
/// resueltos, para que el libro de compras y el diario no tengan que pedir el maestro.
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
    DateTimeOffset UpdatedAt)
{
    /// <summary>RN-023 — aviso no bloqueante de fecha fuera del rango de la temporada, igual que en la actividad.</summary>
    public bool IsOutOfSeasonRange =>
        PurchaseDate < SeasonStartDate || (SeasonEndDate is { } end && PurchaseDate > end);
}
