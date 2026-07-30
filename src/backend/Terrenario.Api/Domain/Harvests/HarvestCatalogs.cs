namespace Terrenario.Api.Domain.Harvests;

/// <summary>
/// Catálogo global fijo de productos de cosecha (MVP-402, RN-030). Es <b>gobernado por sistema</b>:
/// no lo edita el usuario ni se guarda por Workspace, a diferencia del catálogo de tareas (RN-026) o
/// del material de compra, que es texto libre (RN-031).
///
/// <b>Un solo valor en el MVP.</b> La KB exigía el catálogo pero no definía sus valores. Decisión del
/// PO (2026-07-29): la <b>variedad</b> pertenece al terreno —no cambia partida a partida— y el
/// <b>producto</b> debería vivir a nivel de Workspace para poder modular el cálculo de rendimiento
/// según de qué se trate. Ambas cosas son ampliación posterior (`MVP-999`, `P-059`/`P-060`); mientras
/// tanto el MVP está ligado al olivar y el dashboard no distingue variedades.
///
/// Los valores son vocabulario de dominio y van en español (ADR-0009).
/// </summary>
public static class HarvestProducts
{
    public const string AceitunaOlivar = "aceituna_olivar";

    public static readonly IReadOnlySet<string> Supported = new HashSet<string> { AceitunaOlivar };

    public static bool IsSupported(string? value) => value is not null && Supported.Contains(value);
}

/// <summary>
/// Taxonomía cerrada de destinos de la cosecha (MVP-402, RN-012).
///
/// <see cref="Desconocido"/> es un <b>valor válido, no un hueco</b>: el CA-2 de `MVP-402` (HU-2) exige
/// que no conocer todavía el cierre comercial o de uso nunca retrase el registro operativo. La UI puede
/// rotularlo «Sin destino», que es el alias que RN-012 autoriza; el canon en base de datos no cambia.
/// </summary>
public static class HarvestDestinations
{
    public const string VentaAceituna = "venta_aceituna";
    public const string AceiteParaVenta = "aceite_para_venta";
    public const string AceitePersonal = "aceite_personal";
    public const string Desconocido = "desconocido";

    public static readonly IReadOnlySet<string> Supported =
        new HashSet<string> { VentaAceituna, AceiteParaVenta, AceitePersonal, Desconocido };

    public static bool IsSupported(string? value) => value is not null && Supported.Contains(value);
}

/// <summary>
/// Unidades admitidas para <b>informar</b> el rendimiento (MVP-402, RN-014). No es lo mismo que la
/// unidad en la que se <b>guarda</b>: la persistida es siempre la canónica L/100kg (RN-013), porque es
/// la que hace comparables dos campañas y dos Workspaces.
///
/// RN-014 admite tres orígenes; estos dos son los que llegan como valor informado:
/// <list type="number">
/// <item><see cref="L100Kg"/> — litros de aceite por cada 100 kg de aceituna, la canónica.</item>
/// <item><see cref="Kg100Kg"/> — kilos de aceite por cada 100 kg de aceituna, que es como lo dan
/// muchas almazaras («rendimiento graso»). Se convierte con la densidad de RN-016.</item>
/// </list>
/// El tercero —cálculo desde kilos entregados y litros obtenidos— no es una unidad de entrada sino un
/// valor <b>derivado</b>: ver <c>HarvestView.EffectiveYield</c>.
/// </summary>
public static class HarvestYieldUnits
{
    public const string L100Kg = "l_100kg";
    public const string Kg100Kg = "kg_100kg";

    public static readonly IReadOnlySet<string> Supported = new HashSet<string> { L100Kg, Kg100Kg };

    public static bool IsSupported(string? value) => value is not null && Supported.Contains(value);
}

/// <summary>
/// Conversión de rendimiento a la unidad canónica (MVP-402, RN-013/RN-014/RN-016).
///
/// La densidad por defecto del aceite de oliva es <b>0,92 kg/L</b> (RN-016). El override por almazara
/// que esa misma regla contempla queda fuera del MVP —no existe la entidad almazara— y se registra en
/// `MVP-999` (`P-061`): mientras tanto, la constante es única y explícita en un solo sitio, de modo que
/// el día que se parametrice solo cambie de origen, no de fórmula.
/// </summary>
public static class HarvestYieldConversion
{
    /// <summary>Densidad por defecto del aceite de oliva, en kg/L (RN-016).</summary>
    public const decimal DefaultOilDensityKgPerLitre = 0.92m;

    /// <summary>
    /// Lleva un rendimiento informado a la unidad canónica L/100kg. Los kilos de aceite por cada 100 kg
    /// se dividen por la densidad: 20 kg/100kg son 21,74 L/100kg.
    /// </summary>
    public static decimal ToCanonical(decimal value, string? unit)
        => unit == HarvestYieldUnits.Kg100Kg
            ? decimal.Round(value / DefaultOilDensityKgPerLitre, 4, MidpointRounding.AwayFromZero)
            : value;

    /// <summary>
    /// RN-014 (3) — Rendimiento <b>derivado</b> de los kilos recolectados y los litros obtenidos.
    /// Devuelve <c>null</c> cuando falta cualquiera de los dos: no se inventa un rendimiento a partir
    /// de datos incompletos.
    /// </summary>
    public static decimal? FromLitres(decimal kgs, decimal? litres)
        => litres is { } value && kgs > 0
            ? decimal.Round(value / kgs * 100m, 4, MidpointRounding.AwayFromZero)
            : null;
}
