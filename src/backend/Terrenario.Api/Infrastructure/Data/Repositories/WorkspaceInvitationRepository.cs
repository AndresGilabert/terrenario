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
        => await db.WorkspaceInvitations
            .Where(i => i.WorkspaceId == workspaceId && i.Status == InvitationStatuses.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WorkspaceInvitation>> ListPendingEmailAsync(
        Guid workspaceId,
        CancellationToken ct = default)
        // Sin ORDER BY en base de datos: el caso de uso ordena en memoria. Evita ordenar por
        // DateTimeOffset, que EF+SQLite no traduce (aunque PostgreSQL sí), para no romper el test
        // de repositorio contra SQLite real.
        => await db.WorkspaceInvitations
            .Where(i => i.WorkspaceId == workspaceId
                && i.Channel == InvitationChannels.Email
                && i.Status == InvitationStatuses.Pending)
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
