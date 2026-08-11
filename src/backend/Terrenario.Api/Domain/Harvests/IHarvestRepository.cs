namespace Terrenario.Api.Domain.Harvests;

/// <summary>
/// Puerto de persistencia de cosechas (MVP-401).
///
/// <b>Todas</b> las lecturas excluyen las cosechas eliminadas lógicamente (RN-037): el filtro de baja
/// lógica vive en el puerto, no en un filtro global de EF, siguiendo la misma decisión que tomaron
/// <c>IWorkspaceRepository</c> (MVP-206) y <c>IActivityRepository</c> (MVP-301). Así el listado, el
/// diario y el dashboard heredan el comportamiento sin repetirlo.
/// </summary>
public interface IHarvestRepository
{
    Task AddAsync(Harvest harvest, CancellationToken ct = default);

    /// <summary>
    /// Cosecha viva por id dentro del Workspace activo. Devuelve <c>null</c> si no existe, si
    /// pertenece a otro Workspace o si ya está eliminada (el borde de transporte responde 404 en los
    /// tres casos, sin revelar recursos ajenos).
    /// </summary>
    Task<Harvest?> FindByIdAsync(Guid workspaceId, Guid harvestId, CancellationToken ct = default);

    /// <summary>
    /// Cosechas vivas del Workspace con el terreno y la temporada ya resueltos. Filtros alineados con
    /// <c>GET /api/v1/harvests</c>. Orden: fecha de negocio descendente (RN-033) y, a igualdad de
    /// fecha, por fecha de captura descendente.
    /// </summary>
    Task<IReadOnlyList<HarvestView>> ListAsync(
        Guid workspaceId,
        HarvestFilter filter,
        CancellationToken ct = default);

    /// <summary>Misma proyección que el listado, para un único registro (respuestas de alta y edición).</summary>
    Task<HarvestView?> GetViewAsync(Guid workspaceId, Guid harvestId, CancellationToken ct = default);

