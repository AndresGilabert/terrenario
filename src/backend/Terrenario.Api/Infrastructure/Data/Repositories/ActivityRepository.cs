using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Activities;
using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Adaptador EF Core del diario de actividades (MVP-301).
///
/// Dos decisiones que se repetirán en el resto de entidades operativas de la épica:
/// <list type="bullet">
/// <item><b>La baja lógica se filtra aquí</b> (<c>deleted_at IS NULL</c>), no con un filtro global de
/// EF: mismo criterio que <c>WorkspaceRepository</c> en MVP-206. Quien quiera ver lo eliminado tendrá
/// que pedirlo explícitamente el día que exista una política de retención (<c>P-033</c>).</item>
/// <item><b>La colisión de versión de la base de datos se traduce a 409</b>, no a 500: el
/// <c>EnsureVersion</c> del dominio cubre el caso normal, pero dos escrituras simultáneas con la
/// misma versión de partida solo las separa el token de concurrencia de EF.</item>
/// </list>
/// </summary>
public sealed class ActivityRepository(TerrenarioDbContext db) : IActivityRepository
{
    public async Task AddAsync(Activity activity, CancellationToken ct = default)
        => await db.Activities.AddAsync(activity, ct);

    public Task<Activity?> FindByIdAsync(Guid workspaceId, Guid activityId, CancellationToken ct = default)
        => db.Activities.FirstOrDefaultAsync(
            a => a.Id == activityId && a.WorkspaceId == workspaceId && a.DeletedAt == null, ct);

    public async Task<IReadOnlyList<ActivityView>> ListAsync(
        Guid workspaceId,
        ActivityFilter filter,
        CancellationToken ct = default)
    {
        var live = LiveActivities(workspaceId);

        if (filter.From is { } from) live = live.Where(a => a.Date >= from);
        if (filter.To is { } to) live = live.Where(a => a.Date <= to);
        if (filter.PlotId is { } plotId) live = live.Where(a => a.PlotId == plotId);
        if (filter.SeasonId is { } seasonId) live = live.Where(a => a.SeasonId == seasonId);
        if (filter.WorkerId is { } workerId) live = live.Where(a => a.WorkerId == workerId);

        // Filtros y orden se aplican sobre columnas reales **antes** de proyectar, que es lo que EF
        // sabe traducir (lección de P-014): sobre el registro ya proyectado, `OrderBy(v => v.Date)`
        // no es traducible porque `ActivityView` es un tipo posicional, no una entidad mapeada.
        // El desempate por fecha de captura va también en SQL desde MVP-501. Antes se reaplicaba en
        // memoria porque EF+SQLite no traduce `ORDER BY` sobre `DateTimeOffset` y el arnés corría
        // sobre SQLite; con PostgreSQL real esa presión desaparece (P-031).
        return await ProjectViews(
                live.OrderByDescending(a => a.Date).ThenByDescending(a => a.CreatedAt))
            .ToListAsync(ct);
    }

    public Task<ActivityView?> GetViewAsync(Guid workspaceId, Guid activityId, CancellationToken ct = default)
        => ProjectViews(LiveActivities(workspaceId).Where(a => a.Id == activityId)).FirstOrDefaultAsync(ct);

    /// <summary>Actividades vivas del Workspace: el filtro de baja lógica en un único sitio (RN-037).</summary>
    private IQueryable<Activity> LiveActivities(Guid workspaceId)
        => db.Activities.Where(a => a.WorkspaceId == workspaceId && a.DeletedAt == null);

    /// <summary>
    /// Proyección de lectura del diario: resuelve en una sola consulta el nombre del terreno, del
    /// responsable y de la tarea, más el rango de la temporada que necesita el aviso de RN-023. La
    /// tarea entra con <c>LEFT JOIN</c> porque puede ser texto libre y no existir en el catálogo
    /// (RN-025).
    /// </summary>
    private IQueryable<ActivityView> ProjectViews(IQueryable<Activity> activities)
        => from a in activities
           join p in db.Plots on a.PlotId equals p.Id
           join s in db.Seasons on a.SeasonId equals s.Id
           join w in db.Workers on a.WorkerId equals w.Id
           join t in db.Tasks on a.TaskId equals t.Id into taskMatches
           from t in taskMatches.DefaultIfEmpty()
           select new ActivityView(
               a.Id,
               a.WorkspaceId,
               a.PlotId,
               p.Name,
               a.SeasonId,
               s.Name,
               s.StartDate,
               s.EndDate,
               a.WorkerId,
               w.Name,
               a.Date,
               a.Hours,
               a.TaskId,
               t != null ? t.Name : null,
               a.TaskText,
               a.ManualCost,
               a.Description,
               a.Version,
               a.CreatedAt,
               a.UpdatedAt);

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Dos escrituras simultáneas partiendo de la misma versión: la primera gana y la segunda
            // llega aquí. Se responde el mismo 409 que la guarda de aplicación (ADR-0005), no un 500.
            throw new ConcurrencyConflictException(
                "Otra persona ha modificado este registro mientras lo editabas. Refresca para ver la versión actual.");
        }
    }
}
