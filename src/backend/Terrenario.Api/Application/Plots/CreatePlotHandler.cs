using Terrenario.Api.Application.Plots.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Plots;

namespace Terrenario.Api.Application.Plots;

/// <summary>
/// MVP-202 — Da de alta un terreno en el Workspace activo con los datos mínimos (RN-028). El resto
/// de campos son opcionales y no bloquean el alta (CA-1).
///
/// MVP-207 (CA-2) añade la guarda de nombre único por Workspace: el terreno es la unidad a la que se
/// asocia todo registro operativo (RN-001), así que dos parcelas «Prueba» harían ambigua cualquier
/// actividad, cosecha o compra imputada después.
/// </summary>
public sealed class CreatePlotHandler(IPlotRepository plotRepository)
{
    public async Task<PlotSummary> HandleAsync(CreatePlotCommand command, CancellationToken ct = default)
    {
        // El dominio normaliza y valida los campos; se construye primero para no comprobar duplicados
        // contra un texto sin normalizar.
        var plot = Plot.Create(
            command.WorkspaceId,
            command.Name,
            command.OwnershipType,
            command.Alias,
            command.OwnerName,
            command.CadastralReference,
            command.Location,
            command.TreeCount);

        await EnsureNameIsFreeAsync(plotRepository, command.WorkspaceId, plot.Name, null, ct);

        await plotRepository.AddAsync(plot, ct);
        await plotRepository.SaveChangesAsync(ct);

        return ListPlotsHandler.ToSummary(plot);
    }

    /// <summary>
    /// Guarda de duplicados del maestro, compartida con la edición. Lanza
    /// <see cref="PlotConflictException"/> (409) si el nombre ya existe en el Workspace.
    /// </summary>
    internal static async Task EnsureNameIsFreeAsync(
        IPlotRepository plotRepository,
        Guid workspaceId,
        string normalizedName,
        Guid? excludePlotId,
        CancellationToken ct)
    {
        var exists = await plotRepository.ExistsWithNameAsync(workspaceId, normalizedName, excludePlotId, ct);
        if (exists)
            throw new PlotConflictException(
                ErrorCodes.ConflictPlotNameDuplicate,
                $"Ya existe un terreno «{normalizedName}» en este Workspace.");
    }
}
