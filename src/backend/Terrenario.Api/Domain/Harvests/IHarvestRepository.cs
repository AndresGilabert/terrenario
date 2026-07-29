namespace Terrenario.Api.Domain.Harvests;

/// <summary>
/// Puerto de persistencia de cosechas (MVP-401).
///
/// <b>Todas</b> las lecturas excluyen las cosechas eliminadas lógicamente (RN-037): el filtro de baja
/// lógica vive en el puerto, no en un filtro global de EF, siguiendo la misma decisión que tomaron
/// <c>IWorkspaceRepository</c> (MVP-206) y <c>IActivityRepository</c> (MVP-301). Así el listado, el
/// diario y el dashboard heredan el comportamiento sin repetirlo.
/// </summary>
public interface IHarvestRepository
{
    Task AddAsync(Harvest harvest, CancellationToken ct = default);

    /// <summary>
    /// Cosecha viva por id dentro del Workspace activo. Devuelve <c>null</c> si no existe, si
    /// pertenece a otro Workspace o si ya está eliminada (el borde de transporte responde 404 en los
    /// tres casos, sin revelar recursos ajenos).
    /// </summary>
    Task<Harvest?> FindByIdAsync(Guid workspaceId, Guid harvestId, CancellationToken ct = default);

    /// <summary>
    /// Cosechas vivas del Workspace con el terreno y la temporada ya resueltos. Filtros alineados con
    /// <c>GET /api/v1/harvests</c>. Orden: fecha de negocio descendente (RN-033) y, a igualdad de
    /// fecha, por fecha de captura descendente.
    /// </summary>
    Task<IReadOnlyList<HarvestView>> ListAsync(
        Guid workspaceId,
        HarvestFilter filter,
        CancellationToken ct = default);

    /// <summary>Misma proyección que el listado, para un único registro (respuestas de alta y edición).</summary>
    Task<HarvestView?> GetViewAsync(Guid workspaceId, Guid harvestId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Filtros del listado de cosechas (<c>from</c>, <c>to</c>, terreno, temporada, destino).</summary>
public sealed record HarvestFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? PlotId = null,
    Guid? SeasonId = null,
    string? Destination = null);

/// <summary>
/// Vista de lectura de una cosecha con los nombres de terreno y temporada resueltos, para que el
/// listado y el diario no tengan que pedir los maestros por separado. Incluye el rango de la temporada
/// para poder señalar la fecha fuera de rango (RN-023) sin una consulta adicional del cliente.
/// </summary>
public sealed record HarvestView(
    Guid Id,
    Guid WorkspaceId,
    Guid PlotId,
    string PlotName,
    Guid SeasonId,
    string SeasonName,
    DateOnly SeasonStartDate,
    DateOnly? SeasonEndDate,
    DateOnly Date,
    string Product,
    decimal Kgs,
    decimal? Yield,
    decimal? Liters,
    string Destination,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// RN-023 — la fecha cae fuera del rango de la temporada asociada. Es un <b>aviso</b>, nunca un
    /// bloqueo: se calcula en lectura para que la UI pueda marcarlo también en registros antiguos,
    /// aunque la temporada se haya editado después.
    /// </summary>
    public bool IsOutOfSeasonRange =>
        Date < SeasonStartDate || (SeasonEndDate is { } end && Date > end);
}
