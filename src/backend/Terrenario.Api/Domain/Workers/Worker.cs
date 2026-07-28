using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Domain.Workers;

/// <summary>
/// Persona seleccionable como responsable de una labor en el Workspace (RN-002/RN-027). Desde
/// MVP-208 este maestro es <b>el</b> maestro de responsables y cubre las dos clases de persona con un
/// único espacio de identificadores, que es lo que permite que <c>ACTIVITY.worker_id</c> siga siendo
/// una FK simple a <c>workers</c> (P-034):
///
/// <list type="bullet">
///   <item>
///     <b>Miembros del Workspace</b> (<see cref="UserAccountId"/> no nulo): se materializan solos al
///     crear el Workspace y al aceptarse una invitación. Su nombre llega de la identidad de Google
///     (RN-036) y no se edita aquí; su disponibilidad la gobierna la membresía, no una inactivación
///     manual (MVP-204, CA-7).
///   </item>
///   <item>
///     <b>Cuadrilla sin cuenta</b> (<see cref="UserAccountId"/> nulo): jornaleros y operarios que se
///     dan de alta, editan e inactivan a mano (MVP-204, CA-2/CA-3).
///   </item>
/// </list>
///
/// El índice único <c>ux_workers_workspace_name</c> (MVP-207) cubre ahora la unión de las dos clases,
/// de modo que no puede haber dos responsables indistinguibles en el desplegable (hallazgo R-16).
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
    /// Cuenta del sistema a la que pertenece este responsable, cuando la tiene (MVP-208). Es lo que
    /// convierte a un miembro del Workspace en una fila direccionable del maestro sin duplicar el
    /// espacio de identificadores. Nulo en la cuadrilla sin cuenta.
    /// </summary>
    public Guid? UserAccountId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public decimal? HourlyRate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// ¿Es un miembro del Workspace? Su nombre y su disponibilidad los gobierna la cuenta y la
    /// membresía, no la edición del maestro (MVP-208, CA-4).
    /// </summary>
    public bool HasAccount => UserAccountId is not null;

    private Worker() { }

    /// <summary>
    /// Da de alta un trabajador de cuadrilla, sin cuenta (CA-2). Solo <see cref="Name"/> es
    /// obligatorio; la tarifa es opcional y de referencia. Nace activo.
    /// </summary>
    public static Worker Create(Guid workspaceId, string name, decimal? hourlyRate = null)
    {
        var worker = NewFor(workspaceId);
        worker.Apply(name, hourlyRate);

        return worker;
    }

    /// <summary>
    /// Materializa la fila de responsable de un miembro del Workspace (MVP-208, CA-1). El nombre lo
    /// aporta la identidad de Google; la tarifa nace vacía y sí es editable después, por ser dato
    /// operativo.
    /// </summary>
    public static Worker CreateForMember(Guid workspaceId, Guid userAccountId, string displayName)
    {
        if (userAccountId == Guid.Empty)
            throw new WorkerValidationException(
                ErrorCodes.ValidationRequired,
                "El responsable con cuenta necesita una cuenta válida.");

        var worker = NewFor(workspaceId);
        worker.UserAccountId = userAccountId;
        worker.Apply(displayName, null);

        return worker;
    }

    /// <summary>
    /// Edita un trabajador de cuadrilla (CA-2). No cambia el estado de actividad. Un responsable con
    /// cuenta no pasa por aquí: su nombre no es editable en el maestro (RN-036, CA-4).
    /// </summary>
    public void Update(string name, decimal? hourlyRate)
    {
        EnsureNoAccount(
            ErrorCodes.BusinessRuleWorkerIdentityManaged,
            "El nombre de un responsable con cuenta llega de su cuenta de Google y no se edita en el maestro.");

        Apply(name, hourlyRate);
        Touch();
    }

    /// <summary>
    /// Actualiza solo la tarifa horaria. Es la única edición admitida en un responsable con cuenta:
    /// la tarifa es dato operativo del Workspace, no parte de su identidad (CA-4).
    /// </summary>
    public void UpdateHourlyRate(decimal? hourlyRate)
    {
        ApplyHourlyRate(hourlyRate);
        Touch();
    }

    /// <summary>
    /// Activa o inactiva un trabajador de cuadrilla (CA-3). La inactivación es reversible y no borra
    /// datos: los registros históricos que lo referencian siguen siendo válidos. Un miembro no se
    /// inactiva a mano —RN-027 obliga a que sea seleccionable mientras tenga acceso—: su
    /// disponibilidad la gobierna la membresía (<see cref="SyncMembership"/>).
    /// </summary>
    public void SetActive(bool isActive)
    {
        EnsureNoAccount(
            ErrorCodes.BusinessRuleWorkerMembershipManaged,
            "La disponibilidad de un miembro la gobierna su acceso al Workspace, no el maestro de trabajadores.");

        ChangeActive(isActive);
    }

    /// <summary>
    /// Sigue a la membresía sin intervención manual (CA-4): al retirarse el acceso el responsable deja
    /// de ser seleccionable, y al recuperarlo vuelve a serlo. No invalida los registros que ya lo
    /// referencian, igual que la inactivación de la cuadrilla.
    /// </summary>
    public void SyncMembership(bool isActive) => ChangeActive(isActive);

    /// <summary>
    /// Resincroniza el nombre con la identidad de Google (RN-036), que es su única fuente. Se llama
    /// cuando el nombre de display de la cuenta cambia después de materializar la fila.
    /// </summary>
    public void SyncIdentityName(string displayName)
    {
        var normalized = NormalizeName(displayName);
        if (Name == normalized) return;

        Name = normalized;
        Touch();
    }

    /// <summary>
    /// Renombra la fila con un sufijo de desempate (« (2)», « (3)»…) cuando su nombre choca con el de
    /// otro responsable del mismo Workspace. Es la misma política de datos preexistentes que aprobó el
    /// PO en MVP-207: conservar y renombrar, nunca borrar. El sufijo se recorta para no desbordar la
    /// longitud máxima de la columna.
    /// </summary>
    public void RenameWithSuffix(int ordinal)
    {
        Name = WithSuffix(Name, ordinal);
        Touch();
    }

    /// <summary>
    /// Nombre con sufijo de desempate, recortado a la longitud máxima de la columna. Se expone para
    /// que el desempate pueda comprobarse contra el maestro <b>antes</b> de tocar ninguna entidad.
    /// </summary>
    public static string WithSuffix(string name, int ordinal)
    {
        var suffix = $" ({ordinal})";
        var stem = name.Length + suffix.Length > NameMaxLength
            ? name[..(NameMaxLength - suffix.Length)]
            : name;

        return stem + suffix;
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

    private static Worker NewFor(Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
            throw new WorkerValidationException(
                ErrorCodes.ValidationRequiredWorkerWorkspace,
                "El trabajador necesita un Workspace válido.");

        var now = DateTimeOffset.UtcNow;

        return new Worker
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserAccountId = null,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private void Apply(string name, decimal? hourlyRate)
    {
        Name = NormalizeName(name);
        ApplyHourlyRate(hourlyRate);
    }

    private void ApplyHourlyRate(decimal? hourlyRate)
    {
        if (hourlyRate is < 0)
            throw new WorkerValidationException(
                ErrorCodes.ValidationRangeHourlyRate, "La tarifa horaria no puede ser negativa.");
        HourlyRate = hourlyRate;
    }

    private void ChangeActive(bool isActive)
    {
        if (IsActive == isActive) return;
        IsActive = isActive;
        Touch();
    }

    private void EnsureNoAccount(string errorCode, string message)
    {
        if (HasAccount) throw new WorkerBusinessRuleException(errorCode, message);
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
