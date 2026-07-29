using Terrenario.Api.Application.Activities.Commands;
using Terrenario.Api.Application.Tasks;
using Terrenario.Api.Domain.Activities;

namespace Terrenario.Api.Application.Activities;

/// <summary>
/// MVP-301 — Corrige una actividad ya registrada (HU-2). La actividad se busca acotada al Workspace
/// y viva: si no existe, es de otro Workspace o ya fue eliminada lógicamente, devuelve <c>null</c> y
/// el borde de transporte responde 404 sin revelar recursos ajenos.
///
/// <b>Concurrencia optimista</b> (CA-4, ADR-0005): la versión de <c>If-Match</c> se comprueba
/// <b>antes</b> de tocar nada, para que un conflicto no deje el agregado a medias ni gaste las
/// consultas de vínculos.
///
/// MVP-302 — Es también la vía para guardar en el catálogo la tarea libre de una actividad <b>ya
/// registrada</b>: <c>PATCH { save_task_to_catalog: true }</c> sin más campos promociona el texto que
/// ya tiene, sin obligar a reescribirlo (CA-3).
/// </summary>
public sealed class UpdateActivityHandler(
    IActivityRepository activityRepository,
    ActivityLinkResolver linkResolver,
    TaskCatalogPromoter taskCatalogPromoter)
{
    public async Task<ActivitySaveResult?> HandleAsync(
        UpdateActivityCommand command,
        CancellationToken ct = default)
    {
        var activity = await activityRepository.FindByIdAsync(command.WorkspaceId, command.ActivityId, ct);
        if (activity is null) return null;

        activity.EnsureVersion(command.ExpectedVersion);

        var plotId = command.PlotId.Or(activity.PlotId);
        var seasonId = command.SeasonId.Or(activity.SeasonId);
        var workerId = command.WorkerId.Or(activity.WorkerId);
        var (taskId, taskText) = ResolveTask(command, activity);

        // Los vínculos se comprueban antes de mutar: un 400 no debe dejar el agregado a medias en el
        // change tracker de la petición.
        await linkResolver.EnsureLinksAsync(command.WorkspaceId, plotId, seasonId, workerId, taskId, ct);

        activity.Update(
            plotId,
            seasonId,
            workerId,
            command.Date.Or(activity.Date),
            command.Hours.Or(activity.Hours),
            taskId,
            taskText,
            command.ManualCost.Or(activity.ManualCost),
            command.Description.Or(activity.Description),
            command.UserId);

        // La promoción va después de `Update`, que ya ha movido la versión: el usuario ha hecho un
        // solo cambio y la versión sube una sola vez.
        var outcome = await CreateActivityHandler.PromoteTaskIfRequestedAsync(
            command.SaveTaskToCatalog, command.WorkspaceId, activity, taskCatalogPromoter, ct);

        await activityRepository.SaveChangesAsync(ct);

        var view = await activityRepository.GetViewAsync(command.WorkspaceId, activity.Id, ct);

        return view is null ? null : new ActivitySaveResult(view, outcome);
    }

    /// <summary>
    /// La tarea es un par excluyente (RN-025). Si viene <b>cualquiera</b> de los dos campos se
    /// sustituye la pareja completa y el miembro ausente pasa a nulo: enviar solo <c>task_id</c> sobre
    /// una actividad con texto libre dejaría los dos informados y el dominio lo rechazaría, sin que el
    /// cliente pudiera hacer nada razonable.
    /// </summary>
    private static (Guid? TaskId, string? TaskText) ResolveTask(UpdateActivityCommand command, Activity activity)
        => command.TaskId.Present || command.TaskText.Present
            ? (command.TaskId.Value, command.TaskText.Value)
            : (activity.TaskId, activity.TaskText);
}
