using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Domain.Consumptions;

/// <summary>
/// Consumo de material en un terreno (MVP-304). Cubre los <b>dos</b> casos con una sola entidad,
/// como se decidió en <c>MVP-303</c> antes de cerrar el modelo de compras:
///
/// <list type="bullet">
/// <item><b>Imputación de una compra</b> (<see cref="PurchaseId"/> informado): reparte una compra
/// entre terrenos con cantidad aproximada y coste proporcional (HU-1).</item>
/// <item><b>Consumo sin compra previa</b> (<see cref="PurchaseId"/> nulo, RN-032): la ausencia de
/// compra <b>nunca</b> bloquea el registro; el coste imputado es <c>0</c> y la respuesta lo señala
/// para que la UI avise (HU-2, CA-2).</item>
/// </list>
///
/// Es el <b>mismo hecho</b> en los dos casos —«se han consumido X unidades de Y en el terreno Z el
/// día D, con coste C»—; lo único que cambia es de dónde sale el coste. Por eso no son dos entidades.
///
/// <b>La fila es autoexplicativa</b>: guarda su propio <see cref="Product"/> y su propio
/// <see cref="UnitPrice"/>, copiados de la compra al imputar. Es lo que hace verdadero el CA-3
/// (RN-032, «no se recalculan históricos») <i>por estructura</i> y no por convención: editar o
/// eliminar la compra después no reescribe lo que ya se consumió, y registrar una compra posterior no
/// da coste retroactivo a un consumo que se guardó sin ella.
///
/// Hereda de <c>MVP-301</c> la concurrencia optimista (ADR-0005) y la eliminación lógica (RN-037).
/// </summary>
public sealed class PurchaseConsumption
{
    /// <summary>Misma cota que <c>Purchase.ProductMaxLength</c>: el producto se hereda de la compra.</summary>
    public const int ProductMaxLength = 150;

    public const decimal QuantityMax = 99_999_999.99m;

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Compra de la que sale el material. <b>Anulable</b> por RN-032: se puede registrar consumo sin
    /// compra previa, y entonces el coste imputado es <c>0</c>.
    /// </summary>
    public Guid? PurchaseId { get; private set; }

    public Guid PlotId { get; private set; }

    /// <summary>Temporada del consumo (RN-021). Al imputar se hereda de la compra.</summary>
    public Guid SeasonId { get; private set; }

    /// <summary>
    /// Fecha de negocio del consumo, distinta de <see cref="CreatedAt"/>: el diario ordena por ella
    /// (RN-033, CA-4). Un consumo capturado el lunes sobre trabajo del jueves anterior debe caer en el
    /// jueves.
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>Material consumido. Con compra se hereda de ella; sin compra se escribe (RN-031).</summary>
    public string Product { get; private set; } = string.Empty;

    public decimal ConsumedQuantity { get; private set; }

