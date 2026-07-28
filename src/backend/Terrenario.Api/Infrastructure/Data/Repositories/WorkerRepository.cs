using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

public sealed class WorkerRepository(TerrenarioDbContext db) : IWorkerRepository
{
    /// <summary>
    /// Índice único (workspace_id, lower(name)) creado en la migración
    /// <c>AddMasterNameUniqueIndexes</c>. Es la invariante de base de datos que respalda la guarda de
    /// duplicados del maestro (MVP-207, CA-3).
    /// </summary>
    public const string UniqueNameIndexName = "ux_workers_workspace_name";

    public async Task AddAsync(Worker worker, CancellationToken ct = default)
        => await db.Workers.AddAsync(worker, ct);

    public Task<Worker?> FindByIdAsync(Guid workspaceId, Guid workerId, CancellationToken ct = default)
        => db.Workers
            .FirstOrDefaultAsync(w => w.Id == workerId && w.WorkspaceId == workspaceId, ct);

    public async Task<IReadOnlyList<Worker>> ListByWorkspaceAsync(
        Guid workspaceId,
        bool? isActive,
        CancellationToken ct = default)
    {
        var query = db.Workers.Where(w => w.WorkspaceId == workspaceId);

        if (isActive is { } active)
            query = query.Where(w => w.IsActive == active);

        // Orden estable para el maestro: activos primero y luego por nombre. Se ordena por columnas
        // reales antes de proyectar para que EF lo traduzca a SQL (lección de P-014).
        return await query
            .OrderByDescending(w => w.IsActive)
            .ThenBy(w => w.Name)
            .ToListAsync(ct);
    }

    public Task<bool> ExistsWithNameAsync(
        Guid workspaceId,
        string name,
        Guid? excludeWorkerId = null,
        CancellationToken ct = default)
    {
        // Comparación insensible a mayúsculas con `ToLower()`, que tanto Npgsql como SQLite traducen
        // a `lower(...)`: es el mismo criterio del índice único `ux_workers_workspace_name`, así que
        // la guarda de aplicación y la invariante de base de datos no pueden discrepar.
        var normalized = name.ToLower();

        var query = db.Workers.Where(w => w.WorkspaceId == workspaceId && w.Name.ToLower() == normalized);

        if (excludeWorkerId is { } excluded)
            query = query.Where(w => w.Id != excluded);

        return query.AnyAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsDuplicateWorkerName(ex))
        {
            // Dos altas simultáneas con el mismo nombre pasan la guarda de aplicación y chocan aquí:
            // se traduce a la misma respuesta 409 en lugar de a un 500.
            throw new WorkerConflictException(
                ErrorCodes.ConflictWorkerNameDuplicate,
                "Ya existe un trabajador con ese nombre en este Workspace.");
        }
    }

    private static bool IsDuplicateWorkerName(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
           && pg.ConstraintName == UniqueNameIndexName;
}
