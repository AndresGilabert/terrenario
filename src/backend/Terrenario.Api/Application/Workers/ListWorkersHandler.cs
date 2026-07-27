using Terrenario.Api.Application.Workers.Commands;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Application.Workers;

/// <summary>
/// MVP-204 — Lista los trabajadores del Workspace activo (CA-2/CA-3). Admite filtro por estado de
/// actividad, alineado con <c>GET /api/v1/workers</c> (<c>is_active?</c>).
/// </summary>
public sealed class ListWorkersHandler(IWorkerRepository workerRepository)
{
    public async Task<IReadOnlyList<WorkerSummary>> HandleAsync(
        Guid workspaceId,
        bool? isActive,
        CancellationToken ct = default)
    {
        var workers = await workerRepository.ListByWorkspaceAsync(workspaceId, isActive, ct);
        return workers.Select(ToSummary).ToList();
    }

    internal static WorkerSummary ToSummary(Worker worker) => new(
        worker.Id,
        worker.WorkspaceId,
        worker.Name,
        worker.HourlyRate,
        worker.IsActive);
}
