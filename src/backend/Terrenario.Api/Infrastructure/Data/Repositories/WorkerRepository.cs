using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

public sealed class WorkerRepository(TerrenarioDbContext db) : IWorkerRepository
{
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

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
