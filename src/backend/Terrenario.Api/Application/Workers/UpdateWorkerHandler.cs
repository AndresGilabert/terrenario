using Terrenario.Api.Application.Workers.Commands;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Application.Workers;

/// <summary>
/// MVP-204 — Edita un trabajador del Workspace activo (CA-2) o cambia su estado de actividad (CA-3).
/// El trabajador se busca acotado al Workspace: si no existe en él, devuelve <c>null</c> y el borde
/// de transporte responde 404 (no se revela la existencia de trabajadores de otros Workspaces).
///
/// MVP-207 (CA-2): renombrar tampoco puede dejar dos trabajadores con el mismo nombre.
///
/// MVP-208 (CA-4): de un responsable <b>con cuenta</b> solo se edita la tarifa horaria, que es dato
/// operativo del Workspace. Su nombre llega de la identidad de Google (RN-036) y su disponibilidad la
/// gobierna la membresía (RN-027), así que renombrarlo o inactivarlo aquí se rechaza con 422 en vez
/// de dejar el maestro y la membresía diciendo cosas distintas.
/// </summary>
public sealed class UpdateWorkerHandler(IWorkerRepository workerRepository)
{
    public async Task<WorkerSummary?> HandleAsync(UpdateWorkerCommand command, CancellationToken ct = default)
    {
        var worker = await workerRepository.FindByIdAsync(command.WorkspaceId, command.WorkerId, ct);
        if (worker is null) return null;

        if (worker.HasAccount)
        {
            // El dominio es quien rechaza (mismo mensaje en cualquier camino). Se comprueba antes de
            // consultar duplicados: un renombrado que no está permitido no debe costar una consulta.
            if (command.Name.Present) worker.Update(command.Name.Value!, worker.HourlyRate);
            if (command.IsActive.Present) worker.SetActive(command.IsActive.Value);

            worker.UpdateHourlyRate(command.HourlyRate.Or(worker.HourlyRate));

            await workerRepository.SaveChangesAsync(ct);
            return ListWorkersHandler.ToSummary(worker);
        }

        // El nombre se normaliza y valida primero (400) y solo después se comprueba el duplicado
        // (409), sin tocar el agregado hasta que ambas guardas pasan. Se excluye el propio trabajador:
        // cambiar solo las mayúsculas de su nombre no es un conflicto consigo mismo. Desde MVP-208 la
        // comparación cubre también a los miembros, que ya son filas del maestro (CA-3).
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
