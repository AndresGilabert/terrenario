using Terrenario.Api.Application.Harvests.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Harvests;

namespace Terrenario.Api.Application.Harvests;

/// <summary>
/// MVP-402 (RN-013/RN-014/RN-016) — Lleva el rendimiento informado a la unidad canónica L/100kg antes
/// de que llegue al agregado. Vive en la capa de aplicación y no en el dominio a propósito: la unidad
/// de entrada es una concesión al usuario —las almazaras dan el rendimiento graso en kg/100kg—, no una
/// propiedad de la cosecha. Lo que se guarda y lo que compara el dashboard es siempre lo mismo.
/// </summary>
internal static class YieldNormalizer
{
    public static decimal? ToCanonical(decimal? yield, string? unit)
    {
        if (unit is not null && !HarvestYieldUnits.IsSupported(unit))
            throw new HarvestValidationException(
                ErrorCodes.ValidationHarvestYieldUnitInvalid,
                $"La unidad de rendimiento no es válida. Valores admitidos: {string.Join(", ", HarvestYieldUnits.Supported)}.");

        return yield is null ? null : HarvestYieldConversion.ToCanonical(yield.Value, unit);
    }
}

/// <summary>
/// MVP-401 — Registra una cosecha en el Workspace activo (HU-1, CA-1).
///
/// El orden es deliberado, el mismo que MVP-301: primero el dominio valida forma y reglas (400), y
/// solo con un agregado válido se comprueba que los vínculos pertenecen al Workspace (400
/// <c>FOREIGN_KEY_WORKSPACE_MISMATCH</c>). Así una petición mal formada no gasta consultas a los
/// maestros.
/// </summary>
public sealed class CreateHarvestHandler(
    IHarvestRepository harvestRepository,
    HarvestLinkResolver linkResolver)
{
    public async Task<HarvestView> HandleAsync(CreateHarvestCommand command, CancellationToken ct = default)
    {
        var harvest = Harvest.Create(
            command.WorkspaceId,
            command.PlotId,
            command.SeasonId,
            command.Date,
            command.Product,
            command.Kgs,
            command.Destination,
            // RN-014 — el rendimiento puede llegar en kg de aceite/100kg; se guarda en L/100kg.
            YieldNormalizer.ToCanonical(command.Yield, command.YieldUnit),
            command.Liters,
            command.UnitPrice,
            command.UserId);

        await linkResolver.EnsureLinksAsync(command.WorkspaceId, harvest.PlotId, harvest.SeasonId, ct);

        await harvestRepository.AddAsync(harvest, ct);
        await harvestRepository.SaveChangesAsync(ct);

        // Se relee como vista para devolver ya resueltos los nombres de terreno y temporada y el aviso
        // de fecha fuera de rango (RN-023), que es lo que la UI pinta sin más peticiones.
        return await harvestRepository.GetViewAsync(command.WorkspaceId, harvest.Id, ct)
               ?? throw new InvalidOperationException("La cosecha recién creada no se pudo releer.");
    }
}

