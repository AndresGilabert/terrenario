using Terrenario.Api.Application.Plots.Commands;
using Terrenario.Api.Domain.Plots;

namespace Terrenario.Api.Application.Plots;

/// <summary>
/// MVP-202 — Edita un terreno del Workspace activo (CA-2) o cambia su estado de actividad (CA-3). El
/// terreno se busca acotado al Workspace: si no existe en él, devuelve <c>null</c> y el borde de
/// transporte responde 404 (no se revela la existencia de terrenos de otros Workspaces).
///
/// MVP-207 (CA-2): renombrar tampoco puede dejar dos terrenos con el mismo nombre.
/// </summary>
public sealed class UpdatePlotHandler(IPlotRepository plotRepository)
{
    public async Task<PlotSummary?> HandleAsync(UpdatePlotCommand command, CancellationToken ct = default)
    {
        var plot = await plotRepository.FindByIdAsync(command.WorkspaceId, command.PlotId, ct);
        if (plot is null) return null;

        // El nombre se normaliza y valida primero (400) y solo después se comprueba el duplicado
        // (409), sin tocar el agregado hasta que ambas guardas pasan. Se excluye el propio terreno:
        // cambiar solo las mayúsculas de su nombre no es un conflicto consigo mismo.
        if (command.Name.Present)
        {
            var normalized = Plot.NormalizeName(command.Name.Value!);
            await CreatePlotHandler.EnsureNameIsFreeAsync(
                plotRepository, command.WorkspaceId, normalized, plot.Id, ct);
        }

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
