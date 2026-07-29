using Terrenario.Api.Application.Consumptions.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Purchases;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Consumptions;

/// <summary>
/// Comprueba que el terreno y la temporada de un consumo existen en el Workspace activo (RN-001,
/// RN-021). Equivalente para consumos de <c>ActivityLinkResolver</c>.
/// </summary>
public sealed class ConsumptionLinkResolver(
    IPlotRepository plotRepository,
    ISeasonRepository seasonRepository)
{
    public async Task EnsureLinksAsync(Guid workspaceId, Guid plotId, Guid seasonId, CancellationToken ct)
    {
        if (plotId == Guid.Empty || seasonId == Guid.Empty)
            throw new ConsumptionValidationException(
                ErrorCodes.ValidationConsumptionRequiredFields,
                "El consumo necesita terreno y temporada.");

        if (await plotRepository.FindByIdAsync(workspaceId, plotId, ct) is null)
            throw new ConsumptionValidationException(
                ErrorCodes.ForeignKeyWorkspaceMismatch,
                "El terreno indicado no existe en tu Workspace activo.");

        if (await seasonRepository.FindByIdAsync(workspaceId, seasonId, ct) is null)
            throw new ConsumptionValidationException(
                ErrorCodes.ForeignKeyWorkspaceMismatch,
                "La temporada indicada no existe en tu Workspace activo.");
    }
}

/// <summary>
/// Guarda de sobre-imputación (<c>VALIDATION_CONSUMPTION_OVERFLOW</c>): no se puede repartir más
/// material del que se compró. Cuenta solo las imputaciones <b>vivas</b>, así que retirar una libera
/// su cantidad.
/// </summary>
public sealed class PurchaseImputationGuard(IConsumptionRepository consumptionRepository)
{
    public async Task EnsureFitsAsync(
        Guid workspaceId,
        Purchase purchase,
        decimal quantity,
        Guid? excludeConsumptionId,
        CancellationToken ct)
    {
        var alreadyImputed = await consumptionRepository.SumImputedQuantityAsync(
            workspaceId, purchase.Id, excludeConsumptionId, ct);

        if (alreadyImputed + quantity > purchase.TotalQuantity)
        {
            var available = purchase.TotalQuantity - alreadyImputed;
            throw new ConsumptionValidationException(
                ErrorCodes.ValidationConsumptionOverflow,
                $"No puedes imputar más de lo comprado: de «{purchase.Product}» quedan "
                + $"{available:0.##} de {purchase.TotalQuantity:0.##} sin repartir.");
        }
    }
}

/// <summary>
/// MVP-304 — Imputa una compra a un terreno (HU-1, CA-1). El producto, la temporada y el precio
/// unitario se heredan de la compra, y el coste proporcional sale de ese precio congelado.
/// </summary>
public sealed class ImputePurchaseHandler(
    IConsumptionRepository consumptionRepository,
    IPurchaseRepository purchaseRepository,
    ConsumptionLinkResolver linkResolver,
    PurchaseImputationGuard imputationGuard)
{
    /// <returns><c>null</c> si la compra no existe en el Workspace o ya está eliminada (404).</returns>
    public async Task<ConsumptionView?> HandleAsync(ImputePurchaseCommand command, CancellationToken ct = default)
    {
        var purchase = await purchaseRepository.FindByIdAsync(command.WorkspaceId, command.PurchaseId, ct);
        if (purchase is null) return null;

        // El terreno es lo único que aporta el usuario y hay que verificar: la temporada viene de la
        // compra, que ya está validada.
        await linkResolver.EnsureLinksAsync(command.WorkspaceId, command.PlotId, purchase.SeasonId, ct);

        var consumption = PurchaseConsumption.ImputeFromPurchase(
            command.WorkspaceId,
            purchase.Id,
            purchase.SeasonId,
            purchase.Product,
            purchase.UnitPrice,
            command.PlotId,
            command.Date,
            command.Quantity,
            command.UserId);

        // La guarda va después del dominio: una cantidad inválida se rechaza antes de consultar sumas.
        await imputationGuard.EnsureFitsAsync(
            command.WorkspaceId, purchase, consumption.ConsumedQuantity, null, ct);

        await consumptionRepository.AddAsync(consumption, ct);
        await consumptionRepository.SaveChangesAsync(ct);

        return await consumptionRepository.GetViewAsync(command.WorkspaceId, consumption.Id, ct);
    }
}

