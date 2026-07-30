using Terrenario.Api.Domain.Harvests;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Dashboard;

/// <summary>Lo que el cliente pide ver (MVP-403). Todo opcional: los defectos los pone el servidor.</summary>
public sealed record DashboardRequest(Guid? SeasonId = null, IReadOnlyCollection<Guid>? PlotIds = null);

/// <summary>
/// Ámbito de lectura ya resuelto (RN-008): la temporada y los terrenos concretos sobre los que se
/// calculan los widgets.
///
/// <b>Viaja en la respuesta a propósito.</b> Si el cliente no informa filtros, el servidor aplica la
/// temporada activa y todos los terrenos activos; si no dijera cuáles ha elegido, la pantalla mostraría
/// cifras sin poder explicar de qué son. Es también lo que permite a <c>MVP-405</c> pintar los filtros
/// ya posicionados sin adivinar el defecto.
/// </summary>
public sealed record DashboardScope(
    Season? Season,
    IReadOnlyList<Plot> Plots)
{
    /// <summary>
    /// Sin temporada no hay nada que agregar: el MVP asocia toda la producción a una campaña (RN-021),
    /// así que un Workspace sin temporada no tiene resumen, tiene un aviso.
    /// </summary>
    public bool IsResolvable => Season is not null;

    public HarvestAggregateFilter ToFilter()
        => new(Season?.Id, Plots.Select(plot => plot.Id).ToArray());
}

/// <summary>
/// Resuelve el ámbito del dashboard aplicando los valores por defecto de RN-008: sin temporada, la
/// <b>activa</b>; sin terrenos, <b>todos los activos</b>.
///
/// Los terrenos pedidos se intersecan con los del Workspace: un id ajeno o inexistente se descarta en
/// silencio en vez de responder un error. Es una **lectura**, no una escritura —a diferencia del alta
/// de cosecha, donde un vínculo ajeno sí es un `400`—: quien llega con un filtro obsoleto en la URL
/// debe ver el dashboard de lo que sí existe, no una pantalla de error.
///
/// <b>Un terreno inactivo sí cuenta si se pide explícitamente.</b> Inactivar deja de ofrecer para
/// registros nuevos (MVP-202, CA-3), no borra el histórico: excluir su producción al mirar una campaña
/// pasada falsearía los totales.
/// </summary>
public sealed class DashboardScopeResolver(
    ISeasonRepository seasonRepository,
    IPlotRepository plotRepository)
{
    public async Task<DashboardScope> ResolveAsync(
        Guid workspaceId,
        DashboardRequest request,
        CancellationToken ct = default)
    {
        var season = request.SeasonId is { } seasonId
            ? await seasonRepository.FindByIdAsync(workspaceId, seasonId, ct)
            : await seasonRepository.FindActiveByWorkspaceAsync(workspaceId, ct);

        var all = await plotRepository.ListByWorkspaceAsync(workspaceId, null, null, ct);

        var plots = request.PlotIds is { Count: > 0 } requested
            ? all.Where(plot => requested.Contains(plot.Id)).ToList()
            // RN-008 — por defecto, todos los terrenos **activos**.
            : all.Where(plot => plot.IsActive).ToList();

        return new DashboardScope(season, plots);
    }
}
