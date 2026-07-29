namespace Terrenario.Api.Application.Diary;

/// <summary>
/// Tipo de entrada del diario (MVP-305). Catálogo cerrado <c>diary_entry_type</c>; sus valores son
/// vocabulario de dominio y van en español (ADR-0009).
///
/// <c>cosecha</c> **no está todavía**: <c>HARVEST</c> no existe hasta <c>MVP-004</c>. RN-033 define
/// el diario como la mezcla de actividades, cosechas y compras/consumos, así que encenderla es
/// alcance de <c>MVP-401</c> (hallazgo <c>G-4</c>), no una omisión de esta historia. La vista está
/// construida para que añadirla sea una entrada más aquí y un icono más en el cliente.
/// </summary>
public static class DiaryEntryTypes
{
    public const string Activity = "actividad";
    public const string Purchase = "compra";
    public const string Consumption = "consumo";

    /// <summary>Reservado para <c>MVP-401</c>; todavía no se emite.</summary>
    public const string Harvest = "cosecha";

    public static readonly IReadOnlySet<string> Supported =
        new HashSet<string> { Activity, Purchase, Consumption };

    public static bool IsSupported(string? value) => value is not null && Supported.Contains(value);
}

/// <summary>
/// Entrada del diario cronológico unificado (MVP-305, RN-033). Es una **vista de lectura**: no hay
/// entidad «entrada de diario», sino la proyección común de las tres entidades operativas a lo que el
/// muro necesita mostrar.
///
/// Los campos comunes son los que todas comparten —tipo, fecha de negocio, terreno, temporada, coste
/// y versión—; el resto viaja en los opcionales, que cada tipo rellena si le aplican. Así el cliente
/// pinta una tarjeta y no tres, y añadir la cosecha en <c>MVP-401</c> no obliga a rehacerla.
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
    bool? HasPurchase = null);
