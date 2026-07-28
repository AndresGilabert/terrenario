using Terrenario.Api.Application.Workers.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Application.Workers;

/// <summary>
/// MVP-204 — Da de alta un trabajador sin cuenta en el Workspace activo (CA-2). Solo el nombre es
/// obligatorio; la tarifa horaria es opcional y de referencia.
///
/// MVP-207 (CA-2) añade la guarda de nombre único por Workspace: el maestro existe precisamente
/// «para evitar nombres duplicados o inconsistentes» (MVP-204, HU-1), y dos filas «Juan Pérez» no se
/// pueden distinguir al imputar una jornada.
/// </summary>
public sealed class CreateWorkerHandler(IWorkerRepository workerRepository)
{
    public async Task<WorkerSummary> HandleAsync(CreateWorkerCommand command, CancellationToken ct = default)
    {
        // El dominio normaliza y valida el nombre; se construye primero para no comprobar duplicados
        // contra un texto sin normalizar.
        var worker = Worker.Create(command.WorkspaceId, command.Name, command.HourlyRate);

        await EnsureNameIsFreeAsync(workerRepository, command.WorkspaceId, worker.Name, null, ct);

        await workerRepository.AddAsync(worker, ct);
        await workerRepository.SaveChangesAsync(ct);

        return ListWorkersHandler.ToSummary(worker);
    }

    /// <summary>
    /// Guarda de duplicados del maestro, compartida con la edición. Lanza
    /// <see cref="WorkerConflictException"/> (409) si el nombre ya existe en el Workspace.
    /// </summary>
    internal static async Task EnsureNameIsFreeAsync(
        IWorkerRepository workerRepository,
        Guid workspaceId,
        string normalizedName,
        Guid? excludeWorkerId,
        CancellationToken ct)
    {
        var exists = await workerRepository.ExistsWithNameAsync(workspaceId, normalizedName, excludeWorkerId, ct);
        if (exists)
            throw new WorkerConflictException(
                ErrorCodes.ConflictWorkerNameDuplicate,
                $"Ya existe un trabajador «{normalizedName}» en este Workspace.");
    }
}
