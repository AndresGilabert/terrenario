namespace Terrenario.Api.Domain.Seasons;

public interface ISeasonRepository
{
    /// <summary>Registra una temporada nueva en la unidad de trabajo en curso.</summary>
    Task AddAsync(Season season, CancellationToken ct = default);

    /// <summary>
    /// MVP-209 — Temporada de <b>trabajo</b> de un usuario en un Workspace (RN-021). Lee la fijada en su
    /// membresía (<c>active_season_id</c>); si no hay ninguna, o la fijada ya no existe, resuelve un
    /// defecto con <see cref="WorkingSeasonPolicy"/>. Es la que autoselecciona la operativa y el defecto
    /// del dashboard. Devuelve <c>null</c> solo si el Workspace no tiene temporadas.
    /// </summary>
    Task<Season?> FindWorkingSeasonAsync(Guid userId, Guid workspaceId, CancellationToken ct = default);

    /// <summary>
    /// MVP-209 — Fija la temporada de trabajo de un usuario (activar, o al crear una). Solo actualiza la
    /// membresía del usuario indicado: no afecta a otros miembros del mismo Workspace (CA-2). No
    /// persiste por sí sola.
    /// </summary>
    Task SetWorkingSeasonAsync(Guid userId, Guid workspaceId, Guid seasonId, CancellationToken ct = default);

    /// <summary>
    /// Temporada por id dentro del Workspace activo. Devuelve <c>null</c> si no existe o pertenece a
    /// otro Workspace (aislamiento multi-tenant filtrando por <paramref name="workspaceId"/>).
    /// </summary>
    Task<Season?> FindByIdAsync(Guid workspaceId, Guid seasonId, CancellationToken ct = default);

    /// <summary>Temporadas del Workspace, ordenadas para el maestro (abiertas primero, luego por fecha).</summary>
    Task<IReadOnlyList<Season>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>
    /// ¿Hay ya una temporada con ese nombre en el Workspace, ignorando mayúsculas? (MVP-207, CA-2).
    /// Cubre todo el maestro, también las cerradas: cerrar no libera el nombre.
    /// </summary>
    /// <param name="excludeSeasonId">Temporada que se excluye de la comparación (la que se renombra).</param>
    Task<bool> ExistsWithNameAsync(
        Guid workspaceId,
        string name,
        Guid? excludeSeasonId = null,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
