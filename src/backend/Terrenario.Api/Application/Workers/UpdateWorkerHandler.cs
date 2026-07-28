using Terrenario.Api.Application.Workers.Commands;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Application.Workers;

/// <summary>
/// MVP-204 — Edita un trabajador del Workspace activo (CA-2) o cambia su estado de actividad (CA-3).
/// El trabajador se busca acotado al Workspace: si no existe en él, devuelve <c>null</c> y el borde
/// de transporte responde 404 (no se revela la existencia de trabajadores de otros Workspaces).
///
/// MVP-207 (CA-2): renombrar tampoco puede dejar dos trabajadores con el mismo nombre.
/// </summary>
public sealed class UpdateWorkerHandler(IWorkerRepository workerRepository)
{
    public async Task<WorkerSummary?> HandleAsync(UpdateWorkerCommand command, CancellationToken ct = default)
    {
        var worker = await workerRepository.FindByIdAsync(command.WorkspaceId, command.WorkerId, ct);
        if (worker is null) return null;

        // El nombre se normaliza y valida primero (400) y solo después se comprueba el duplicado
        // (409), sin tocar el agregado hasta que ambas guardas pasan. Se excluye el propio trabajador:
        // cambiar solo las mayúsculas de su nombre no es un conflicto consigo mismo.
        if (command.Name.Present)
        {
            var normalized = Worker.NormalizeName(command.Name.Value!);
            await CreateWorkerHandler.EnsureNameIsFreeAsync(
                workerRepository, command.WorkspaceId, normalized, worker.Id, ct);
        }

        // Edición parcial: los campos ausentes conservan el valor actual (no se borran).
        worker.Update(
            command.Name.Or(worker.Name)!,
            command.HourlyRate.Or(worker.HourlyRate));

        if (command.IsActive.Present)
            worker.SetActive(command.IsActive.Value);

        await workerRepository.SaveChangesAsync(ct);

        return ListWorkersHandler.ToSummary(worker);
    }
}