    /// <summary>
    /// MVP-403 — Filas mínimas para agregar el dashboard: solo las columnas que suman, sin <c>JOIN</c>
    /// a los maestros. Excluye las eliminadas (RN-037), como todas las lecturas de este puerto.
    ///
    /// <b>Una sola lectura para los cuatro widgets.</b> La KB exige que resumen, gráficos y detalle no
    /// se contradigan entre sí; con una única consulta lo cumplen <i>por construcción</i>, porque todos
    /// agregan sobre el mismo conjunto de filas y no sobre cuatro consultas que podrían intercalarse con
    /// una escritura. Y la agregación queda detrás de este método, así que moverla a <c>GROUP BY</c> en
    /// SQL —la evolución que ya prevé <c>ADR-0004</c> para consultas analíticas— no toca a los llamantes.
    /// </summary>
    Task<IReadOnlyList<HarvestAggregateRow>> ListAggregateRowsAsync(
        Guid workspaceId,
        HarvestAggregateFilter filter,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Ámbito de agregación del dashboard (MVP-403): temporada y conjunto de terrenos. A diferencia de
/// <see cref="HarvestFilter"/> admite <b>varios</b> terrenos, que es como filtra el dashboard (RN-008).
/// <c>null</c> o vacío significa «sin restringir por esa dimensión»: los valores por defecto los
/// resuelve el caso de uso, no el puerto.
/// </summary>
public sealed record HarvestAggregateFilter(
    Guid? SeasonId = null,
    IReadOnlyCollection<Guid>? PlotIds = null);

/// <summary>
/// Fila mínima de agregación (MVP-403/MVP-404): lo justo para sumar kilos, litros y rendimiento por
/// terreno, destino, temporada y <b>periodo</b>. No lleva nombres resueltos porque quien agrupa ya
/// tiene los maestros cargados. <see cref="Date"/> se añadió en MVP-404 para poder agrupar la evolución
/// de rendimiento por mes o semana.
/// </summary>
public sealed record HarvestAggregateRow(
    Guid PlotId,
    Guid SeasonId,
    DateOnly Date,
    decimal Kgs,
    decimal? Yield,
    decimal? Liters,
    string Destination)
{
    /// <summary>
    /// Rendimiento en la unidad canónica L/100kg (RN-013), declarado o derivado de los litros
    /// (RN-014). Misma regla que <see cref="HarvestView.EffectiveYield"/>: el dato no cambia de
    /// significado según quién lo lea.
    /// </summary>
    public decimal? EffectiveYield => Yield ?? HarvestYieldConversion.FromLitres(Kgs, Liters);

    /// <summary>
    /// Litros de aceite de la partida, declarados o derivados del rendimiento. Es la cara simétrica de
    /// <see cref="EffectiveYield"/>: RN-004 obliga a informar uno de los dos, y el resumen de temporada
    /// necesita los litros totales «cuando exista dato», no solo cuando se escribieran litros.
    /// </summary>
    public decimal? EffectiveLiters =>
        Liters ?? (Yield is { } yield ? decimal.Round(yield / 100m * Kgs, 2, MidpointRounding.AwayFromZero) : null);

    /// <summary>¿La partida aporta algún dato de aceite? Es lo que decide si entra en los promedios.</summary>
    public bool HasOilData => Yield is not null || Liters is not null;
}

/// <summary>
/// Filtros del listado de cosechas (<c>from</c>, <c>to</c>, terreno, temporada, destino, producto).
///
/// <c>Product</c> lo añade MVP-805: es lo que faltaba para poder preguntar «¿ya hay una partida de este
/// terreno, esta fecha y este producto?», que es la comparación con la que RN-044 avisa de un duplicado.
/// Se añade al filtro que ya existe en vez de abrir una consulta paralela: son las mismas columnas y el
/// mismo conjunto vivo, y dos caminos de lectura sobre lo mismo acaban divergiendo.
/// </summary>
public sealed record HarvestFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? PlotId = null,
    Guid? SeasonId = null,
    string? Destination = null,
    string? Product = null);

/// <summary>
/// Vista de lectura de una cosecha con los nombres de terreno y temporada resueltos, para que el
/// listado y el diario no tengan que pedir los maestros por separado. Incluye el rango de la temporada
/// para poder señalar la fecha fuera de rango (RN-023) sin una consulta adicional del cliente.
/// </summary>
public sealed record HarvestView(
    Guid Id,
    Guid WorkspaceId,
    Guid PlotId,
    string PlotName,
    Guid SeasonId,
    string SeasonName,
    DateOnly SeasonStartDate,
    DateOnly? SeasonEndDate,
    DateOnly Date,
    string Product,
    decimal Kgs,
    decimal? Yield,
    decimal? Liters,
    string Destination,
    decimal? UnitPrice,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// RN-023 — la fecha cae fuera del rango de la temporada asociada. Es un <b>aviso</b>, nunca un
    /// bloqueo: se calcula en lectura para que la UI pueda marcarlo también en registros antiguos,
    /// aunque la temporada se haya editado después.
    /// </summary>
    public bool IsOutOfSeasonRange =>
        Date < SeasonStartDate || (SeasonEndDate is { } end && Date > end);

    /// <summary>
    /// MVP-402 — Rendimiento en la unidad canónica L/100kg (RN-013) <b>venga de donde venga</b>: el
    /// valor informado, o el derivado de los litros obtenidos y los kilos recolectados cuando lo que
    /// se declaró fueron litros (RN-014, tercer origen).
    ///
    /// Es lo que hace que RN-004 —rendimiento y litros excluyentes— no cueste información: una cosecha
    /// que declaró litros tiene rendimiento igualmente, y el dashboard puede promediarla sin que cada
    /// consumidor rehaga la división.
    /// </summary>
    public decimal? EffectiveYield => Yield ?? HarvestYieldConversion.FromLitres(Kgs, Liters);

    /// <summary>
    /// De dónde sale <see cref="EffectiveYield"/>: <c>informado</c>, <c>calculado</c> o <c>null</c> si
    /// no hay dato. La UI lo necesita para no presentar como declarado un valor que se ha deducido.
    /// </summary>
    public string? YieldSource =>
        Yield is not null ? "informado" : EffectiveYield is not null ? "calculado" : null;

    /// <summary>
    /// MVP-707 — Importe de la partida: <c>kilos × precio</c>, o <c>null</c> si no hay precio. Se
    /// deriva aquí igual que en el agregado: guardarlo permitiría que divergiera de sus dos factores
    /// tras una corrección, y entonces habría dos verdades sobre lo mismo (CA-3).
    ///
    /// <c>null</c> es «no se sabe», no cero: una partida sin precio no ha ingresado 0 €.
    /// </summary>
    public decimal? Amount =>
        UnitPrice is null ? null : decimal.Round(Kgs * UnitPrice.Value, 2, MidpointRounding.AwayFromZero);
}
