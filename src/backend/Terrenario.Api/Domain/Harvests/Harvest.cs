using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Domain.Harvests;

/// <summary>
/// Cosecha registrada en el Workspace (MVP-401). Es la materia prima del dashboard MVP: convierte la
/// recolección real en kilos, destino y rendimiento comparables entre temporadas.
///
/// Reglas de negocio que materializa:
/// <list type="bullet">
/// <item>RN-001 — toda cosecha va asociada a un terreno (<see cref="PlotId"/>).</item>
/// <item>RN-004 — <see cref="Kgs"/> obligatorio; <see cref="Yield"/> y <see cref="Liters"/> son
/// opcionales pero <b>no pueden coexistir</b> en el mismo registro.</item>
/// <item>RN-021 — temporada obligatoria (<see cref="SeasonId"/>); la UI autoselecciona la activa.</item>
/// <item>RN-023 — una fecha fuera del rango de la temporada <b>no bloquea</b>: se avisa y se guarda.
/// Por eso el agregado no valida el rango; lo calcula <c>IsOutOfSeasonRange</c> en la lectura.</item>
/// <item>RN-029 — el alcance MVP se limita a producto, kilos, destino y uno entre rendimiento o
/// litros: aquí no hay precio, molturación ni balance.</item>
/// <item>RN-037 — la eliminación es <b>lógica</b> (<see cref="DeletedAt"/>), con confirmación
/// explícita en la UI.</item>
/// </list>
///
/// <b>Concurrencia optimista</b> (ADR-0005): <see cref="Version"/> arranca en 1 y se incrementa en
/// cada mutación. No se reinventa el patrón: es el mismo que estrenó <c>ACTIVITY</c> en MVP-301 y que
/// ya reutilizan compras (MVP-303) y consumos (MVP-304).
///
/// <b>MVP-402 cierra la semántica de producción</b>: <see cref="Product"/> y
/// <see cref="Destination"/> se validan contra los catálogos cerrados (<see cref="HarvestProducts"/>,
/// <see cref="HarvestDestinations"/>) y <see cref="Yield"/> se guarda siempre en la unidad canónica
/// L/100kg (RN-013), sea cual sea la unidad en la que se informó (RN-014/RN-016). Lo que llega aquí ya
/// está convertido: el agregado no conoce unidades de entrada, solo la canónica.
/// </summary>
public sealed class Harvest
{
    /// <summary>Código de catálogo, no texto libre: la cota es holgada para el valor más largo previsto.</summary>
    public const int ProductMaxLength = 60;

    public const int DestinationMaxLength = 30;

    /// <summary>Cota de <c>decimal(10,2)</c> de los kilos recolectados y de los litros obtenidos.</summary>
    public const decimal KgsMax = 99_999_999.99m;

    public const decimal LitersMax = 99_999_999.99m;

    /// <summary>
    /// Cota superior del rendimiento en la unidad canónica L/100kg (RN-013). Cien litros por cada cien
    /// kilos de aceituna ya es físicamente imposible —no puede salir más aceite que fruto—, así que un
    /// valor por encima es siempre un error de tecleo, no una campaña excepcional.
    /// </summary>
    public const decimal YieldMax = 100m;

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid PlotId { get; private set; }
    public Guid SeasonId { get; private set; }

    /// <summary>Fecha de negocio de la cosecha. Es la que ordena el diario (RN-033), no <see cref="CreatedAt"/>.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Producto recolectado (RN-030). El catálogo cerrado lo aplica MVP-402.</summary>
    public string Product { get; private set; } = string.Empty;

    public decimal Kgs { get; private set; }

    /// <summary>
    /// Rendimiento en la unidad canónica L/100kg (RN-013). Excluyente con <see cref="Liters"/> por
    /// RN-004: informar los dos permitiría que se contradijeran y el dashboard no sabría cuál creer.
    /// </summary>
    public decimal? Yield { get; private set; }

    /// <summary>Litros de aceite obtenidos. Excluyente con <see cref="Yield"/> (RN-004).</summary>
    public decimal? Liters { get; private set; }

    /// <summary>Destino de lo recolectado (RN-012). El catálogo cerrado lo aplica MVP-402.</summary>
    public string Destination { get; private set; } = string.Empty;

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid UpdatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Versión para el bloqueo optimista (<c>If-Match</c>, ADR-0005). Arranca en 1.</summary>
    public long Version { get; private set; }

    /// <summary>Marca de eliminación lógica (RN-037). Nunca hay borrado físico.</summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    private Harvest() { }

    /// <summary>
    /// Da de alta una cosecha (HU-1, CA-1). Los vínculos llegan ya verificados como pertenecientes al
    /// Workspace activo: comprobarlo es responsabilidad del caso de uso, que es quien tiene acceso a
    /// los maestros.
    /// </summary>
    public static Harvest Create(
        Guid workspaceId,
        Guid plotId,
        Guid seasonId,
        DateOnly date,
        string product,
        decimal kgs,
        string destination,
        decimal? yield,
        decimal? liters,
        Guid userId)
    {
        if (workspaceId == Guid.Empty)
            throw new HarvestValidationException(
                ErrorCodes.ValidationHarvestRequiredFields,
                "La cosecha necesita un Workspace válido.");

        var now = DateTimeOffset.UtcNow;

        var harvest = new Harvest
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            CreatedBy = userId,
            CreatedAt = now,
            UpdatedBy = userId,
            UpdatedAt = now,
            Version = 1
        };

        harvest.Apply(plotId, seasonId, date, product, kgs, destination, yield, liters);

