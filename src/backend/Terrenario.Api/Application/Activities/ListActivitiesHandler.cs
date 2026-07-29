using Terrenario.Api.Domain.Activities;

namespace Terrenario.Api.Application.Activities;

/// <summary>
/// MVP-301 — Lista las actividades vivas del Workspace activo con los filtros de
/// <c>GET /api/v1/activities</c> (<c>from</c>, <c>to</c>, <c>plot_id</c>, <c>season_id</c>,
/// <c>worker_id</c>). Las eliminadas lógicamente no salen (RN-037): el filtro vive en el repositorio.
///
/// El orden es por <b>fecha de negocio</b> descendente, no por fecha de captura: es lo que pide el
/// diario (RN-033) y lo que hace útil el listado para revisar lo que se hizo, no lo que se apuntó.
/// </summary>
public sealed class ListActivitiesHandler(IActivityRepository activityRepository)
{
    public Task<IReadOnlyList<ActivityView>> HandleAsync(
        Guid workspaceId,
        ActivityFilter filter,
        CancellationToken ct = default)
        => activityRepository.ListAsync(workspaceId, filter, ct);
}

/// <summary>
/// MVP-305 — Una actividad viva concreta del Workspace activo. Lo necesita el diario unificado para
/// abrir el formulario de corrección: la entrada del diario es una proyección común de los tres
/// tipos y no lleva todos los campos de la actividad.
/// </summary>
public sealed class GetActivityHandler(IActivityRepository activityRepository)
{
    /// <returns><c>null</c> si no existe, es de otro Workspace o ya fue eliminada (404).</returns>
    public Task<ActivityView?> HandleAsync(Guid workspaceId, Guid activityId, CancellationToken ct = default)
        => activityRepository.GetViewAsync(workspaceId, activityId, ct);
}
