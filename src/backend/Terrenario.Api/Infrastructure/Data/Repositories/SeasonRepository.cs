using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

public sealed class SeasonRepository(TerrenarioDbContext db) : ISeasonRepository
{
    /// <summary>
    /// Índice único (workspace_id, lower(name)) creado en la migración
    /// <c>AddMasterNameUniqueIndexes</c>. Es la invariante de base de datos que respalda la guarda de
    /// duplicados del maestro (MVP-207, CA-3). No confundir con <c>ux_seasons_workspace_active</c>,
    /// que materializa RN-022 (una sola activa por Workspace).
    /// </summary>
    public const string UniqueNameIndexName = "ux_seasons_workspace_name";

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

        // El alta pasa por aquí, así que la traducción del nombre duplicado también tiene que estar
        // en este camino: sin ella, una carrera entre dos altas devolvería 500 en vez de 409.
        await PersistAsync(ct);
        await tx.CommitAsync(ct);
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
