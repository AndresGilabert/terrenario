namespace Terrenario.Api.Domain.Seasons;

public interface ISeasonRepository
{
    /// <summary>Registra una temporada nueva en la unidad de trabajo en curso.</summary>
    Task AddAsync(Season season, CancellationToken ct = default);

    /// <summary>
    /// Temporada activa del Workspace (RN-021/RN-022). En MVP hay como mucho una; se usa para la
    /// autoselección operativa y para el estado de la cabecera.
    /// </summary>
    Task<Season?> FindActiveByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>
    /// Temporada por id dentro del Workspace activo. Devuelve <c>null</c> si no existe o pertenece a
    /// otro Workspace (aislamiento multi-tenant filtrando por <paramref name="workspaceId"/>).
    /// </summary>
    Task<Season?> FindByIdAsync(Guid workspaceId, Guid seasonId, CancellationToken ct = default);

    /// <summary>Temporadas del Workspace, ordenadas para el maestro (activa primero, luego por fecha).</summary>
    Task<IReadOnlyList<Season>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>
    /// Persiste <paramref name="season"/> como la <b>única</b> temporada activa del Workspace,
    /// desactivando cualquier otra activa (RN-022, MVP-203 HU-2). Es la operación de "cambiar de
    /// temporada activa" y también la de "crear una nueva que pasa a ser la activa".
    ///
    /// Hace el cambio en una transacción y en dos fases (primero desactivar la anterior, luego activar
    /// la nueva) para no violar nunca el índice único parcial <c>ux_seasons_workspace_active</c>, que
    /// en PostgreSQL se comprueba por fila y no admite dos activas ni de forma transitoria.
    /// </summary>
    /// <param name="isNew"><c>true</c> si <paramref name="season"/> aún no está persistida (alta).</param>
    Task ActivateExclusivelyAsync(Season season, bool isNew, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
