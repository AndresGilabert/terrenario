using Terrenario.Api.Application.Activities.Commands;
using Terrenario.Api.Application.Tasks;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Activities;

namespace Terrenario.Api.Application.Activities;

/// <summary>
/// MVP-301 — Registra una actividad completa en el diario del Workspace activo (HU-1, CA-1).
///
/// El orden es deliberado: primero el dominio valida forma y reglas (400), y solo con un agregado
/// válido se comprueba que los vínculos pertenecen al Workspace (400
/// <c>FOREIGN_KEY_WORKSPACE_MISMATCH</c>). Así una petición mal formada no gasta cuatro consultas a
/// los maestros.
///
/// El coste llega siempre del cliente y se guarda tal cual (RN-003, CA-3): la tarifa horaria del
/// responsable puede <i>sugerirlo</i> en la UI, pero el servidor no lo calcula ni lo recalcula.
///
/// MVP-302 — Si la petición pide guardar la tarea libre en el catálogo, la promoción ocurre en la
/// <b>misma unidad de trabajo</b>: la tarea y la actividad se persisten juntas o no se persiste
/// ninguna (CA-3).
/// </summary>
public sealed class CreateActivityHandler(
    IActivityRepository activityRepository,
    ActivityLinkResolver linkResolver,
    TaskCatalogPromoter taskCatalogPromoter)
{
    public async Task<ActivitySaveResult> HandleAsync(
        CreateActivityCommand command,
        CancellationToken ct = default)
    {
        var activity = Activity.Create(
            command.WorkspaceId,
            command.PlotId,
            command.SeasonId,
            command.WorkerId,
            command.Date,
            command.Hours,
            command.TaskId,
            command.TaskText,
            command.ManualCost,
            command.Description,
            command.UserId);

        // Solo se comprueban los vínculos que llegan del cliente: la tarea que promociona MVP-302 la
        // crea este mismo caso de uso y todavía no está en base de datos.
        await linkResolver.EnsureLinksAsync(
            command.WorkspaceId, activity.PlotId, activity.SeasonId, activity.WorkerId, activity.TaskId, ct);

        var outcome = await PromoteTaskIfRequestedAsync(
            command.SaveTaskToCatalog, command.WorkspaceId, activity, taskCatalogPromoter, ct);

        await activityRepository.AddAsync(activity, ct);
        await activityRepository.SaveChangesAsync(ct);

        // Se relee como vista para devolver ya resueltos los nombres de terreno, responsable y tarea
        // y el aviso de fecha fuera de rango (RN-023), que es lo que la UI pinta sin más peticiones.
        var view = await activityRepository.GetViewAsync(command.WorkspaceId, activity.Id, ct)
                   ?? throw new InvalidOperationException("La actividad recién creada no se pudo releer.");

        return new ActivitySaveResult(view, outcome);
    }

    /// <summary>
    /// MVP-302 — Promoción de la tarea libre al catálogo, compartida con la edición. Devuelve
    /// <c>null</c> si no se pidió. Rechaza la petición cuando la tarea ya viene del catálogo: no hay
    /// nada que guardar y callarlo dejaría al usuario creyendo que se ha hecho algo.
    /// </summary>
    internal static async Task<TaskCatalogOutcome?> PromoteTaskIfRequestedAsync(
        bool requested,
        Guid workspaceId,
        Activity activity,
        TaskCatalogPromoter promoter,
        CancellationToken ct)
    {
        if (!requested) return null;

        if (activity.TaskText is not { } freeText)
            throw new ActivityValidationException(
                ErrorCodes.ValidationActivityTaskNotFreeText,
                "Solo se puede guardar en el catálogo una tarea escrita a mano.");

        var (task, outcome) = await promoter.ResolveOrCreateAsync(workspaceId, freeText, ct);
        activity.UseCatalogTask(task.Id);

        return outcome;
    }
}
