using Terrenario.Api.Application.Workers.Commands;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Application.Workers;

/// <summary>
/// MVP-204 — Da de alta un trabajador sin cuenta en el Workspace activo (CA-2). Solo el nombre es
/// obligatorio; la tarifa horaria es opcional y de referencia.
/// </summary>
public sealed class CreateWorkerHandler(IWorkerRepository workerRepository)
{
    public async Task<WorkerSummary> HandleAsync(CreateWorkerCommand command, CancellationToken ct = default)
    {
        var worker = Worker.Create(command.WorkspaceId, command.Name, command.HourlyRate);

        await workerRepository.AddAsync(worker, ct);
        await workerRepository.SaveChangesAsync(ct);

        return ListWorkersHandler.ToSummary(worker);
    }
}
