namespace Terrenario.Api.Domain.Diary;

/// <summary>
/// MVP-506 — Fila cruda del diario unificado, tal y como sale de la base de datos.
///
/// Es la <b>forma común</b> a la que se proyectan las cuatro entidades operativas para poder unirlas
/// con <c>UNION ALL</c>. Los campos que solo aplican a un tipo viajan como nulos en los demás: es el
/// precio de que la mezcla la resuelva SQL, y a cambio se puede ordenar, paginar y contar sobre el
/// conjunto real en vez de sobre cuatro listas ya materializadas.
///
/// No es la entrada que ve el cliente (<c>DiaryEntry</c>): aquí viaja además el rango de la temporada,
/// que solo sirve para derivar el aviso de RN-023 sobre la página ya traída.
/// </summary>
/// <remarks>
/// Propiedades con inicializador de objeto y no parámetros de constructor: EF Core no sabe aplicar
/// una operación de conjunto (<c>UNION</c>) sobre una proyección construida con constructor —la trata
/// como proyección de cliente y falla con «Unable to translate set operation after client projection
/// has been applied»—. Con asignaciones de miembro sí puede empujarlas a la proyección SQL, que es
/// justo lo que esta historia necesita.
/// </remarks>
public sealed class DiaryRow
{
    public required string Type { get; init; }
    public required Guid Id { get; init; }
    public required DateOnly Date { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public Guid? PlotId { get; init; }
    public string? PlotName { get; init; }
    public required Guid SeasonId { get; init; }
    public required string SeasonName { get; init; }
    public required DateOnly SeasonStartDate { get; init; }
    public DateOnly? SeasonEndDate { get; init; }
    public required decimal Cost { get; init; }
    public required long Version { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? WorkerName { get; init; }
    public decimal? Hours { get; init; }
    public Guid? TaskId { get; init; }
    public decimal? Quantity { get; init; }
    public bool? HasPurchase { get; init; }

    /// <summary>
    /// MVP-708 (RN-043) — Fecha de la compra imputada; solo en consumos que cuelgan de una. El aviso
    /// se deriva de ella sobre la página ya traída, igual que el de RN-023 se deriva del rango de la
    /// temporada: son datos de contexto, no columnas del recurso.
    /// </summary>
    public DateOnly? PurchaseDate { get; init; }
    public decimal? Kgs { get; init; }
    public string? Destination { get; init; }
    public decimal? Yield { get; init; }

    /// <summary>
    /// MVP-707 — Importe <b>ingresado</b> por la partida (<c>kilos × precio</c>), solo en cosechas.
    /// Va aparte de <see cref="Cost"/> a propósito: mezclar ingreso y gasto en la misma columna
    /// obligaría a un signo, y un signo es una convención que cada consumidor puede leer al revés.
    /// <c>null</c> es «sin precio», no cero.
    /// </summary>
    public decimal? Amount { get; init; }
}

/// <summary>
/// Filtros del diario. Todos se resuelven <b>en servidor</b> desde MVP-506: antes la búsqueda por
/// texto era local sobre lo ya traído, lo que dejaba de ser correcto en cuanto hubiera paginación
/// (`P-052`: buscar sobre una página no es buscar).
/// </summary>
public sealed record DiaryFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? PlotId = null,
    Guid? SeasonId = null,
    /// <summary>Tipos a incluir; vacío ⇒ todos (catálogo <c>diary_entry_type</c>).</summary>
    IReadOnlyCollection<string>? Types = null,
    /// <summary>
    /// MVP-506 (`P-056`) — Responsable de la labor. Solo las actividades tienen responsable, así que
    /// filtrar por él deja fuera compras, consumos y cosechas <b>por definición</b>, igual que filtrar
    /// por terreno deja fuera las compras.
    /// </summary>
    Guid? WorkerId = null,
    /// <summary>Búsqueda por texto sobre titular, terreno, responsable y descripción.</summary>
    string? Search = null,
    /// <summary>
    /// MVP-707 — Varios terrenos a la vez. Lo necesita la lectura económica del dashboard, cuyo filtro
    /// de terrenos es múltiple (<c>plot_ids</c>, MVP-405) mientras que el del diario es de uno. Acota
    /// igual que <see cref="PlotId"/>: donde hay terreno, restringe; donde no lo hay —la compra—, deja
    /// la fuente fuera. Va al final del registro para no desplazar a los llamadores posicionales.
    /// </summary>
    IReadOnlyCollection<Guid>? PlotIds = null);

/// <summary>Página solicitada, con el patrón de paginación de <c>contratos-api.md</c>.</summary>
public sealed record DiaryPageRequest(int Page, int Limit)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;

    public int Skip => (Page - 1) * Limit;
}

/// <summary>
/// Totales del diario <b>completo filtrado</b>, no de la página. Es lo que sostiene la cabecera: un
/// resumen que solo contara la página visible sería una cifra distinta en cada scroll.
/// </summary>
public sealed record DiaryTotals(
    int Total,
    int Activities,
    int Purchases,
    int Consumptions,
    int Harvests,
    decimal TotalKg,
    /// <summary>
    /// Gasto real: labores + compras + consumos <b>sin compra previa</b>. Las imputaciones quedan
    /// fuera a propósito (hallazgo <c>R-01</c> de <c>MVP-399</c>): reparten dinero que la compra ya
    /// aportó, así que sumarlas contaría el mismo gasto dos veces.
    /// </summary>
    decimal TotalCost,
    /// <summary>Lo repartido por terrenos: desglose de <see cref="TotalCost"/>, no gasto añadido.</summary>
    decimal ImputedCost,
    int ConsumptionsWithoutPurchase,
    /// <summary>
    /// MVP-707 — Ingreso de lo filtrado: la suma de <c>kilos × precio</c> de las cosechas que tienen
    /// precio. <c>null</c> cuando **ninguna** lo tiene, que no es lo mismo que cero (CA-5): una
    /// campaña sin precios registrados no ha ingresado 0 €, es que no se sabe.
    /// </summary>
    decimal? TotalIncome,
    /// <summary>Cuántas cosechas de lo filtrado llevan precio, para poder decir sobre cuántas se suma.</summary>
    int HarvestsWithPrice);

/// <summary>
/// MVP-506 — Puerto de lectura del diario unificado.
///
/// Sustituye a la mezcla en memoria de MVP-305, que reutilizaba los cuatro puertos operativos. Aquella
/// era equivalente <b>mientras no hubiera paginación</b> —en los dos casos se traían todas las filas
/// del rango— pero deja de serlo en cuanto la hay: paginar sobre tres listas ya materializadas no es
/// paginar (`P-051`).
/// </summary>
public interface IDiaryRepository
{
    /// <summary>Una página del muro, ordenada por fecha de negocio y desempatada por fecha de captura.</summary>
    Task<IReadOnlyList<DiaryRow>> ListPageAsync(
        Guid workspaceId,
        DiaryFilter filter,
        DiaryPageRequest page,
        CancellationToken ct = default);

    /// <summary>Totales del conjunto filtrado completo, en una sola consulta agregada.</summary>
    Task<DiaryTotals> GetTotalsAsync(
        Guid workspaceId,
        DiaryFilter filter,
        CancellationToken ct = default);
}
