using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Tasks;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

public sealed class TaskRepository(TerrenarioDbContext db) : ITaskRepository
{
    /// <summary>
    /// Índice único (workspace_id, lower(name)) creado en la migración <c>AddTasks</c>. Es la
    /// invariante de base de datos que respalda la guarda de duplicados del catálogo (MVP-205).
    /// </summary>
    public const string UniqueNameIndexName = "ux_tasks_workspace_name";

    public async Task AddAsync(TaskItem task, CancellationToken ct = default)
        => await db.Tasks.AddAsync(task, ct);

    public Task<TaskItem?> FindByIdAsync(Guid workspaceId, Guid taskId, CancellationToken ct = default)
        => db.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.WorkspaceId == workspaceId, ct);

    public async Task<IReadOnlyList<TaskItem>> ListByWorkspaceAsync(
        Guid workspaceId,
        bool? isActive,
        CancellationToken ct = default)
    {
        var query = db.Tasks.Where(t => t.WorkspaceId == workspaceId);

        if (isActive is { } active)
            query = query.Where(t => t.IsActive == active);

        // Orden estable del catálogo: activas primero y luego por nombre. Se ordena por columnas
        // reales antes de proyectar para que EF lo traduzca a SQL (lección de P-014).
        return await query
            .OrderByDescending(t => t.IsActive)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);
    }

    public Task<bool> ExistsWithNameAsync(
        Guid workspaceId,
        string name,
        Guid? excludeTaskId = null,
        CancellationToken ct = default)
    {
        // Comparación insensible a mayúsculas con `ToLower()`, que tanto Npgsql como SQLite traducen
        // a `lower(...)`: es el mismo criterio del índice único `ux_tasks_workspace_name`, así que la
        // guarda de aplicación y la invariante de base de datos no pueden discrepar.
        var normalized = name.ToLower();

        var query = db.Tasks.Where(t => t.WorkspaceId == workspaceId && t.Name.ToLower() == normalized);

        if (excludeTaskId is { } excluded)
            query = query.Where(t => t.Id != excluded);

        return query.AnyAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsDuplicateTaskName(ex))
        {
            // Dos altas simultáneas con el mismo nombre pasan la guarda de aplicación y chocan aquí:
            // se traduce a la misma respuesta 409 en lugar de a un 500.
            throw new TaskConflictException(
                ErrorCodes.ConflictTaskNameDuplicate,
                "Ya existe una tarea con ese nombre en el catálogo de este Workspace.");
        }
    }

    private static bool IsDuplicateTaskName(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
           && pg.ConstraintName == UniqueNameIndexName;
}