    /// <summary>
    /// Precio unitario <b>congelado</b> en el momento de imputar, copiado de la compra. <c>0</c> sin
    /// compra previa. Guardarlo aquí es lo que impide que editar la compra reescriba el coste de lo
    /// ya consumido (RN-032).
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Coste proporcional: <see cref="ConsumedQuantity"/> × <see cref="UnitPrice"/>. <c>0</c> sin compra.</summary>
    public decimal ProportionalCost { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid UpdatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public long Version { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    /// <summary>Sin compra detrás el coste es desconocido, no cero de verdad: la UI lo avisa (CA-2).</summary>
    public bool HasPurchase => PurchaseId is not null;

    private PurchaseConsumption() { }

    /// <summary>
    /// Imputa una compra a un terreno (HU-1, CA-1). El producto, la temporada y el precio unitario se
    /// <b>heredan de la compra</b>: el usuario solo decide terreno, fecha y cantidad.
    /// </summary>
    public static PurchaseConsumption ImputeFromPurchase(
        Guid workspaceId,
        Guid purchaseId,
        Guid seasonId,
        string product,
        decimal unitPrice,
        Guid plotId,
        DateOnly date,
        decimal quantity,
        Guid userId)
    {
        if (purchaseId == Guid.Empty)
            throw new ConsumptionValidationException(
                ErrorCodes.ValidationConsumptionRequiredFields, "La imputación necesita una compra válida.");

        var consumption = NewFor(workspaceId, userId);
        consumption.PurchaseId = purchaseId;
        consumption.Apply(seasonId, plotId, date, product, quantity, unitPrice);

        return consumption;
    }

    /// <summary>
    /// Registra un consumo <b>sin compra previa</b> (HU-2, CA-2, RN-032). El coste es <c>0</c> y el
    /// producto y la temporada los aporta el usuario, porque no hay compra de la que heredarlos.
    /// </summary>
    public static PurchaseConsumption RegisterWithoutPurchase(
        Guid workspaceId,
        Guid seasonId,
        Guid plotId,
        DateOnly date,
        string product,
        decimal quantity,
        Guid userId)
    {
        var consumption = NewFor(workspaceId, userId);
        consumption.PurchaseId = null;
        consumption.Apply(seasonId, plotId, date, product, quantity, unitPrice: 0m);

        return consumption;
    }

    /// <summary>
    /// Corrige un consumo. El precio unitario <b>no se toca</b>: sigue siendo el que se congeló al
    /// imputar, así que cambiar la cantidad recalcula el coste con el precio de entonces (RN-032).
    /// </summary>
    public void Update(
        Guid seasonId,
        Guid plotId,
        DateOnly date,
        string product,
        decimal quantity,
        Guid userId)
    {
        Apply(seasonId, plotId, date, product, quantity, UnitPrice);
        Touch(userId);
    }

    /// <summary>Eliminación <b>lógica</b> (RN-037). Idempotente en el dominio.</summary>
    public void Delete(Guid userId)
    {
        if (IsDeleted) return;
        DeletedAt = DateTimeOffset.UtcNow;
        Touch(userId);
    }

    /// <summary>
    /// MVP-806 — Reapunta el terreno al superviviente de una fusión de maestros. Los consumos son la
    /// referencia que el spec señala como fácil de olvidar: el terreno también se referencia desde
    /// aquí, no solo desde actividades y cosechas. <b>Sí mueve la versión</b> (ADR-0005).
    /// </summary>
    public void ReassignPlot(Guid plotId, Guid userId)
    {
        EnsureLink(plotId);
        if (PlotId == plotId) return;

        PlotId = plotId;
        Touch(userId);
    }

    /// <summary>MVP-806 — Reapunta la temporada al superviviente de una fusión. Ver <see cref="ReassignPlot"/>.</summary>
    public void ReassignSeason(Guid seasonId, Guid userId)
    {
        EnsureLink(seasonId);
        if (SeasonId == seasonId) return;

        SeasonId = seasonId;
        Touch(userId);
    }

    private static void EnsureLink(Guid replacement)
    {
        if (replacement == Guid.Empty)
            throw new ConsumptionValidationException(
                ErrorCodes.ValidationConsumptionRequiredFields,
                "El consumo necesita terreno y temporada.");
    }

    /// <summary>Comprueba la versión de <c>If-Match</c> antes de mutar nada (ADR-0005).</summary>
    public void EnsureVersion(long expectedVersion)
    {
        if (expectedVersion == Version) return;

        throw new ConcurrencyConflictException(
            "Otra persona ha modificado este consumo mientras lo editabas. Refresca para ver la versión actual.")
        {
            CurrentVersion = Version
        };
    }

    /// <summary>Normaliza y valida el producto sin mutar el agregado.</summary>
    public static string NormalizeProduct(string? product)
    {
        var normalized = (product ?? string.Empty).Trim();
        if (normalized.Length == 0)
            throw new ConsumptionValidationException(
                ErrorCodes.ValidationConsumptionRequiredProduct,
                "El producto consumido es obligatorio.");
        if (normalized.Length > ProductMaxLength)
            throw new ConsumptionValidationException(
                ErrorCodes.ValidationConsumptionProductLength,
                $"El producto no puede superar {ProductMaxLength} caracteres.");

        return normalized;
    }

    private static PurchaseConsumption NewFor(Guid workspaceId, Guid userId)
    {
        if (workspaceId == Guid.Empty)
            throw new ConsumptionValidationException(
                ErrorCodes.ValidationConsumptionRequiredFields,
                "El consumo necesita un Workspace válido.");

        var now = DateTimeOffset.UtcNow;

        return new PurchaseConsumption
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            CreatedBy = userId,
            CreatedAt = now,
            UpdatedBy = userId,
            UpdatedAt = now,
            Version = 1
        };
    }

    private void Touch(Guid userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    private void Apply(
        Guid seasonId,
        Guid plotId,
        DateOnly date,
        string product,
        decimal quantity,
        decimal unitPrice)
    {
        // RN-001 — todo registro operativo va a un terreno; RN-021 — y a una temporada.
        if (plotId == Guid.Empty || seasonId == Guid.Empty)
            throw new ConsumptionValidationException(
                ErrorCodes.ValidationConsumptionRequiredFields,
                "El consumo necesita terreno y temporada.");

        if (quantity <= 0 || quantity > QuantityMax)
            throw new ConsumptionValidationException(
                ErrorCodes.ValidationConsumptionQuantityRange,
                "La cantidad consumida debe ser mayor que 0.");

        SeasonId = seasonId;
        PlotId = plotId;
        Date = date;
        Product = NormalizeProduct(product);
        ConsumedQuantity = decimal.Round(quantity, 2, MidpointRounding.AwayFromZero);
        UnitPrice = decimal.Round(unitPrice, 4, MidpointRounding.AwayFromZero);
        // Sin compra el coste es 0 (RN-032); con compra, proporcional al precio unitario congelado.
        ProportionalCost = decimal.Round(ConsumedQuantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
    }
}
