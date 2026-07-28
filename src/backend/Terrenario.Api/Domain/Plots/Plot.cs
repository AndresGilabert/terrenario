using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Domain.Plots;

/// <summary>
/// Terreno (parcela) de un Workspace. Es la unidad base de todo registro operativo del MVP: toda
/// actividad, cosecha y consumo se asocia a un terreno (RN-001).
///
/// MVP-202 implementa el maestro con <b>alta mínima</b> (RN-028): solo <see cref="Name"/> y
/// <see cref="OwnershipType"/> son obligatorios; el resto de campos (alias, propietario, referencia
/// catastral, ubicación y número de árboles) son opcionales e informativos y pueden completarse
/// después sin bloquear el uso del terreno (CA-1/CA-2). La ausencia de <see cref="TreeCount"/> no
/// bloquea nada aquí; se tratará como dato incompleto en el dashboard (RN-010).
///
/// La inactivación (CA-3) es un cambio de estado reversible sobre <see cref="IsActive"/> (convención
/// <c>is_</c> del modelo de datos), no un borrado: preserva la integridad de los registros históricos
/// que referencian el terreno.
/// </summary>
public sealed class Plot
{
    public const int NameMaxLength = 150;
    public const int AliasMaxLength = 60;
    public const int OwnerNameMaxLength = 150;
    public const int CadastralReferenceMaxLength = 50;
    public const int LocationMaxLength = 200;

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string OwnershipType { get; private set; } = string.Empty;
    public string? Alias { get; private set; }
    public string? OwnerName { get; private set; }
    public string? CadastralReference { get; private set; }
    public string? Location { get; private set; }
    public int? TreeCount { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Plot() { }

    /// <summary>
    /// Da de alta un terreno con los datos mínimos (RN-028). Nace activo. Los campos opcionales se
    /// normalizan (recorte de espacios; cadena vacía ≡ ausente) y se validan longitudes y rango.
    /// </summary>
    public static Plot Create(
        Guid workspaceId,
        string name,
        string ownershipType,
        string? alias = null,
        string? ownerName = null,
        string? cadastralReference = null,
        string? location = null,
        int? treeCount = null)
    {
        if (workspaceId == Guid.Empty)
            throw new PlotValidationException(
                ErrorCodes.ValidationRequiredPlotWorkspace,
                "El terreno necesita un Workspace válido.");

        var now = DateTimeOffset.UtcNow;

        var plot = new Plot
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        plot.Apply(name, ownershipType, alias, ownerName, cadastralReference, location, treeCount);

        return plot;
    }

    /// <summary>
    /// Actualiza los datos descriptivos del terreno (edición, MVP-202 CA-2). No cambia el estado de
    /// actividad: para eso está <see cref="SetActive"/>.
    /// </summary>
    public void Update(
        string name,
        string ownershipType,
        string? alias,
        string? ownerName,
        string? cadastralReference,
        string? location,
        int? treeCount)
    {
        Apply(name, ownershipType, alias, ownerName, cadastralReference, location, treeCount);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Activa o inactiva el terreno (CA-3). La inactivación es reversible y no borra datos: los
    /// registros históricos que lo referencian siguen siendo válidos.
    /// </summary>
    public void SetActive(bool isActive)
    {
        if (IsActive == isActive) return;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void Apply(
        string name,
        string ownershipType,
        string? alias,
        string? ownerName,
        string? cadastralReference,
        string? location,
        int? treeCount)
    {
        Name = NormalizeName(name);

        var normalizedOwnership = (ownershipType ?? string.Empty).Trim();
        if (normalizedOwnership.Length == 0)
            throw new PlotValidationException(
                ErrorCodes.ValidationRequiredPlotOwnershipType,
                "El tipo de propiedad del terreno es obligatorio.");
        if (!PlotOwnershipTypes.IsValid(normalizedOwnership))
            throw new PlotValidationException(
                ErrorCodes.ValidationPlotOwnershipTypeInvalid,
                "El tipo de propiedad debe ser 'propia' o 'cedida'.");
        OwnershipType = normalizedOwnership;

        Alias = NormalizeOptional(
            alias, AliasMaxLength,
            ErrorCodes.ValidationPlotAliasLength, $"El alias no puede superar {AliasMaxLength} caracteres.");
        OwnerName = NormalizeOptional(
            ownerName, OwnerNameMaxLength,
            ErrorCodes.ValidationPlotOwnerNameLength, $"El propietario no puede superar {OwnerNameMaxLength} caracteres.");
        CadastralReference = NormalizeOptional(
            cadastralReference, CadastralReferenceMaxLength,
            ErrorCodes.ValidationPlotCadastralLength, $"La referencia catastral no puede superar {CadastralReferenceMaxLength} caracteres.");
        Location = NormalizeOptional(
            location, LocationMaxLength,
            ErrorCodes.ValidationPlotLocationLength, $"La ubicación no puede superar {LocationMaxLength} caracteres.");

        if (treeCount is < 0)
            throw new PlotValidationException(
                ErrorCodes.ValidationRangeTreeCount,
                "El número de árboles no puede ser negativo.");
        TreeCount = treeCount;
    }

    /// <summary>
    /// Normaliza y valida el nombre del terreno <b>sin mutar</b> ningún agregado. Se expone para que
    /// la comprobación de duplicados del maestro (MVP-207, CA-2) trabaje sobre el mismo texto que
    /// acabará persistido (mismo recorte de espacios) y pueda hacerse antes de tocar la entidad.
    /// </summary>
    public static string NormalizeName(string name) => NormalizeRequired(
        name, NameMaxLength,
        ErrorCodes.ValidationRequiredName, "El nombre del terreno es obligatorio.",
        ErrorCodes.ValidationPlotNameLength, $"El nombre del terreno no puede superar {NameMaxLength} caracteres.");

    private static string NormalizeRequired(
        string? value, int maxLength,
        string requiredCode, string requiredMessage,
        string lengthCode, string lengthMessage)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0)
            throw new PlotValidationException(requiredCode, requiredMessage);
        if (normalized.Length > maxLength)
            throw new PlotValidationException(lengthCode, lengthMessage);
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string lengthCode, string lengthMessage)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0) return null;
        if (normalized.Length > maxLength)
            throw new PlotValidationException(lengthCode, lengthMessage);
        return normalized;
    }
}
