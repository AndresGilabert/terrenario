using Terrenario.Api.Domain.Materials;

namespace Terrenario.Api.Application.Materials;

/// <summary>
/// MVP-708 (<c>P-057</c>) — Vocabulario de materiales aprendido del histórico del Workspace (RN-031).
///
/// Sustituye a <c>ListPurchaseProductsHandler</c> (MVP-303), que solo miraba compras. El campo de
/// material del consumo sin compra previa es el <b>mismo campo de texto libre</b> que el del alta de
/// compra, en la misma pantalla, y no sugería nada: favorecía justo la dispersión de nombres que las
/// sugerencias existen para evitar.
/// </summary>
public sealed class ListMaterialSuggestionsHandler(IMaterialRepository materialRepository)
{
    /// <summary>Tope de sugerencias devueltas: es una ayuda de escritura, no un listado navegable.</summary>
    public const int MaxSuggestions = 20;

    public Task<IReadOnlyList<MaterialSuggestion>> HandleAsync(
        Guid workspaceId,
        string? search,
        CancellationToken ct = default)
        => materialRepository.ListSuggestionsAsync(workspaceId, search, MaxSuggestions, ct);
}