/// <summary>
/// MVP-401 — Corrige una cosecha ya registrada (HU-2, CA-2). Se busca acotada al Workspace y viva: si
/// no existe, es de otro Workspace o ya fue eliminada lógicamente, devuelve <c>null</c> y el borde de
/// transporte responde 404 sin revelar recursos ajenos.
///
/// <b>Concurrencia optimista</b> (CA-5, ADR-0005): la versión de <c>If-Match</c> se comprueba
/// <b>antes</b> de tocar nada, para que un conflicto no deje el agregado a medias ni gaste las
/// consultas de vínculos.
/// </summary>
public sealed class UpdateHarvestHandler(
    IHarvestRepository harvestRepository,
    HarvestLinkResolver linkResolver)
{
    public async Task<HarvestView?> HandleAsync(UpdateHarvestCommand command, CancellationToken ct = default)
    {
        var harvest = await harvestRepository.FindByIdAsync(command.WorkspaceId, command.HarvestId, ct);
        if (harvest is null) return null;

        harvest.EnsureVersion(command.ExpectedVersion);

        var plotId = command.PlotId.Or(harvest.PlotId);
        var seasonId = command.SeasonId.Or(harvest.SeasonId);
        var (yield, liters) = ResolveYieldPair(command, harvest);

        // Los vínculos se comprueban antes de mutar: un 400 no debe dejar el agregado a medias en el
        // change tracker de la petición.
        await linkResolver.EnsureLinksAsync(command.WorkspaceId, plotId, seasonId, ct);

        harvest.Update(
            plotId,
            seasonId,
            command.Date.Or(harvest.Date),
            command.Product.Or(harvest.Product)!,
            command.Kgs.Or(harvest.Kgs),
            command.Destination.Or(harvest.Destination)!,
            yield,
            liters,
            command.UnitPrice.Or(harvest.UnitPrice),
            command.UserId);

        await harvestRepository.SaveChangesAsync(ct);

        return await harvestRepository.GetViewAsync(command.WorkspaceId, harvest.Id, ct);
    }

    /// <summary>
    /// El rendimiento y los litros son un par excluyente (RN-004). Si viene <b>cualquiera</b> de los
    /// dos se sustituye la pareja completa y el ausente pasa a nulo: enviar solo <c>liters</c> sobre
    /// una cosecha que ya tenía <c>yield</c> dejaría los dos informados y el dominio lo rechazaría.
    /// Es el mismo criterio que MVP-301 aplicó al par tarea de la actividad.
    /// </summary>
    private static (decimal? Yield, decimal? Liters) ResolveYieldPair(
        UpdateHarvestCommand command,
        Harvest harvest)
        => command.Yield.Present || command.Liters.Present
            // RN-014 — la unidad solo aplica al valor que llega en esta petición: lo ya persistido
            // está en la canónica y volver a convertirlo lo estropearía.
            ? (YieldNormalizer.ToCanonical(command.Yield.Value, command.YieldUnit), command.Liters.Value)
            : (harvest.Yield, harvest.Liters);
}

/// <summary>
/// MVP-401 — Elimina una cosecha de forma <b>lógica</b> (RN-037, CA-5): deja de aparecer en el
/// listado, en el diario y en el dashboard, pero la fila permanece en base de datos. No hay papelera
/// ni restauración en el MVP; la purga se decide con la política de retención (<c>P-033</c>).
///
/// Exige la versión vigente en <c>If-Match</c> (ADR-0005). La confirmación explícita del usuario la
/// pone la UI.
/// </summary>
public sealed class DeleteHarvestHandler(IHarvestRepository harvestRepository)
{
    /// <returns><c>false</c> si la cosecha no existe, es de otro Workspace o ya estaba eliminada (404).</returns>
    public async Task<bool> HandleAsync(DeleteHarvestCommand command, CancellationToken ct = default)
    {
        var harvest = await harvestRepository.FindByIdAsync(command.WorkspaceId, command.HarvestId, ct);
        if (harvest is null) return false;

        harvest.EnsureVersion(command.ExpectedVersion);
        harvest.Delete(command.UserId);

        await harvestRepository.SaveChangesAsync(ct);

        return true;
    }
}

/// <summary>MVP-401 — Listado de cosechas vivas del Workspace por fecha de negocio descendente (RN-033).</summary>
public sealed class ListHarvestsHandler(IHarvestRepository harvestRepository)
{
    public Task<IReadOnlyList<HarvestView>> HandleAsync(
        Guid workspaceId,
        HarvestFilter filter,
        CancellationToken ct = default)
        => harvestRepository.ListAsync(workspaceId, filter, ct);
}

/// <summary>
/// MVP-401 — Una cosecha concreta. La necesita el diario unificado: su entrada es una proyección común
/// de los cuatro tipos y no lleva todos los campos que pide el formulario de corrección.
/// </summary>
public sealed class GetHarvestHandler(IHarvestRepository harvestRepository)
{
    public Task<HarvestView?> HandleAsync(Guid workspaceId, Guid harvestId, CancellationToken ct = default)
        => harvestRepository.GetViewAsync(workspaceId, harvestId, ct);
}
