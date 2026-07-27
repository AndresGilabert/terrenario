using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

public sealed class SeasonRepository(TerrenarioDbContext db) : ISeasonRepository
{
    public async Task AddAsync(Season season, CancellationToken ct = default)
        => await db.Seasons.AddAsync(season, ct);

    public Task<Season?> FindActiveByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
        => db.Seasons
            .Where(s => s.WorkspaceId == workspaceId && s.IsActive)
            .FirstOrDefaultAsync(ct);

    public Task<Season?> FindByIdAsync(Guid workspaceId, Guid seasonId, CancellationToken ct = default)
        => db.Seasons
            .FirstOrDefaultAsync(s => s.Id == seasonId && s.WorkspaceId == workspaceId, ct);

    public async Task<IReadOnlyList<Season>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
    {
        // Orden del maestro: la activa arriba, después las planificadas/cerradas por fecha de inicio
        // descendente (la campaña más reciente primero). Se ordena por columnas reales para que EF lo
        // traduzca a SQL (lección de P-014).
        return await db.Seasons
            .Where(s => s.WorkspaceId == workspaceId)
            .OrderByDescending(s => s.IsActive)
            .ThenBy(s => s.IsClosed)
            .ThenByDescending(s => s.StartDate)
            .ToListAsync(ct);
    }

    public async Task ActivateExclusivelyAsync(Season season, bool isNew, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        // No hay estrategia de reintento configurada (Program.cs), así que basta una transacción
        // explícita. Fase 1: desactivar cualquier otra activa con un UPDATE directo (inmediato, no
        // pasa por el rastreador) para que, al llegar la fase 2, no exista ninguna otra activa y el
        // índice único parcial no se viole ni transitoriamente.
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.Seasons
            .Where(s => s.WorkspaceId == season.WorkspaceId && s.Id != season.Id && s.IsActive)
            .ExecuteUpdateAsync(
                set => set
                    .SetProperty(s => s.IsActive, false)
                    .SetProperty(s => s.UpdatedAt, now),
                ct);

        // Fase 2: activar la temporada objetivo (nueva o existente ya marcada activa por el dominio).
        if (isNew)
            await db.Seasons.AddAsync(season, ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
