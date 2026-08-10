using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Domain.Purchases;

/// <summary>
/// Compra de material del Workspace (MVP-303). Es la contrapartida de gasto de la operativa diaria y
/// la base de la imputación por terreno que entrega <c>MVP-304</c>.
///
/// Reglas de negocio que materializa:
/// <list type="bullet">
/// <item>RN-031 — el producto o material se registra en <b>texto libre</b>: no hay catálogo cerrado.
/// La UI sugiere valores del histórico del Workspace para acelerar la captura y dar consistencia sin
/// imponerla.</item>
/// <item>RN-021 — toda compra queda asociada a una temporada (<see cref="SeasonId"/>). Cierra
/// <c>P-050</c>: el contrato ya lo exigía y el ER no lo declaraba.</item>
/// <item>RN-023 — una fecha fuera del rango de la temporada <b>no bloquea</b>; se avisa en lectura,
/// igual que en la actividad.</item>
/// <item>RN-037 — la eliminación es <b>lógica</b> (<see cref="DeletedAt"/>).</item>
/// </list>
///
/// <b>Concurrencia optimista</b> (ADR-0005): mismo patrón que estrenó <c>ACTIVITY</c> en MVP-301
/// —<see cref="Version"/> desde 1, incrementada en cada mutación—.
/// </summary>
public sealed class Purchase
{
    public const int ProductMaxLength = 150;

    /// <summary>Cota de <c>decimal(10,2)</c> para cantidad y coste totales.</summary>
    public const decimal AmountMax = 99_999_999.99m;

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid SeasonId { get; private set; }

    /// <summary>Fecha de negocio de la compra. Es la que ordena el diario (RN-033), no <see cref="CreatedAt"/>.</summary>
    public DateOnly PurchaseDate { get; private set; }

    /// <summary>Material comprado, en texto libre (RN-031).</summary>
    public string Product { get; private set; } = string.Empty;

    public decimal TotalQuantity { get; private set; }
    public decimal TotalCost { get; private set; }

    /// <summary>
    /// Precio unitario, derivado de <see cref="TotalCost"/> / <see cref="TotalQuantity"/> y
    /// <b>persistido</b> para trazabilidad (así lo fija el ER). Es lo que <c>MVP-304</c> usará para
    /// calcular el coste proporcional de cada imputación, y guardarlo garantiza que una imputación
    /// vieja pueda explicarse aunque la compra se edite después.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid UpdatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public long Version { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    private Purchase() { }

    /// <summary>Registra una compra con el mínimo que exige la KB (HU-1, CA-1).</summary>
    public static Purchase Create(
        Guid workspaceId,
        Guid seasonId,
        DateOnly purchaseDate,
        string product,
        decimal totalQuantity,
        decimal totalCost,
        Guid userId)
    {
        if (workspaceId == Guid.Empty)
            throw new PurchaseValidationException(
                ErrorCodes.ValidationPurchaseRequiredFields,
                "La compra necesita un Workspace válido.");

        var now = DateTimeOffset.UtcNow;

        var purchase = new Purchase
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            CreatedBy = userId,
            CreatedAt = now,
            UpdatedBy = userId,
            UpdatedAt = now,
            Version = 1
        };

        purchase.Apply(seasonId, purchaseDate, product, totalQuantity, totalCost);

        return purchase;
    }

    /// <summary>Corrige una compra ya registrada. Incrementa <see cref="Version"/> (ADR-0005).</summary>
    public void Update(
        Guid seasonId,
        DateOnly purchaseDate,
        string product,
        decimal totalQuantity,
        decimal totalCost,
        Guid userId)
    {
        Apply(seasonId, purchaseDate, product, totalQuantity, totalCost);
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
    /// MVP-806 — Reapunta la temporada al superviviente de una fusión de maestros. No revalida el
    /// resto del registro: lo único que cambia es la clave ajena. <b>Sí mueve la versión</b>, que es lo
    /// que hace que una edición simultánea reciba <c>409</c> en vez de quedar pisada (ADR-0005).
    /// </summary>
    public void ReassignSeason(Guid seasonId, Guid userId)
    {
        if (seasonId == Guid.Empty)
            throw new PurchaseValidationException(
                ErrorCodes.ValidationPurchaseRequiredFields, "La compra necesita una temporada.");
        if (SeasonId == seasonId) return;

        SeasonId = seasonId;
        Touch(userId);
    }

    /// <summary>Comprueba la versión de <c>If-Match</c> antes de mutar nada (ADR-0005).</summary>
    public void EnsureVersion(long expectedVersion)
    {
        if (expectedVersion == Version) return;

        throw new ConcurrencyConflictException(
            "Otra persona ha modificado esta compra mientras la editabas. Refresca para ver la versión actual.")
        {
            CurrentVersion = Version
        };
    }

    /// <summary>
    /// Normaliza y valida un producto <b>sin mutar</b> el agregado. Se expone para que la búsqueda de
    /// sugerencias y la comparación con el histórico trabajen sobre el mismo texto que se persistiría.
    /// </summary>
    public static string NormalizeProduct(string? product)
    {
        var normalized = (product ?? string.Empty).Trim();
        if (normalized.Length == 0)
            throw new PurchaseValidationException(
                ErrorCodes.ValidationPurchaseRequiredProduct,
                "El producto o material de la compra es obligatorio.");
        if (normalized.Length > ProductMaxLength)
            throw new PurchaseValidationException(
                ErrorCodes.ValidationPurchaseProductLength,
                $"El producto no puede superar {ProductMaxLength} caracteres.");

        return normalized;
    }

    private void Touch(Guid userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    private void Apply(
        Guid seasonId,
        DateOnly purchaseDate,
        string product,
        decimal totalQuantity,
        decimal totalCost)
    {
        // RN-021 — la compra pertenece a una temporada (P-050).
        if (seasonId == Guid.Empty)
            throw new PurchaseValidationException(
                ErrorCodes.ValidationPurchaseRequiredFields,
                "La compra necesita una temporada.");

        Product = NormalizeProduct(product);

        // Cantidad y coste **estrictamente positivos**: una compra de 0 unidades o de 0 € no es una
        // compra, y `total_quantity = 0` haría además indefinido el precio unitario.
        if (totalQuantity <= 0 || totalQuantity > AmountMax || totalCost <= 0 || totalCost > AmountMax)
            throw new PurchaseValidationException(
                ErrorCodes.ValidationPurchaseTotalsRange,
                "La cantidad y el coste de la compra deben ser mayores que 0.");

        SeasonId = seasonId;
        PurchaseDate = purchaseDate;
        TotalQuantity = decimal.Round(totalQuantity, 2, MidpointRounding.AwayFromZero);
        TotalCost = decimal.Round(totalCost, 2, MidpointRounding.AwayFromZero);
        // Se deriva de los valores **ya redondeados** que se van a persistir, para que el precio
        // unitario guardado sea exactamente el que explica la fila y no uno calculado con más cifras.
        UnitPrice = decimal.Round(TotalCost / TotalQuantity, 4, MidpointRounding.AwayFromZero);
    }
}