        return harvest;
    }

    /// <summary>
    /// Corrige una cosecha ya registrada (HU-2, CA-2). Incrementa <see cref="Version"/>: cualquier
    /// cliente que siga con la versión anterior recibirá <c>409</c> al intentar escribir (CA-5).
    /// </summary>
    public void Update(
        Guid plotId,
        Guid seasonId,
        DateOnly date,
        string product,
        decimal kgs,
        string destination,
        decimal? yield,
        decimal? liters,
        Guid userId)
    {
        Apply(plotId, seasonId, date, product, kgs, destination, yield, liters);
        Touch(userId);
    }

    /// <summary>
    /// Eliminación <b>lógica</b> (RN-037, CA-5): la cosecha desaparece del diario, del listado y del
    /// dashboard, pero no de la base de datos. Es idempotente en el dominio; el caso de uso responde
    /// 404 si ya estaba eliminada, para que no se pueda borrar dos veces lo mismo.
    /// </summary>
    public void Delete(Guid userId)
    {
        if (IsDeleted) return;
        DeletedAt = DateTimeOffset.UtcNow;
        Touch(userId);
    }

    /// <summary>
    /// Comprueba que la versión que trae el cliente es la vigente (ADR-0005). Se llama <b>antes</b> de
    /// mutar nada: el conflicto no debe dejar el agregado a medias.
    /// </summary>
    public void EnsureVersion(long expectedVersion)
    {
        if (expectedVersion == Version) return;

        throw new ConcurrencyConflictException(
            "Otra persona ha modificado esta cosecha mientras la editabas. Refresca para ver la versión actual.")
        {
            CurrentVersion = Version
        };
    }

    private void Touch(Guid userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    private void Apply(
        Guid plotId,
        Guid seasonId,
        DateOnly date,
        string product,
        decimal kgs,
        string destination,
        decimal? yield,
        decimal? liters)
    {
        // RN-001/RN-021 — terreno y temporada son parte del registro mínimo.
        if (plotId == Guid.Empty || seasonId == Guid.Empty)
            throw new HarvestValidationException(
                ErrorCodes.ValidationHarvestRequiredFields,
                "La cosecha necesita terreno y temporada.");

        // RN-030 (MVP-402, CA-1) — producto obligatorio y **dentro del catálogo global fijo**. La
        // comprobación es de pertenencia, no de longitud: es un código gobernado por sistema, no texto
        // libre como el material de compra (RN-031).
        var normalizedProduct = (product ?? string.Empty).Trim();
        if (normalizedProduct.Length == 0)
            throw new HarvestValidationException(
                ErrorCodes.ValidationProductInvalid,
                "La cosecha necesita un producto.");
        if (!HarvestProducts.IsSupported(normalizedProduct))
            throw new HarvestValidationException(
                ErrorCodes.ValidationProductInvalid,
                $"El producto no pertenece al catálogo. Valores admitidos: {string.Join(", ", HarvestProducts.Supported)}.");

        // RN-012 (MVP-402, CA-1/CA-3) — destino obligatorio y dentro de la taxonomía cerrada.
        // `desconocido` es un valor válido, no un hueco: no conocer todavía el cierre comercial no
        // puede retrasar el registro operativo (HU-2).
        var normalizedDestination = (destination ?? string.Empty).Trim();
        if (normalizedDestination.Length == 0)
            throw new HarvestValidationException(
                ErrorCodes.ValidationDestinationInvalid,
                "La cosecha necesita un destino. Usa «desconocido» si todavía no lo sabes.");
        if (!HarvestDestinations.IsSupported(normalizedDestination))
            throw new HarvestValidationException(
                ErrorCodes.ValidationDestinationInvalid,
                $"El destino no pertenece al catálogo. Valores admitidos: {string.Join(", ", HarvestDestinations.Supported)}.");

        // RN-004 — sin kilos no hay cosecha que medir.
        if (kgs <= 0 || kgs > KgsMax)
            throw new HarvestValidationException(
                ErrorCodes.ValidationHarvestKgsRequired,
                "Los kilos deben ser mayores que 0.");

        // RN-004 — rendimiento y litros son opcionales, pero no pueden coexistir: son dos formas de
        // decir lo mismo y guardarlas juntas permitiría que se contradijeran.
        if (yield is not null && liters is not null)
            throw new HarvestValidationException(
                ErrorCodes.ValidationHarvestXorYieldLiters,
                "Indica el rendimiento o los litros obtenidos, pero no los dos: son dos formas de medir lo mismo.");

        if (yield is { } yieldValue && (yieldValue <= 0 || yieldValue > YieldMax))
            throw new HarvestValidationException(
                ErrorCodes.ValidationHarvestYieldRange,
                $"El rendimiento debe ser mayor que 0 y no superar {YieldMax:0.##} L/100kg.");

        if (liters is { } litersValue && (litersValue <= 0 || litersValue > LitersMax))
            throw new HarvestValidationException(
                ErrorCodes.ValidationHarvestLitersRange,
                "Los litros deben ser mayores que 0.");

        PlotId = plotId;
        SeasonId = seasonId;
        Date = date;
        Product = normalizedProduct;
        Destination = normalizedDestination;
        // Se redondea a la precisión persistida para que lo leído coincida con lo escrito, igual que
        // en actividades y compras.
        Kgs = decimal.Round(kgs, 2, MidpointRounding.AwayFromZero);
        Yield = yield is null ? null : decimal.Round(yield.Value, 4, MidpointRounding.AwayFromZero);
        Liters = liters is null ? null : decimal.Round(liters.Value, 2, MidpointRounding.AwayFromZero);
    }
}
