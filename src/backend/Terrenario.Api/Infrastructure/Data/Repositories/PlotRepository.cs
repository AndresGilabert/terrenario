using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Plots;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

public sealed class PlotRepository(TerrenarioDbContext db) : IPlotRepository
{
    public async Task AddAsync(Plot plot, CancellationToken ct = default)
        => await db.Plots.AddAsync(plot, ct);

    public Task<Plot?> FindByIdAsync(Guid workspaceId, Guid plotId, CancellationToken ct = default)
        => db.Plots
            .FirstOrDefaultAsync(p => p.Id == plotId && p.WorkspaceId == workspaceId, ct);

    public async Task<IReadOnlyList<Plot>> ListByWorkspaceAsync(
        Guid workspaceId,
        string? search,
        bool? isActive,
        CancellationToken ct = default)
    {
        var query = db.Plots.Where(p => p.WorkspaceId == workspaceId);

        if (isActive is { } active)
            query = query.Where(p => p.IsActive == active);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.Alias != null && p.Alias.ToLower().Contains(term)) ||
                (p.Location != null && p.Location.ToLower().Contains(term)));
        }

        // Orden estable para el maestro: activos primero y luego por nombre. Se ordena por columnas
        // reales antes de proyectar para que EF lo traduzca a SQL (lección de P-014).
        return await query
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
