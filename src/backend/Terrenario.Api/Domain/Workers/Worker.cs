using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Domain.Workers;

/// <summary>
/// Trabajador del maestro de un Workspace (MVP-204). Es uno de los responsables seleccionables al
/// registrar actividad (RN-002/RN-027). El maestro mantiene <b>trabajadores sin cuenta vinculada</b>
/// (jornaleros, cuadrilla) de forma consistente para evitar nombres duplicados o sueltos (HU-1/HU-2).
///
/// Los miembros del Workspace se exponen automáticamente como seleccionables (RN-027) pero
/// <b>no</b> se materializan como filas de <c>workers</c>: viven en <c>workspace_members</c> y se
/// combinan en la vista de personas del Workspace. Este agregado cubre solo a los trabajadores sin
/// cuenta.
///
/// <see cref="HourlyRate"/> es una tarifa de referencia para sugerir coste más adelante, nunca un
/// automatismo: el coste operativo se sigue registrando a mano (RN-003). Los trabajadores con
/// histórico se <b>inactivan</b> (<see cref="IsActive"/>), no se borran (CA-3), para no invalidar los
/// registros que los referencian.
/// </summary>
public sealed class Worker
{
    public const int NameMaxLength = 150;

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Reservado (modelo de datos canónico): vincula el trabajador a una cuenta del sistema cuando
    /// exista. En MVP-204 no se materializa —los miembros se exponen desde <c>workspace_members</c>,
    /// no como filas de <c>workers</c>— y nace siempre <c>null</c>.
    /// </summary>
    public Guid? UserAccountId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public decimal? HourlyRate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Worker() { }

    /// <summary>
    /// Da de alta un trabajador sin cuenta (CA-2). Solo <see cref="Name"/> es obligatorio; la tarifa
    /// es opcional y de referencia. Nace activo.
    /// </summary>
    public static Worker Create(Guid workspaceId, string name, decimal? hourlyRate = null)
    {
        if (workspaceId == Guid.Empty)
            throw new WorkerValidationException(
                ErrorCodes.ValidationRequiredWorkerWorkspace,
                "El trabajador necesita un Workspace válido.");

        var now = DateTimeOffset.UtcNow;

        var worker = new Worker
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserAccountId = null,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        worker.Apply(name, hourlyRate);

        return worker;
    }

    /// <summary>Actualiza los datos del trabajador (edición, CA-2). No cambia el estado de actividad.</summary>
    public void Update(string name, decimal? hourlyRate)
    {
        Apply(name, hourlyRate);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Activa o inactiva el trabajador (CA-3). La inactivación es reversible y no borra datos: los
    /// registros históricos que lo referencian siguen siendo válidos.
    /// </summary>
    public void SetActive(bool isActive)
    {
        if (IsActive == isActive) return;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Normaliza y valida un nombre de trabajador <b>sin mutar</b> ningún agregado. Se expone para que
    /// la comprobación de duplicados del maestro (MVP-207, CA-2) trabaje sobre el mismo texto que
    /// acabará persistido (mismo recorte de espacios) y pueda hacerse antes de tocar la entidad.
    /// </summary>
    public static string NormalizeName(string name)
    {
        var normalized = (name ?? string.Empty).Trim();
        if (normalized.Length == 0)
            throw new WorkerValidationException(
                ErrorCodes.ValidationRequiredName, "El nombre del trabajador es obligatorio.");
        if (normalized.Length > NameMaxLength)
            throw new WorkerValidationException(
                ErrorCodes.ValidationWorkerNameLength,
                $"El nombre del trabajador no puede superar {NameMaxLength} caracteres.");

        return normalized;
    }

    private void Apply(string name, decimal? hourlyRate)
    {
        Name = NormalizeName(name);

        if (hourlyRate is < 0)
            throw new WorkerValidationException(
                ErrorCodes.ValidationRangeHourlyRate, "La tarifa horaria no puede ser negativa.");
        HourlyRate = hourlyRate;
    }
}
