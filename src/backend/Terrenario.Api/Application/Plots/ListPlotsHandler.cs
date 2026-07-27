using Terrenario.Api.Application.Plots.Commands;
using Terrenario.Api.Domain.Plots;

namespace Terrenario.Api.Application.Plots;

/// <summary>
/// MVP-202 — Lista los terrenos del Workspace activo (CA-1). Admite búsqueda por texto y filtro por
/// estado de actividad, alineado con <c>GET /api/v1/plots</c> (<c>search?</c>, <c>is_active?</c>).
/// </summary>
public sealed class ListPlotsHandler(IPlotRepository plotRepository)
{
    public async Task<IReadOnlyList<PlotSummary>> HandleAsync(
        Guid workspaceId,
        string? search,
        bool? isActive,
        CancellationToken ct = default)
    {
        var plots = await plotRepository.ListByWorkspaceAsync(workspaceId, search, isActive, ct);
        return plots.Select(ToSummary).ToList();
    }

    internal static PlotSummary ToSummary(Plot plot) => new(
        plot.Id,
        plot.WorkspaceId,
        plot.Name,
        plot.OwnershipType,
        plot.Alias,
        plot.OwnerName,
        plot.CadastralReference,
        plot.Location,
        plot.TreeCount,
        plot.IsActive);
}
