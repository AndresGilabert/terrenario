using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

/// <summary>
/// MVP-206 — Adaptador EF Core de las solicitudes de traspaso y reactivación.
/// </summary>
public sealed class WorkspaceReactivationRequestRepository(TerrenarioDbContext db)
    : IWorkspaceReactivationRequestRepository
{
    public async Task AddRangeAsync(
        IEnumerable<WorkspaceReactivationRequest> requests,
        CancellationToken ct = default)
        => await db.WorkspaceReactivationRequests.AddRangeAsync(requests, ct);

    public Task<WorkspaceReactivationRequest?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => db.WorkspaceReactivationRequests.FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);

    public Task<WorkspaceReactivationRequest?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => db.WorkspaceReactivationRequests.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<ReactivationRequestDetail>> ListPendingAuthorizationsAsync(
        Guid authorizerUserId,
        CancellationToken ct = default)
        // Join a Workspace y a la cuenta solicitante para que la pantalla de decisión no tenga que
        // resolver nombres por su cuenta. Sin ORDER BY en base de datos: ordenar por DateTimeOffset
        // no lo traduce EF+SQLite (mismo criterio que las invitaciones pendientes de MVP-204).
        => (await db.WorkspaceReactivationRequests
            .Where(r => r.AuthorizerUserId == authorizerUserId
                && r.Status == ReactivationRequestStatuses.Requested)
            .Join(db.Workspaces, r => r.WorkspaceId, w => w.Id, (r, w) => new { Request = r, Workspace = w })
            .Join(db.Users, x => x.Request.RecipientUserId, u => u.Id, (x, u) => new { x.Request, x.Workspace, User = u })
            .Select(x => new ReactivationRequestDetail(
                x.Request.Id,
                x.Workspace.Id,
                x.Workspace.Name,
                x.User.Id,
                x.User.DisplayName,
                x.User.Email,
                x.Request.RequestedAt!.Value,
                x.Request.ExpiresAt))
            .ToListAsync(ct))
            .OrderByDescending(r => r.RequestedAt)
            .ToList();

    public async Task<IReadOnlyList<WorkspaceReactivationRequest>> ListOpenForWorkspaceAsync(
        Guid workspaceId,
        CancellationToken ct = default)
        => await db.WorkspaceReactivationRequests
            .Where(r => r.WorkspaceId == workspaceId
                && (r.Status == ReactivationRequestStatuses.Pending
                    || r.Status == ReactivationRequestStatuses.Requested))
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
