namespace Terrenario.Api.Application.Diary;

/// <summary>
/// Tipo de entrada del diario (MVP-305). Catálogo cerrado <c>diary_entry_type</c>; sus valores son
/// vocabulario de dominio y van en español (ADR-0009).
///
/// <c>cosecha</c> se **enciende en MVP-401**, que es quien crea <c>HARVEST</c>: RN-033 define el
/// diario como la mezcla de actividades, cosechas y compras/consumos, así que hasta entonces la vista
/// principal estaba incompleta por construcción (hallazgo <c>G-4</c>). Con los cuatro valores vivos,
/// <c>RN-033</c> queda cumplida entera.
/// </summary>
public static class DiaryEntryTypes
{
    public const string Activity = "actividad";
    public const string Purchase = "compra";
    public const string Consumption = "consumo";

    /// <summary>Cosecha (MVP-401): el cuarto tipo, el que completa RN-033.</summary>
    public const string Harvest = "cosecha";

    public static readonly IReadOnlySet<string> Supported =
        new HashSet<string> { Activity, Purchase, Consumption, Harvest };

    public static bool IsSupported(string? value) => value is not null && Supported.Contains(value);
}

/// <summary>
/// Entrada del diario cronológico unificado (MVP-305, RN-033). Es una **vista de lectura**: no hay
/// entidad «entrada de diario», sino la proyección común de las cuatro entidades operativas a lo que
/// el muro necesita mostrar.
///
/// Los campos comunes son los que todas comparten —tipo, fecha de negocio, terreno, temporada, coste
/// y versión—; el resto viaja en los opcionales, que cada tipo rellena si le aplican. Así el cliente
/// pinta una tarjeta y no cuatro: añadir la cosecha en <c>MVP-401</c> no obligó a rehacerla, solo a
/// sumar <see cref="Kgs"/> y <see cref="Destination"/> a los opcionales.
///
/// <see cref="Version"/> viaja porque el borrado desde el diario exige <c>If-Match</c> (ADR-0005):
/// sin ella el usuario tendría que abrir el registro solo para poder eliminarlo.
/// </summary>
public sealed record DiaryEntry(
    string Type,
    Guid Id,
    /// <summary>Fecha de **negocio**: la que ordena el diario (RN-033), no la de captura.</summary>
    DateOnly Date,
    /// <summary>Titular de la tarjeta: la tarea, el material comprado o el material consumido.</summary>
    string Title,
    string? Description,
    Guid? PlotId,
    string? PlotName,
    Guid SeasonId,
    string SeasonName,
    /// <summary>Coste del registro. <c>0</c> en un consumo sin compra previa, donde además es desconocido.</summary>
    decimal Cost,
    long Version,
    bool IsOutOfSeasonRange,
    DateTimeOffset CreatedAt,
    /// <summary>Solo en actividades.</summary>
    string? WorkerName = null,
    decimal? Hours = null,
    /// <summary>
    /// Solo en actividades: tarea del catálogo, o <c>null</c> si se escribió a mano. El diario lo
    /// necesita para ofrecer guardarla en el catálogo (MVP-302) solo cuando tiene sentido.
    /// </summary>
    Guid? TaskId = null,
    /// <summary>Solo en compras y consumos.</summary>
    decimal? Quantity = null,
    /// <summary>Solo en consumos: <c>false</c> ⇒ el coste es desconocido, no cero (RN-032).</summary>
    bool? HasPurchase = null,
    /// <summary>
    /// Solo en cosechas (MVP-401): kilos recolectados. No se reutiliza <see cref="Quantity"/> porque
    /// no es la misma magnitud —allí es cantidad de material comprado o consumido, sin unidad fija— y
    /// mezclarlas obligaría a la tarjeta a adivinar cómo rotularla.
    /// </summary>
    decimal? Kgs = null,
    /// <summary>Solo en cosechas: destino de lo recolectado (RN-012).</summary>
    string? Destination = null,
    /// <summary>
    /// Solo en cosechas (MVP-402): rendimiento en la unidad canónica L/100kg (RN-013), sea informado o
    /// derivado de los litros obtenidos (RN-014). <c>null</c> cuando la partida todavía no lo declara.
    /// </summary>
    decimal? Yield = null,
    /// <summary>
    /// Solo en cosechas (MVP-707): importe ingresado, <c>kilos × precio</c>. <c>null</c> cuando la
    /// partida no tiene precio, que no es lo mismo que 0 €.
    /// </summary>
    decimal? Amount = null);
