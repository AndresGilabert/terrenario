namespace Terrenario.Api.Domain.Plots;

/// <summary>
/// Catálogo cerrado <c>plot_ownership_type</c> (MVP-202). El alta mínima de terreno exige
/// <c>tipo_propiedad</c> (RN-028) y la visión de producto lo define como la distinción entre
/// explotaciones «propias» y «cedidas». Los valores son vocabulario de dominio y se mantienen en
/// español (convención de catálogos cerrados de <c>contratos-api.md</c>); el identificador del
/// catálogo va en inglés (ADR-0009).
/// </summary>
public static class PlotOwnershipTypes
{
    public const string Propia = "propia";
    public const string Cedida = "cedida";

    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        Propia,
        Cedida
    };

    public static bool IsValid(string? value) => value is not null && Allowed.Contains(value);
}
