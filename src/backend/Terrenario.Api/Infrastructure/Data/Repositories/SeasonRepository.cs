using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

public sealed class SeasonRepository(TerrenarioDbContext db) : ISeasonRepository
{
    /// <summary>
    /// Índice único (workspace_id, lower(name)) creado en la migración
    /// <c>AddMasterNameUniqueIndexes</c>. Es la invariante de base de datos que respalda la guarda de
    /// duplicados del maestro (MVP-207, CA-3).
    /// </summary>
    public const string UniqueNameIndexName = "ux_seasons_workspace_name";

    public async Task AddAsync(Season season, CancellationToken ct = default)
        => await db.Seasons.AddAsync(season, ct);

    public async Task<Season?> FindWorkingSeasonAsync(
        Guid userId, Guid workspaceId, CancellationToken ct = default)
    {
        // La fijada en la membresía del usuario (MVP-209). Solo la membresía activa cuenta.
        var fixedId = await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId
                        && m.Status == WorkspaceMemberStatuses.Active)
            .Select(m => m.ActiveSeasonId)
            .FirstOrDefaultAsync(ct);

        if (fixedId is { } id)
        {
            var chosen = await db.Seasons
                .FirstOrDefaultAsync(s => s.Id == id && s.WorkspaceId == workspaceId, ct);
            if (chosen is not null) return chosen;
            // La fijada se borró: se cae al defecto (la FK ON DELETE SET NULL ya lo habría limpiado,
            // pero se comprueba igual por si la lectura precede a la limpieza).
        }

        var seasons = await db.Seasons.Where(s => s.WorkspaceId == workspaceId).ToListAsync(ct);
        return WorkingSeasonPolicy.ResolveDefault(seasons, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public async Task SetWorkingSeasonAsync(
        Guid userId, Guid workspaceId, Guid seasonId, CancellationToken ct = default)
    {
        // UPDATE directo a la membresía del usuario: no pasa por el rastreador ni toca a otros miembros.
        await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId
                        && m.Status == WorkspaceMemberStatuses.Active)
            .ExecuteUpdateAsync(set => set.SetProperty(m => m.ActiveSeasonId, seasonId), ct);
    }

    public Task<Season?> FindByIdAsync(Guid workspaceId, Guid seasonId, CancellationToken ct = default)
        => db.Seasons
            .FirstOrDefaultAsync(s => s.Id == seasonId && s.WorkspaceId == workspaceId, ct);

    public async Task<IReadOnlyList<Season>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
    {
        // Orden del maestro: las no cerradas arriba, después por fecha de inicio descendente (la campaña
        // más reciente primero). El orden ya no depende de «activa» (MVP-209): esa es una preferencia
        // por usuario que resuelve el caso de uso. Se ordena por columnas reales (lección de P-014).
        return await db.Seasons
            .Where(s => s.WorkspaceId == workspaceId)
            .OrderBy(s => s.IsClosed)
            .ThenByDescending(s => s.StartDate)
            .ToListAsync(ct);
    }

    public Task<bool> ExistsWithNameAsync(
        Guid workspaceId,
        string name,
        Guid? excludeSeasonId = null,
        CancellationToken ct = default)
    {
        // Comparación insensible a mayúsculas con `ToLower()`, que tanto Npgsql como SQLite traducen
        // a `lower(...)`: es el mismo criterio del índice único `ux_seasons_workspace_name`, así que
        // la guarda de aplicación y la invariante de base de datos no pueden discrepar.
        var normalized = name.ToLower();

        var query = db.Seasons.Where(s => s.WorkspaceId == workspaceId && s.Name.ToLower() == normalized);

        if (excludeSeasonId is { } excluded)
            query = query.Where(s => s.Id != excluded);

        return query.AnyAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => PersistAsync(ct);

    private async Task PersistAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsDuplicateSeasonName(ex))
        {
            // Dos altas simultáneas con el mismo nombre pasan la guarda de aplicación y chocan aquí:
            // se traduce a la misma respuesta 409 en lugar de a un 500.
            throw new SeasonConflictException(
                ErrorCodes.ConflictSeasonNameDuplicate,
                "Ya existe una temporada con ese nombre en este Workspace.");
        }
    }

    private static bool IsDuplicateSeasonName(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
           && pg.ConstraintName == UniqueNameIndexName;
}
