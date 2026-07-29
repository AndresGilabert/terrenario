using Terrenario.Api.Application.Activities.Commands;
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
/// </summary>
public sealed class CreateActivityHandler(
    IActivityRepository activityRepository,
    ActivityLinkResolver linkResolver)
{
    public async Task<ActivityView> HandleAsync(CreateActivityCommand command, CancellationToken ct = default)
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

        await linkResolver.EnsureLinksAsync(
            command.WorkspaceId, activity.PlotId, activity.SeasonId, activity.WorkerId, activity.TaskId, ct);

        await activityRepository.AddAsync(activity, ct);
        await activityRepository.SaveChangesAsync(ct);

        // Se relee como vista para devolver ya resueltos los nombres de terreno, responsable y tarea
        // y el aviso de fecha fuera de rango (RN-023), que es lo que la UI pinta sin más peticiones.
        return await activityRepository.GetViewAsync(command.WorkspaceId, activity.Id, ct)
               ?? throw new InvalidOperationException("La actividad recién creada no se pudo releer.");
    }
}
