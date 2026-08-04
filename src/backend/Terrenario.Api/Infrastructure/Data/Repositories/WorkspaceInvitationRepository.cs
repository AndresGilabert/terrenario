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

    /// <summary>
    /// MVP-505 (CA-3) — Anula las invitaciones pendientes dirigidas a un correo. Se cargan y se
    /// anulan por el agregado en vez de con un `UPDATE` masivo: la transición a `anulada` tiene sus
    /// reglas (`Cancel`) y saltárselas aquí las dejaría en un estado que el dominio no admite.
    ///
    /// La anulación se atribuye a la propia persona: es ella quien, al darse de baja, retira las
    /// invitaciones que la nombraban.
    /// </summary>
    public async Task<int> CancelPendingForEmailAsync(
        string email,
        Guid cancelledByUserId,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var canonical = email.Trim().ToLowerInvariant();

        var pending = await db.WorkspaceInvitations
            .Where(i => i.Channel == InvitationChannels.Email
                && i.Email == canonical
                && i.Status == InvitationStatuses.Pending)
            .ToListAsync(ct);

        foreach (var invitation in pending)
            invitation.Cancel(cancelledByUserId, now);

        return pending.Count;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
