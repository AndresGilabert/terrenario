namespace Terrenario.Api.Domain.Materials;

/// <summary>
/// MVP-708 (<c>P-057</c>) — Puerto de lectura del <b>vocabulario de materiales</b> del Workspace
/// (RN-031).
///
/// Nace como puerto propio y no como un método más de <c>IPurchaseRepository</c> porque el
/// vocabulario dejó de ser de las compras: se aprende de los <b>dos</b> libros —lo comprado y lo
/// consumido sin compra previa— y un método que lee dos entidades escondido tras el puerto de una
/// sola sería una firma que miente. Es el mismo criterio con el que <c>MVP-506</c> sacó el diario
/// unificado a <c>IDiaryRepository</c> en vez de repartirlo entre los puertos operativos.
///
/// Sigue sin ser un catálogo: no se administra, no se edita y el usuario siempre puede escribir algo
/// que no esté en la lista.
/// </summary>
public interface IMaterialRepository
{
    /// <summary>
    /// Materiales ya escritos en el Workspace, los más frecuentes primero, filtrando opcionalmente
    /// por un fragmento de texto. Excluye lo eliminado lógicamente (RN-037): si se retiró, no
    /// conviene volver a proponerlo.
    /// </summary>
    Task<IReadOnlyList<MaterialSuggestion>> ListSuggestionsAsync(
        Guid workspaceId,
        string? search,
        int limit,
        CancellationToken ct = default);
}

/// <summary>
/// Material del histórico con cuántas veces se ha escrito ese nombre (RN-031). El campo se llama
/// <c>Product</c> —y no <c>Material</c>— porque es el nombre del dato en el contrato y en las dos
/// entidades que lo alimentan (<c>purchases.product</c>, <c>purchase_consumptions.product</c>).
/// </summary>
public sealed record MaterialSuggestion(string Product, int TimesUsed);
