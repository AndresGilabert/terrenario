using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

public sealed class WorkspaceRepository(TerrenarioDbContext db) : IWorkspaceRepository
{
    public async Task AddAsync(Workspace workspace, WorkspaceMember ownerMembership, CancellationToken ct = default)
    {
        await db.Workspaces.AddAsync(workspace, ct);
        await db.WorkspaceMembers.AddAsync(ownerMembership, ct);
    }

    public Task<Workspace?> FindForMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default)
        => db.Workspaces
            .Where(w => w.Id == workspaceId)
            .Where(w => db.WorkspaceMembers.Any(m => m.WorkspaceId == w.Id && m.UserId == userId && m.IsActive))
            .FirstOrDefaultAsync(ct);

    public Task<Workspace?> FindDefaultForUserAsync(Guid userId, CancellationToken ct = default)
        => db.WorkspaceMembers
            .Where(m => m.UserId == userId && m.IsActive)
            .OrderByDescending(m => m.JoinedAt)
            .Join(db.Workspaces, m => m.WorkspaceId, w => w.Id, (_, w) => w)
            .FirstOrDefaultAsync(ct);

    public Task<Workspace?> FindByIdAsync(Guid workspaceId, CancellationToken ct = default)
        => db.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, ct);

    public Task<bool> HasActiveMembershipAsync(Guid workspaceId, Guid userId, CancellationToken ct = default)
        => db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.IsActive, ct);

    public async Task AddMemberAsync(WorkspaceMember member, CancellationToken ct = default)
        => await db.WorkspaceMembers.AddAsync(member, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
