using Terrenario.Api.Domain.Tasks;

namespace Terrenario.Api.Application.Tasks;

/// <summary>
/// Qué pasó al guardar en el catálogo una tarea escrita a mano (MVP-302). Viaja en la respuesta como
/// <c>task_catalog_outcome</c> para que la UI pueda decir la verdad en vez de un «guardado» genérico:
/// no es lo mismo haber creado una tarea que haber reutilizado —o reactivado— una que ya existía.
/// </summary>
public enum TaskCatalogOutcome
{
    /// <summary>No existía y se ha dado de alta en el catálogo.</summary>
    Created,

    /// <summary>Ya existía y activa: se reutiliza en vez de crear una segunda (CA-1 de MVP-302).</summary>
    Reused,

    /// <summary>Existía pero inactivada: se reactiva. MVP-205 lo fijó así: «se reactivan, no se duplican».</summary>
    Reactivated
}

/// <summary>
/// MVP-302 — Convierte en tarea del catálogo del Workspace una tarea introducida en texto libre
/// durante el registro de una actividad (RN-026, HU-1).
///
/// <b>No construye la guarda de duplicados: la reutiliza.</b> La prevención de duplicados se adelantó
/// a <c>MVP-205</c> (<c>P-026</c>) y vive en dos niveles —comparación por <c>lower(name)</c> más el
/// índice único <c>ux_tasks_workspace_name</c>—. Aquí se consulta esa misma comparación para
/// <b>resolver</b> el nombre en vez de chocar contra él: si la tarea ya existe se reutiliza (y si
/// estaba inactivada se reactiva), de modo que el flujo de actividad nunca ve un
/// <c>409 CONFLICT_TASK_NAME_DUPLICATE</c> por algo que el usuario no puede arreglar.
///
/// No persiste: el caso de uso de la actividad hace un único <c>SaveChanges</c> con el mismo
/// <c>DbContext</c>, así que la tarea y la actividad entran juntas o no entra ninguna (CA-3).
/// </summary>
public sealed class TaskCatalogPromoter(ITaskRepository taskRepository)
{
    public async Task<(TaskItem Task, TaskCatalogOutcome Outcome)> ResolveOrCreateAsync(
        Guid workspaceId,
        string freeText,
        CancellationToken ct = default)
    {
        // El nombre se normaliza con las reglas del catálogo (recorte y longitud), para buscar y para
        // crear con el mismo texto que acabaría persistido.
        var name = TaskItem.NormalizeName(freeText);

        var existing = await taskRepository.FindByNameAsync(workspaceId, name, ct);
        if (existing is not null)
        {
            if (existing.IsActive) return (existing, TaskCatalogOutcome.Reused);

            // Las inactivas siguen ocupando su nombre (MVP-205, CA-3). Que el usuario vuelva a
            // escribir esa labor es justo la señal de que la quiere disponible otra vez.
            existing.SetActive(true);
            return (existing, TaskCatalogOutcome.Reactivated);
        }

        var task = TaskItem.Create(workspaceId, name);
        await taskRepository.AddAsync(task, ct);

        return (task, TaskCatalogOutcome.Created);
    }
}
