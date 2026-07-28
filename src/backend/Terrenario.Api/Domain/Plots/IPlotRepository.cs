namespace Terrenario.Api.Domain.Plots;

public interface IPlotRepository
{
    /// <summary>Registra un terreno nuevo en la unidad de trabajo en curso.</summary>
    Task AddAsync(Plot plot, CancellationToken ct = default);

    /// <summary>
    /// Terreno por id dentro del Workspace activo. Devuelve <c>null</c> si no existe o pertenece a
    /// otro Workspace (el aislamiento multi-tenant se refuerza filtrando por <paramref name="workspaceId"/>).
    /// </summary>
    Task<Plot?> FindByIdAsync(Guid workspaceId, Guid plotId, CancellationToken ct = default);

    /// <summary>
    /// Terrenos del Workspace (MVP-202). Filtra opcionalmente por texto (<paramref name="search"/>,
    /// sobre nombre/alias/ubicación) y por estado de actividad (<paramref name="isActive"/>).
    /// </summary>
    Task<IReadOnlyList<Plot>> ListByWorkspaceAsync(
        Guid workspaceId,
        string? search,
        bool? isActive,
        CancellationToken ct = default);

    /// <summary>
    /// ¿Hay ya un terreno con ese nombre en el Workspace, ignorando mayúsculas? (MVP-207, CA-2). Cubre
    /// todo el maestro, también los inactivos: inactivar no libera el nombre. El alias no entra en la
    /// comparación: es un apodo libre y puede repetirse.
    /// </summary>
    /// <param name="excludePlotId">Terreno que se excluye de la comparación (el que se renombra).</param>
    Task<bool> ExistsWithNameAsync(
        Guid workspaceId,
        string name,
        Guid? excludePlotId = null,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