/// <summary>
/// MVP-304 — Registra un consumo <b>sin compra previa</b> (HU-2, CA-2, RN-032). La ausencia de compra
/// nunca bloquea: el coste imputado es <c>0</c> y la respuesta lo señala (<c>has_purchase: false</c>)
/// para que la UI avise del impacto en la calidad del dato.
/// </summary>
public sealed class RegisterConsumptionHandler(
    IConsumptionRepository consumptionRepository,
    ConsumptionLinkResolver linkResolver)
{
    public async Task<ConsumptionView> HandleAsync(
        RegisterConsumptionCommand command,
        CancellationToken ct = default)
    {
        var consumption = PurchaseConsumption.RegisterWithoutPurchase(
            command.WorkspaceId,
            command.SeasonId,
            command.PlotId,
            command.Date,
            command.Product,
            command.Quantity,
            command.UserId);

        await linkResolver.EnsureLinksAsync(
            command.WorkspaceId, consumption.PlotId, consumption.SeasonId, ct);

        await consumptionRepository.AddAsync(consumption, ct);
        await consumptionRepository.SaveChangesAsync(ct);

        return await consumptionRepository.GetViewAsync(command.WorkspaceId, consumption.Id, ct)
               ?? throw new InvalidOperationException("El consumo recién creado no se pudo releer.");
    }
}

/// <summary>
/// MVP-304 — Corrige un consumo. El precio unitario **no** se recalcula: sigue siendo el que se
/// congeló al imputar, de modo que cambiar la cantidad ajusta el coste con el precio de entonces
/// (RN-032, CA-3). Si la compra cambió de precio, lo ya consumido no se reescribe.
/// </summary>
public sealed class UpdateConsumptionHandler(
    IConsumptionRepository consumptionRepository,
    IPurchaseRepository purchaseRepository,
    ConsumptionLinkResolver linkResolver,
    PurchaseImputationGuard imputationGuard)
{
    public async Task<ConsumptionView?> HandleAsync(
        UpdateConsumptionCommand command,
        CancellationToken ct = default)
    {
        var consumption = await consumptionRepository.FindByIdAsync(
            command.WorkspaceId, command.ConsumptionId, ct);
        if (consumption is null) return null;

        consumption.EnsureVersion(command.ExpectedVersion);

        var seasonId = command.SeasonId.Or(consumption.SeasonId);
        var plotId = command.PlotId.Or(consumption.PlotId);
        var quantity = command.Quantity.Or(consumption.ConsumedQuantity);

        await linkResolver.EnsureLinksAsync(command.WorkspaceId, plotId, seasonId, ct);

        // Subir la cantidad de una imputación puede desbordar la compra igual que crearla (CA-1); se
        // excluye la propia fila para que su cantidad actual no cuente dos veces.
        if (consumption.PurchaseId is { } purchaseId)
        {
            var purchase = await purchaseRepository.FindByIdAsync(command.WorkspaceId, purchaseId, ct);
            if (purchase is not null)
                await imputationGuard.EnsureFitsAsync(
                    command.WorkspaceId, purchase, quantity, consumption.Id, ct);
        }

        consumption.Update(
            seasonId,
            plotId,
            command.Date.Or(consumption.Date),
            command.Product.Or(consumption.Product)!,
            quantity,
            command.UserId);

        await consumptionRepository.SaveChangesAsync(ct);

        return await consumptionRepository.GetViewAsync(command.WorkspaceId, consumption.Id, ct);
    }
}

/// <summary>MVP-304 — Elimina un consumo de forma <b>lógica</b> (RN-037), con la versión vigente.</summary>
public sealed class DeleteConsumptionHandler(IConsumptionRepository consumptionRepository)
{
    public async Task<bool> HandleAsync(DeleteConsumptionCommand command, CancellationToken ct = default)
    {
        var consumption = await consumptionRepository.FindByIdAsync(
            command.WorkspaceId, command.ConsumptionId, ct);
        if (consumption is null) return false;

        consumption.EnsureVersion(command.ExpectedVersion);
        consumption.Delete(command.UserId);

        await consumptionRepository.SaveChangesAsync(ct);

        return true;
    }
}

/// <summary>MVP-304 — Consumos e imputaciones del Workspace activo, por fecha de negocio (CA-4).</summary>
public sealed class ListConsumptionsHandler(IConsumptionRepository consumptionRepository)
{
    public Task<IReadOnlyList<ConsumptionView>> HandleAsync(
        Guid workspaceId,
        ConsumptionFilter filter,
        CancellationToken ct = default)
        => consumptionRepository.ListAsync(workspaceId, filter, ct);
}
