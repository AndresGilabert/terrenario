using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Domain.Tasks;

/// <summary>
/// Tarea del catálogo de un Workspace (MVP-205). Cada Workspace mantiene su propio catálogo,
/// que <b>arranca vacío</b> y es editable por cualquier miembro (RN-026, CA-1/CA-2). Las tareas se
/// reutilizan después al registrar actividad, donde la tarea es obligatoria y puede venir del
/// catálogo o de texto libre (RN-025).
///
/// Las tareas con histórico se <b>inactivan</b> (<see cref="IsActive"/>), no se borran (CA-3): los
/// registros que ya las referencian siguen siendo válidos y la tarea deja de ofrecerse para nuevos
/// registros. La inactivación es reversible.
///
/// El tipo se llama <c>TaskItem</c> y no <c>Task</c> (término del glosario) para no colisionar con
/// <see cref="System.Threading.Tasks.Task"/>; la tabla y el recurso de API sí son <c>tasks</c>.
/// </summary>
public sealed class TaskItem
{
    public const int NameMaxLength = 120;

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private TaskItem() { }

    /// <summary>
    /// Da de alta una tarea del catálogo (CA-2). Solo <see cref="Name"/> es obligatorio: el catálogo
    /// se puebla sin configuración externa adicional. Nace activa salvo que se indique lo contrario
    /// (el contrato admite <c>is_active</c> en el alta).
    /// </summary>
    public static TaskItem Create(Guid workspaceId, string name, bool isActive = true)
    {
        if (workspaceId == Guid.Empty)
            throw new TaskValidationException(
                ErrorCodes.ValidationRequiredTaskWorkspace,
                "La tarea necesita un Workspace válido.");

        var now = DateTimeOffset.UtcNow;

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        task.Apply(name);

        return task;
    }

    /// <summary>Renombra la tarea (edición, HU-1). No cambia el estado de actividad.</summary>
    public void Rename(string name)
    {
        Apply(name);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Activa o inactiva la tarea (HU-2, CA-3). La inactivación es reversible y no borra datos: los
    /// registros históricos que la referencian siguen siendo válidos.
    /// </summary>
    public void SetActive(bool isActive)
    {
        if (IsActive == isActive) return;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Normaliza y valida un nombre de tarea <b>sin mutar</b> ningún agregado. Se expone para que la
    /// comprobación de duplicados del catálogo trabaje sobre el mismo texto que acabará persistido
    /// (mismo recorte de espacios) y pueda hacerse antes de tocar la entidad.
    /// </summary>
    public static string NormalizeName(string name)
    {
        var normalized = (name ?? string.Empty).Trim();
        if (normalized.Length == 0)
            throw new TaskValidationException(
                ErrorCodes.ValidationRequiredTaskName, "El nombre de la tarea es obligatorio.");
        if (normalized.Length > NameMaxLength)
            throw new TaskValidationException(
                ErrorCodes.ValidationTaskNameLength,
                $"El nombre de la tarea no puede superar {NameMaxLength} caracteres.");

        return normalized;
    }

    private void Apply(string name) => Name = NormalizeName(name);
}
