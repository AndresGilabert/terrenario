namespace Terrenario.Api.Domain.Seasons;

public interface ISeasonRepository
{
    /// <summary>Registra una temporada nueva en la unidad de trabajo en curso.</summary>
    Task AddAsync(Season season, CancellationToken ct = default);

    /// <summary>
    /// Temporada activa del Workspace (RN-021/RN-022). En MVP hay como mucho una; se usa para la
    /// autoselección operativa y para el ajuste de la propuesta inicial (MVP-201).
    /// </summary>
    Task<Season?> FindActiveByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
