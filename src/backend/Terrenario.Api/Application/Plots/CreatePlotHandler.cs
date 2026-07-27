using Terrenario.Api.Application.Plots.Commands;
using Terrenario.Api.Domain.Plots;

namespace Terrenario.Api.Application.Plots;

/// <summary>
/// MVP-202 — Da de alta un terreno en el Workspace activo con los datos mínimos (RN-028). El resto
/// de campos son opcionales y no bloquean el alta (CA-1).
/// </summary>
public sealed class CreatePlotHandler(IPlotRepository plotRepository)
{
    public async Task<PlotSummary> HandleAsync(CreatePlotCommand command, CancellationToken ct = default)
    {
        var plot = Plot.Create(
            command.WorkspaceId,
            command.Name,
            command.OwnershipType,
            command.Alias,
            command.OwnerName,
            command.CadastralReference,
            command.Location,
            command.TreeCount);

        await plotRepository.AddAsync(plot, ct);
        await plotRepository.SaveChangesAsync(ct);

        return ListPlotsHandler.ToSummary(plot);
    }
}
