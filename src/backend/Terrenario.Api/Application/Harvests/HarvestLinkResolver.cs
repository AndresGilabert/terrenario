using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Harvests;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Harvests;

/// <summary>
/// Comprueba que los vínculos de una cosecha (terreno y temporada) existen <b>en el Workspace
/// activo</b> antes de persistirla. Es la guarda de <c>FOREIGN_KEY_WORKSPACE_MISMATCH</c> del
/// contrato: sin ella, un id de otra explotación llegaría a la base de datos y la violación de clave
/// ajena se traduciría en un 500 en vez de en un 400 con mensaje útil.
///
/// <b>Los maestros inactivos siguen siendo válidos</b>, igual que en actividades (MVP-301): la UI
/// ofrece solo los activos para registros nuevos, pero corregir una cosecha que referencia un terreno
/// ya inactivado no debe obligar a reactivarlo.
/// </summary>
public sealed class HarvestLinkResolver(
    IPlotRepository plotRepository,
    ISeasonRepository seasonRepository)
{
    public async Task EnsureLinksAsync(
        Guid workspaceId,
        Guid plotId,
        Guid seasonId,
        CancellationToken ct)
    {
        // Un vínculo vacío es un campo obligatorio que falta, no una referencia rota: se distingue
        // antes de gastar consultas para que el cliente reciba el código correcto (RN-001/RN-021).
        if (plotId == Guid.Empty || seasonId == Guid.Empty)
            throw new HarvestValidationException(
                ErrorCodes.ValidationHarvestRequiredFields,
                "La cosecha necesita terreno y temporada.");

        if (await plotRepository.FindByIdAsync(workspaceId, plotId, ct) is null)
            throw Mismatch("El terreno indicado no existe en tu Workspace activo.");

        if (await seasonRepository.FindByIdAsync(workspaceId, seasonId, ct) is null)
            throw Mismatch("La temporada indicada no existe en tu Workspace activo.");
    }

    private static HarvestValidationException Mismatch(string message) =>
        new(ErrorCodes.ForeignKeyWorkspaceMismatch, message);
}
