using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Operations;
using Terrenario.Api.Domain.Tasks;

namespace Terrenario.Api.Domain.Activities;

/// <summary>
/// Actividad registrada en el diario del Workspace (MVP-301). Es la unidad de captura más frecuente
/// del MVP: qué se ha hecho, quién lo ha hecho, cuánto ha durado, cuánto ha costado y dónde.
///
/// Reglas de negocio que materializa:
/// <list type="bullet">
/// <item>RN-001 — toda actividad va asociada a un terreno (<see cref="PlotId"/>).</item>
/// <item>RN-002 — responsable (<see cref="WorkerId"/>) y tiempo (<see cref="Hours"/>) obligatorios.</item>
/// <item>RN-003 — el coste es siempre <b>manual</b> (<see cref="ManualCost"/>): no se calcula ni se
/// recalcula desde la tarifa del responsable, que solo puede <i>sugerir</i> un valor en la UI.</item>
/// <item>RN-021 — temporada obligatoria (<see cref="SeasonId"/>); la UI autoselecciona la activa.</item>
/// <item>RN-023 — una fecha fuera del rango de la temporada <b>no bloquea</b>: se avisa y se guarda.
/// Por eso el agregado no valida el rango; lo calcula <c>IsOutOfSeasonRange</c> en la lectura.</item>
/// <item>RN-025 — la tarea es obligatoria y llega <b>del catálogo o en texto libre</b>, nunca las dos
/// a la vez: es lo que cierra <c>P-028</c> (ver <see cref="TaskId"/>/<see cref="TaskText"/>).</item>
/// <item>RN-037 — la eliminación es <b>lógica</b> (<see cref="DeletedAt"/>): un borrado accidental no
/// destruye operativa ya capturada. La confirmación explícita en la UI es alcance de MVP-305.</item>
/// </list>
///
/// <b>Concurrencia optimista</b> (ADR-0005): <see cref="Version"/> arranca en 1 y se incrementa en
/// cada mutación. <c>ACTIVITY</c> estrena aquí el patrón que reutilizan compras (MVP-303),
/// consumos (MVP-304) y cosechas (MVP-401).
/// </summary>
public sealed class Activity
{
    /// <summary>
    /// Misma cota que <see cref="TaskItem.NameMaxLength"/>: una tarea libre debe poder guardarse tal
    /// cual en el catálogo (MVP-302) sin que el texto capturado no quepa.
    /// </summary>
    public const int TaskTextMaxLength = TaskItem.NameMaxLength;

    public const int DescriptionMaxLength = 500;

    /// <summary>Cota de <c>decimal(5,2)</c>: hasta 999,99 horas en un mismo registro.</summary>
    public const decimal HoursMax = 999.99m;

    /// <summary>Cota de <c>decimal(10,2)</c> del coste manual.</summary>
    public const decimal ManualCostMax = 99_999_999.99m;

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid PlotId { get; private set; }
    public Guid SeasonId { get; private set; }
    public Guid WorkerId { get; private set; }

    /// <summary>Fecha de negocio de la actividad. Es la que ordena el diario (RN-033), no <see cref="CreatedAt"/>.</summary>
    public DateOnly Date { get; private set; }

    public decimal Hours { get; private set; }

    /// <summary>
    /// Tarea del catálogo del Workspace (MVP-205). Excluyente con <see cref="TaskText"/>: RN-025
    /// admite «del catálogo <b>o</b> en texto libre», y guardar las dos permitiría que divergieran.
    /// </summary>
    public Guid? TaskId { get; private set; }

    /// <summary>Tarea escrita al vuelo cuando todavía no está en el catálogo (RN-025).</summary>
    public string? TaskText { get; private set; }

    public decimal ManualCost { get; private set; }
    public string? Description { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid UpdatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Versión para el bloqueo optimista (<c>If-Match</c>, ADR-0005). Arranca en 1.</summary>
    public long Version { get; private set; }

    /// <summary>Marca de eliminación lógica (RN-037). Nunca hay borrado físico.</summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    private Activity() { }

    /// <summary>
    /// Da de alta una actividad completa (HU-1, CA-1). Todos los vínculos llegan ya verificados como
    /// pertenecientes al Workspace activo: comprobarlo es responsabilidad del caso de uso, que es
    /// quien tiene acceso a los maestros.
    /// </summary>
    public static Activity Create(
        Guid workspaceId,
        Guid plotId,
        Guid seasonId,
        Guid workerId,
        DateOnly date,
        decimal hours,
        Guid? taskId,
        string? taskText,
        decimal manualCost,
        string? description,
        Guid userId)
    {
        if (workspaceId == Guid.Empty)
            throw new ActivityValidationException(
                ErrorCodes.ValidationActivityRequiredFields,
                "La actividad necesita un Workspace válido.");

        var now = DateTimeOffset.UtcNow;

        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            CreatedBy = userId,
            CreatedAt = now,
            UpdatedBy = userId,
            UpdatedAt = now,
            Version = 1
        };

        activity.Apply(plotId, seasonId, workerId, date, hours, taskId, taskText, manualCost, description);

        return activity;
    }

