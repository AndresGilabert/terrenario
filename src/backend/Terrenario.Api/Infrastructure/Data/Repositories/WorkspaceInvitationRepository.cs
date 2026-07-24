using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

public sealed class WorkspaceInvitationRepository(TerrenarioDbContext db) : IWorkspaceInvitationRepository
{
    public async Task AddAsync(WorkspaceInvitation invitation, CancellationToken ct = default)
        => await db.WorkspaceInvitations.AddAsync(invitation, ct);

    public Task<WorkspaceInvitation?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => db.WorkspaceInvitations.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<WorkspaceInvitation>> ListPendingAsync(
        Guid workspaceId,
        CancellationToken ct = default)
        => await db.WorkspaceInvitations
            .Where(i => i.WorkspaceId == workspaceId && i.Status == InvitationStatuses.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
