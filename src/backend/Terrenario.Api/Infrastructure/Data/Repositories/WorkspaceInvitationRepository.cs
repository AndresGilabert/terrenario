using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

public sealed class WorkspaceInvitationRepository(TerrenarioDbContext db) : IWorkspaceInvitationRepository
{
    public async Task AddAsync(WorkspaceInvitation invitation, CancellationToken ct = default)
        => await db.WorkspaceInvitations.AddAsync(invitation, ct);

    public Task<WorkspaceInvitation?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => db.WorkspaceInvitations.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

    public Task<WorkspaceInvitation?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => db.WorkspaceInvitations.FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<WorkspaceInvitation>> ListPendingAsync(
        Guid workspaceId,
        CancellationToken ct = default)
        // Las más recientes primero, ordenado en base de datos. Hasta MVP-501 salía sin ORDER BY y el
        // caso de uso reordenaba en memoria, solo porque EF+SQLite no traducía el orden sobre
        // DateTimeOffset y habría roto el test de repositorio (P-031).
        => await db.WorkspaceInvitations
            .Where(i => i.WorkspaceId == workspaceId && i.Status == InvitationStatuses.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WorkspaceInvitation>> ListReceivedPendingAsync(
        string canonicalEmail,
        CancellationToken ct = default)
        => await db.WorkspaceInvitations
            .Where(i => i.Channel == InvitationChannels.Email
                && i.Email == canonicalEmail
                && i.Status == InvitationStatuses.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
