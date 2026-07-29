namespace Terrenario.Api.Domain.Activities;

/// <summary>
/// Puerto de persistencia de actividades (MVP-301).
///
/// <b>Todas</b> las lecturas excluyen las actividades eliminadas lógicamente (RN-037): el filtro de
/// baja lógica vive en el puerto, no en un filtro global de EF, siguiendo la misma decisión que tomó
/// <c>IWorkspaceRepository</c> en MVP-206. Así el diario, los listados y —más adelante— el dashboard
/// heredan el comportamiento sin repetirlo.
/// </summary>
public interface IActivityRepository
{
    Task AddAsync(Activity activity, CancellationToken ct = default);

    /// <summary>
    /// Actividad viva por id dentro del Workspace activo. Devuelve <c>null</c> si no existe, si
    /// pertenece a otro Workspace o si ya está eliminada (el borde de transporte responde 404 en los
    /// tres casos, sin revelar recursos ajenos).
    /// </summary>
    Task<Activity?> FindByIdAsync(Guid workspaceId, Guid activityId, CancellationToken ct = default);

    /// <summary>
    /// Actividades vivas del Workspace con sus nombres de terreno, responsable y tarea ya resueltos.
    /// Filtros opcionales alineados con <c>GET /api/v1/activities</c>. Orden: fecha de negocio
    /// descendente (RN-033) y, a igualdad de fecha, por fecha de captura descendente.
    /// </summary>
    Task<IReadOnlyList<ActivityView>> ListAsync(
        Guid workspaceId,
        ActivityFilter filter,
        CancellationToken ct = default);

    /// <summary>Misma proyección que el listado, para un único registro (respuestas de alta y edición).</summary>
    Task<ActivityView?> GetViewAsync(Guid workspaceId, Guid activityId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Filtros del listado de actividades (<c>from</c>, <c>to</c>, terreno, temporada, responsable).</summary>
public sealed record ActivityFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? PlotId = null,
    Guid? SeasonId = null,
    Guid? WorkerId = null);

/// <summary>
/// Vista de lectura de una actividad con los datos que el diario necesita mostrar sin pedir los
/// maestros por separado. Incluye el rango de la temporada para poder señalar la fecha fuera de rango
/// (RN-023) sin una consulta adicional del cliente.
/// </summary>
public sealed record ActivityView(
    Guid Id,
    Guid WorkspaceId,
    Guid PlotId,
    string PlotName,
    Guid SeasonId,
    string SeasonName,
    DateOnly SeasonStartDate,
    DateOnly? SeasonEndDate,
    Guid WorkerId,
    string WorkerName,
    DateOnly Date,
    decimal Hours,
    Guid? TaskId,
    string? TaskName,
    string? TaskText,
    decimal ManualCost,
    string? Description,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Texto de la tarea, venga del catálogo o del campo libre (RN-025). El diario no distingue: para
    /// quien lee, una labor es una labor.
    /// </summary>
    public string Task => TaskName ?? TaskText ?? string.Empty;

    /// <summary>
    /// RN-023 — la fecha cae fuera del rango de la temporada asociada. Es un <b>aviso</b>, nunca un
    /// bloqueo: se calcula en lectura para que la UI pueda marcarlo también en registros antiguos,
    /// aunque la temporada se haya editado después.
    /// </summary>
    public bool IsOutOfSeasonRange =>
        Date < SeasonStartDate || (SeasonEndDate is { } end && Date > end);
}
