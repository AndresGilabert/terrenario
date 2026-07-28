using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Plots;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

public sealed class PlotRepository(TerrenarioDbContext db) : IPlotRepository
{
    /// <summary>
    /// Índice único (workspace_id, lower(name)) creado en la migración
    /// <c>AddMasterNameUniqueIndexes</c>. Es la invariante de base de datos que respalda la guarda de
    /// duplicados del maestro (MVP-207, CA-3).
    /// </summary>
    public const string UniqueNameIndexName = "ux_plots_workspace_name";

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

    public Task<bool> ExistsWithNameAsync(
        Guid workspaceId,
        string name,
        Guid? excludePlotId = null,
        CancellationToken ct = default)
    {
        // Comparación insensible a mayúsculas con `ToLower()`, que tanto Npgsql como SQLite traducen
        // a `lower(...)`: es el mismo criterio del índice único `ux_plots_workspace_name`, así que la
        // guarda de aplicación y la invariante de base de datos no pueden discrepar.
        var normalized = name.ToLower();

        var query = db.Plots.Where(p => p.WorkspaceId == workspaceId && p.Name.ToLower() == normalized);

        if (excludePlotId is { } excluded)
            query = query.Where(p => p.Id != excluded);

        return query.AnyAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsDuplicatePlotName(ex))
        {
            // Dos altas simultáneas con el mismo nombre pasan la guarda de aplicación y chocan aquí:
            // se traduce a la misma respuesta 409 en lugar de a un 500.
            throw new PlotConflictException(
                ErrorCodes.ConflictPlotNameDuplicate,
                "Ya existe un terreno con ese nombre en este Workspace.");
        }
    }

    private static bool IsDuplicatePlotName(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
           && pg.ConstraintName == UniqueNameIndexName;
}
