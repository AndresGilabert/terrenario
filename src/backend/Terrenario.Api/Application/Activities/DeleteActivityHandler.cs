using Terrenario.Api.Application.Activities.Commands;
using Terrenario.Api.Domain.Activities;

namespace Terrenario.Api.Application.Activities;

/// <summary>
/// MVP-301 — Elimina una actividad de forma <b>lógica</b> (RN-037): deja de aparecer en el diario,
/// en los listados y en el dashboard, pero la fila permanece en base de datos. No hay papelera ni
/// restauración en el MVP; la purga se decide con la política de retención (<c>P-033</c>).
///
/// Exige la versión vigente en <c>If-Match</c> (ADR-0005): borrar es la operación menos reversible
/// del diario, así que es justo donde no puede pisarse una edición ajena en silencio (CA-4).
/// La confirmación explícita del usuario la pone la UI en MVP-305.
/// </summary>
public sealed class DeleteActivityHandler(IActivityRepository activityRepository)
{
    /// <returns><c>false</c> si la actividad no existe, es de otro Workspace o ya estaba eliminada (404).</returns>
    public async Task<bool> HandleAsync(DeleteActivityCommand command, CancellationToken ct = default)
    {
        var activity = await activityRepository.FindByIdAsync(command.WorkspaceId, command.ActivityId, ct);
        if (activity is null) return false;

        activity.EnsureVersion(command.ExpectedVersion);
        activity.Delete(command.UserId);

        await activityRepository.SaveChangesAsync(ct);

        return true;
    }
}
