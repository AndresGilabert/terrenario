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

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