    /// <summary>
    /// Corrige una actividad ya registrada (HU-2). Incrementa <see cref="Version"/>: cualquier cliente
    /// que siga con la versión anterior recibirá <c>409</c> al intentar escribir (CA-4).
    /// </summary>
    public void Update(
        Guid plotId,
        Guid seasonId,
        Guid workerId,
        DateOnly date,
        decimal hours,
        Guid? taskId,
        string? taskText,
        decimal manualCost,
        string? description,
        Guid userId)
    {
        Apply(plotId, seasonId, workerId, date, hours, taskId, taskText, manualCost, description);
        Touch(userId);
    }

    /// <summary>
    /// Eliminación <b>lógica</b> (RN-037): el registro desaparece del diario, de los listados y del
    /// dashboard, pero no se borra de la base de datos. Es idempotente en el dominio; el caso de uso
    /// devuelve 404 si ya estaba eliminada, para que el diario no muestre dos veces la misma baja.
    /// </summary>
    public void Delete(Guid userId)
    {
        if (IsDeleted) return;
        DeletedAt = DateTimeOffset.UtcNow;
        Touch(userId);
    }

    /// <summary>
    /// MVP-302 — La tarea escrita a mano pasa a referenciar su fila del catálogo (RN-025/RN-026): se
    /// asigna <see cref="TaskId"/> y se limpia <see cref="TaskText"/>, manteniendo la exclusividad del
    /// par.
    ///
    /// <b>No mueve la versión</b> a propósito: en el alta forma parte del mismo registro que se está
    /// creando, y en la edición <see cref="Update"/> ya la ha movido antes. Subirla aquí contaría dos
    /// cambios donde el usuario hizo uno.
    /// </summary>
    public void UseCatalogTask(Guid taskId)
    {
        if (taskId == Guid.Empty)
            throw new ActivityValidationException(
                ErrorCodes.ValidationActivityTaskRequired,
                "La tarea del catálogo no es válida.");

        TaskId = taskId;
        TaskText = null;
    }

    /// <summary>
    /// Comprueba que la versión que trae el cliente es la vigente (ADR-0005). Se llama <b>antes</b> de
    /// mutar nada: el conflicto no debe dejar el agregado a medias.
    /// </summary>
    public void EnsureVersion(long expectedVersion)
    {
        if (expectedVersion == Version) return;

        throw new ConcurrencyConflictException(
            "Otra persona ha modificado este registro mientras lo editabas. Refresca para ver la versión actual.")
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
        Guid workerId,
        DateOnly date,
        decimal hours,
        Guid? taskId,
        string? taskText,
        decimal manualCost,
        string? description)
    {
        // RN-001/RN-002/RN-021 — terreno, responsable y temporada son parte del registro mínimo.
        if (plotId == Guid.Empty || seasonId == Guid.Empty || workerId == Guid.Empty)
            throw new ActivityValidationException(
                ErrorCodes.ValidationActivityRequiredFields,
                "La actividad necesita terreno, temporada y responsable.");

        // RN-025 — tarea obligatoria, del catálogo o en texto libre, pero no ambas: si se guardaran
        // las dos podrían divergir y el diario no sabría cuál mostrar.
        var normalizedTaskText = (taskText ?? string.Empty).Trim();
        if (taskId is null && normalizedTaskText.Length == 0)
            throw new ActivityValidationException(
                ErrorCodes.ValidationActivityTaskRequired,
                "La actividad necesita una tarea: elígela del catálogo o escríbela.");
        if (taskId is not null && normalizedTaskText.Length > 0)
            throw new ActivityValidationException(
                ErrorCodes.ValidationActivityTaskRequired,
                "Indica la tarea del catálogo o un texto libre, pero no las dos.");
        if (normalizedTaskText.Length > TaskTextMaxLength)
            throw new ActivityValidationException(
                ErrorCodes.ValidationActivityTaskTextLength,
                $"La tarea no puede superar {TaskTextMaxLength} caracteres.");

        // RN-002 — sin tiempo dedicado no hay actividad.
        if (hours <= 0 || hours > HoursMax)
            throw new ActivityValidationException(
                ErrorCodes.ValidationActivityHoursRange,
                $"Las horas deben ser mayores que 0 y no superar {HoursMax:0.##}.");

        // RN-003 — el coste es manual y obligatorio; 0 es válido (una labor propia sin coste imputado).
        if (manualCost < 0 || manualCost > ManualCostMax)
            throw new ActivityValidationException(
                ErrorCodes.ValidationActivityCostRange,
                "El coste no puede ser negativo.");

        var normalizedDescription = (description ?? string.Empty).Trim();
        if (normalizedDescription.Length > DescriptionMaxLength)
            throw new ActivityValidationException(
                ErrorCodes.ValidationActivityDescriptionLength,
                $"La descripción no puede superar {DescriptionMaxLength} caracteres.");

        PlotId = plotId;
        SeasonId = seasonId;
        WorkerId = workerId;
        Date = date;
        Hours = decimal.Round(hours, 2, MidpointRounding.AwayFromZero);
        TaskId = taskId;
        TaskText = normalizedTaskText.Length == 0 ? null : normalizedTaskText;
        ManualCost = decimal.Round(manualCost, 2, MidpointRounding.AwayFromZero);
        Description = normalizedDescription.Length == 0 ? null : normalizedDescription;
    }
}
