using Terrenario.Api.Application.Plots.Commands;
using Terrenario.Api.Domain.Plots;

namespace Terrenario.Api.Application.Plots;

/// <summary>
/// MVP-202 — Edita un terreno del Workspace activo (CA-2) o cambia su estado de actividad (CA-3). El
/// terreno se busca acotado al Workspace: si no existe en él, devuelve <c>null</c> y el borde de
/// transporte responde 404 (no se revela la existencia de terrenos de otros Workspaces).
/// </summary>
public sealed class UpdatePlotHandler(IPlotRepository plotRepository)
{
    public async Task<PlotSummary?> HandleAsync(UpdatePlotCommand command, CancellationToken ct = default)
    {
        var plot = await plotRepository.FindByIdAsync(command.WorkspaceId, command.PlotId, ct);
        if (plot is null) return null;

        // Edición parcial: los campos ausentes conservan el valor actual (no se borran).
        plot.Update(
            command.Name.Or(plot.Name)!,
            command.OwnershipType.Or(plot.OwnershipType)!,
            command.Alias.Or(plot.Alias),
            command.OwnerName.Or(plot.OwnerName),
            command.CadastralReference.Or(plot.CadastralReference),
            command.Location.Or(plot.Location),
            command.TreeCount.Or(plot.TreeCount));

        if (command.IsActive.Present)
            plot.SetActive(command.IsActive.Value);

        await plotRepository.SaveChangesAsync(ct);

        return ListPlotsHandler.ToSummary(plot);
    }
}
