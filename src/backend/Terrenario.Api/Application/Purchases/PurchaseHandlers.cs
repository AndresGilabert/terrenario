using Terrenario.Api.Application.Purchases.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Purchases;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Purchases;

/// <summary>
/// Comprueba que la temporada de una compra existe en el Workspace activo (RN-021, <c>P-050</c>).
/// Es el equivalente para compras de <c>ActivityLinkResolver</c>, pero con un solo vínculo: la compra
/// no cuelga de terreno ni de responsable —el reparto por terrenos es la imputación de
/// <c>MVP-304</c>—.
///
/// Las temporadas cerradas siguen siendo válidas: RN-024 dice que cerrar es informativo y no bloquea
/// altas ni ediciones.
/// </summary>
public sealed class PurchaseSeasonResolver(ISeasonRepository seasonRepository)
{
    public async Task EnsureSeasonAsync(Guid workspaceId, Guid seasonId, CancellationToken ct)
    {
        if (seasonId == Guid.Empty)
            throw new PurchaseValidationException(
                ErrorCodes.ValidationPurchaseRequiredFields, "La compra necesita una temporada.");

        if (await seasonRepository.FindByIdAsync(workspaceId, seasonId, ct) is null)
            throw new PurchaseValidationException(
                ErrorCodes.ForeignKeyWorkspaceMismatch,
                "La temporada indicada no existe en tu Workspace activo.");
    }
}

/// <summary>MVP-303 — Registra una compra del Workspace activo (HU-1, CA-1).</summary>
public sealed class CreatePurchaseHandler(
    IPurchaseRepository purchaseRepository,
    PurchaseSeasonResolver seasonResolver)
{
    public async Task<PurchaseView> HandleAsync(CreatePurchaseCommand command, CancellationToken ct = default)
    {
        // El dominio valida forma y rangos antes de consultar el maestro, como en actividades.
        var purchase = Purchase.Create(
            command.WorkspaceId,
            command.SeasonId,
            command.PurchaseDate,
            command.Product,
            command.TotalQuantity,
            command.TotalCost,
            command.UserId);

        await seasonResolver.EnsureSeasonAsync(command.WorkspaceId, purchase.SeasonId, ct);

        await purchaseRepository.AddAsync(purchase, ct);
        await purchaseRepository.SaveChangesAsync(ct);

        return await purchaseRepository.GetViewAsync(command.WorkspaceId, purchase.Id, ct)
               ?? throw new InvalidOperationException("La compra recién creada no se pudo releer.");
    }
}

/// <summary>
/// MVP-303 — Corrige una compra ya registrada. Devuelve <c>null</c> si no existe en el Workspace o ya
/// está eliminada (404). Exige la versión vigente (ADR-0005).
///
/// <b>Editar una compra no recalcula sus imputaciones</b> (RN-032, CA-3 de <c>MVP-304</c>): el coste
/// proporcional que ya se guardó se calculó con el <c>unit_price</c> vigente entonces y se conserva.
/// Por eso <c>unit_price</c> se persiste en la compra en vez de derivarse en cada lectura.
/// </summary>
public sealed class UpdatePurchaseHandler(
    IPurchaseRepository purchaseRepository,
    PurchaseSeasonResolver seasonResolver)
{
    public async Task<PurchaseView?> HandleAsync(UpdatePurchaseCommand command, CancellationToken ct = default)
    {
        var purchase = await purchaseRepository.FindByIdAsync(command.WorkspaceId, command.PurchaseId, ct);
        if (purchase is null) return null;

        purchase.EnsureVersion(command.ExpectedVersion);

        var seasonId = command.SeasonId.Or(purchase.SeasonId);
        await seasonResolver.EnsureSeasonAsync(command.WorkspaceId, seasonId, ct);

        purchase.Update(
            seasonId,
            command.PurchaseDate.Or(purchase.PurchaseDate),
            command.Product.Or(purchase.Product)!,
            command.TotalQuantity.Or(purchase.TotalQuantity),
            command.TotalCost.Or(purchase.TotalCost),
            command.UserId);

        await purchaseRepository.SaveChangesAsync(ct);

        return await purchaseRepository.GetViewAsync(command.WorkspaceId, purchase.Id, ct);
    }
}

/// <summary>
/// MVP-303 — Elimina una compra de forma <b>lógica</b> (RN-037), con la versión vigente en
/// <c>If-Match</c>. La confirmación explícita la pone la UI (MVP-305).
/// </summary>
public sealed class DeletePurchaseHandler(IPurchaseRepository purchaseRepository)
{
    /// <returns><c>false</c> si no existe, es de otro Workspace o ya estaba eliminada (404).</returns>
    public async Task<bool> HandleAsync(DeletePurchaseCommand command, CancellationToken ct = default)
    {
        var purchase = await purchaseRepository.FindByIdAsync(command.WorkspaceId, command.PurchaseId, ct);
        if (purchase is null) return false;

        purchase.EnsureVersion(command.ExpectedVersion);
        purchase.Delete(command.UserId);

        await purchaseRepository.SaveChangesAsync(ct);

        return true;
    }
}

/// <summary>MVP-303 — Libro de compras del Workspace activo, con los filtros del contrato.</summary>
public sealed class ListPurchasesHandler(IPurchaseRepository purchaseRepository)
{
    public Task<IReadOnlyList<PurchaseView>> HandleAsync(
        Guid workspaceId,
        PurchaseFilter filter,
        CancellationToken ct = default)
        => purchaseRepository.ListAsync(workspaceId, filter, ct);
}

/// <summary>
/// MVP-303 (HU-2) — Vocabulario de materiales aprendido del histórico del Workspace (RN-031). No es
/// un catálogo: no se administra, no se puede editar y el usuario siempre puede escribir algo que no
/// esté en la lista. Solo existe para escribir menos y repetir menos variantes del mismo nombre.
/// </summary>
public sealed class ListPurchaseProductsHandler(IPurchaseRepository purchaseRepository)
{
    /// <summary>Tope de sugerencias devueltas: es una ayuda de escritura, no un listado navegable.</summary>
    public const int MaxSuggestions = 20;

    public Task<IReadOnlyList<ProductSuggestion>> HandleAsync(
        Guid workspaceId,
        string? search,
        CancellationToken ct = default)
        => purchaseRepository.ListProductSuggestionsAsync(workspaceId, search, MaxSuggestions, ct);
}
