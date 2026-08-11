using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Domain.Consumptions;

/// <summary>
/// Puerto de persistencia de consumos e imputaciones (MVP-304). Como en actividades y compras,
/// <b>todas</b> las lecturas excluyen los eliminados lógicamente (RN-037).
/// </summary>
public interface IConsumptionRepository
{
    Task AddAsync(PurchaseConsumption consumption, CancellationToken ct = default);

    /// <summary>Consumo vivo por id dentro del Workspace activo. <c>null</c> si no procede (404).</summary>
    Task<PurchaseConsumption?> FindByIdAsync(
        Guid workspaceId,
        Guid consumptionId,
        CancellationToken ct = default);

    /// <summary>
    /// Consumos vivos del Workspace con el terreno y la temporada resueltos. Orden: fecha de negocio
    /// descendente (RN-033, CA-4), no fecha de captura.
    /// </summary>
    Task<IReadOnlyList<ConsumptionView>> ListAsync(
        Guid workspaceId,
        ConsumptionFilter filter,
        CancellationToken ct = default);

    /// <summary>Misma proyección, para un único registro (respuestas de alta y edición).</summary>
    Task<ConsumptionView?> GetViewAsync(Guid workspaceId, Guid consumptionId, CancellationToken ct = default);

    /// <summary>
    /// Cantidad ya imputada (viva) de una compra, para la guarda de sobre-imputación
    /// (<c>VALIDATION_CONSUMPTION_OVERFLOW</c>). <paramref name="excludeConsumptionId"/> permite
    /// excluir la propia imputación al corregir su cantidad.
    /// </summary>
    Task<decimal> SumImputedQuantityAsync(
        Guid workspaceId,
        Guid purchaseId,
        Guid? excludeConsumptionId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Cantidad imputada viva por compra, para el conjunto de compras indicado. Lo usa el libro de
    /// compras para mostrar «imputado / total» sin una consulta por fila.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> SumImputedQuantityByPurchaseAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> purchaseIds,
        CancellationToken ct = default);

    /// <summary>
    /// ¿Le queda alguna imputación viva a esta compra? Es la guarda que impide dar de baja una compra
    /// dejando huérfanos registros operativos que sí están en el diario (ver <c>MVP-304</c>).
    /// </summary>
    Task<int> CountLiveByPurchaseAsync(Guid workspaceId, Guid purchaseId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Filtros de <c>GET /api/v1/consumptions</c>.</summary>
public sealed record ConsumptionFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? PlotId = null,
    Guid? SeasonId = null,
    Guid? PurchaseId = null,
    /// <summary>
    /// Búsqueda parcial e insensible a mayúsculas sobre el material, igual que en compras. Añadido en
    /// <c>MVP-399</c> (hallazgo <c>R-06</c>): el buscador del libro filtraba las compras pero no los
    /// consumos, así que buscar «gasóleo» dejaba a la vista consumos de cualquier otra cosa.
    /// </summary>
    string? Product = null);

/// <summary>
/// Vista de lectura de un consumo con el terreno y la temporada resueltos y el aviso de fecha fuera
/// de rango derivado, para que el libro y el diario no pidan los maestros por separado.
///
/// MVP-804 — Resuelve también <b>quién</b> apuntó el consumo y quién lo corrigió por última vez
/// (<see cref="IAuthoredRecord"/>). Como la compra, no tiene lectura por id: el modal se abre con la
/// fila del listado.
/// </summary>
public sealed record ConsumptionView(
    Guid Id,
    Guid WorkspaceId,
    Guid? PurchaseId,
    Guid PlotId,
    string PlotName,
    Guid SeasonId,
    string SeasonName,
    DateOnly SeasonStartDate,
    DateOnly? SeasonEndDate,
    DateOnly Date,
    string Product,
    decimal ConsumedQuantity,
    decimal UnitPrice,
    decimal ProportionalCost,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    /// <summary>
    /// MVP-708 (<c>P-058</c>) — Fecha de la compra imputada; <c>null</c> en un consumo sin compra
    /// previa. Es lo único que se lee de la compra, y solo para poder avisar de un consumo anterior
    /// a ella (RN-043). El coste y el material siguen siendo los del propio consumo (RN-032).
    /// </summary>
    DateOnly? PurchaseDate = null,
    /// <summary>MVP-804 — Nombre de quien apuntó el consumo; <c>null</c> si su cuenta ya no nombra a nadie.</summary>
    string? CreatedByAccountName = null,
    /// <summary>MVP-804 — Nombre de quien hizo la última corrección, con el mismo criterio.</summary>
    string? UpdatedByAccountName = null) : IAuthoredRecord
{
    /// <summary>
    /// <c>false</c> cuando el consumo se registró sin compra previa (RN-032): el coste es <c>0</c>
    /// porque se desconoce, no porque fuera gratis. Es la señal con la que la UI avisa (CA-2).
    /// </summary>
    public bool HasPurchase => PurchaseId is not null;

    /// <summary>RN-023 — aviso no bloqueante de fecha fuera del rango de la temporada.</summary>
    public bool IsOutOfSeasonRange =>
        Date < SeasonStartDate || (SeasonEndDate is { } end && Date > end);

    /// <summary>
    /// RN-043 (MVP-708, <c>P-058</c>) — Consumo con fecha <b>anterior</b> a la de su compra. No
    /// bloquea: la captura retroactiva es legítima y RN-032 ya asume que el papeleo va por detrás del
    /// campo. Pero gastar algo antes de comprarlo es casi siempre un error de tecleo en la fecha, así
    /// que se señala igual que RN-023 señala la fecha fuera de temporada.
    ///
    /// Un consumo sin compra previa nunca lo activa: sin compra no hay fecha contra la que comparar.
    /// </summary>
    public bool IsBeforePurchaseDate => PurchaseDate is { } purchased && Date < purchased;
}
