using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

public sealed class WorkspaceRepository(TerrenarioDbContext db) : IWorkspaceRepository
{
    /// <summary>
    /// Base de todas las lecturas del puerto: un Workspace dado de baja (MVP-206, CA-2) deja de
    /// existir para el resto de la aplicación —no resuelve contexto ni aparece en el selector—
    /// aunque sus datos sigan íntegros en base de datos. El único acceso que lo ve es
    /// <see cref="FindIncludingDeletedAsync"/>, que usa la reactivación.
    /// </summary>
    private IQueryable<Workspace> LiveWorkspaces => db.Workspaces.Where(w => w.DeletedAt == null);

    public async Task AddAsync(Workspace workspace, WorkspaceMember ownerMembership, CancellationToken ct = default)
    {
        await db.Workspaces.AddAsync(workspace, ct);
        await db.WorkspaceMembers.AddAsync(ownerMembership, ct);
    }

    public Task<Workspace?> FindForMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default)
        => LiveWorkspaces
            .Where(w => w.Id == workspaceId)
            .Where(w => db.WorkspaceMembers.Any(m =>
                m.WorkspaceId == w.Id
                && m.UserId == userId
                && m.Status == WorkspaceMemberStatuses.Active))
            .FirstOrDefaultAsync(ct);

    // Workspace por defecto al perder el activo (CA-8): la membresía más reciente. Orden y `LIMIT 1`
    // en base de datos; hasta MVP-501 se traía la lista entera para quedarse con una fila, porque
    // EF+SQLite no traducía ORDER BY sobre DateTimeOffset y el arnés corría sobre SQLite (P-031).
    public Task<Workspace?> FindDefaultForUserAsync(Guid userId, CancellationToken ct = default)
        => db.WorkspaceMembers
            .Where(m => m.UserId == userId && m.Status == WorkspaceMemberStatuses.Active)
            .Join(LiveWorkspaces, m => m.WorkspaceId, w => w.Id, (m, w) => new { Member = m, Workspace = w })
            .OrderByDescending(x => x.Member.JoinedAt)
            .Select(x => x.Workspace)
            .FirstOrDefaultAsync(ct);

    public Task<Workspace?> FindByIdAsync(Guid workspaceId, CancellationToken ct = default)
        => LiveWorkspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, ct);

    public Task<Workspace?> FindIncludingDeletedAsync(Guid workspaceId, CancellationToken ct = default)
        => db.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, ct);

    public async Task<IReadOnlyList<WorkspaceMembership>> ListActiveMembershipsAsync(
        Guid userId,
        CancellationToken ct = default)
        // El orden va por la columna real del Workspace ANTES de proyectar: EF Core no sabe
        // traducir un OrderBy sobre una propiedad del DTO construido en el Select/Join, y hacerlo
        // lanzaba "could not be translated" (HTTP 500) en todo el listado de Workspaces (MVP-104).
        => await db.WorkspaceMembers
            .Where(m => m.UserId == userId && m.Status == WorkspaceMemberStatuses.Active)
            .Join(
                LiveWorkspaces,
                m => m.WorkspaceId,
                w => w.Id,
                (m, w) => new { Member = m, Workspace = w })
            .OrderBy(x => x.Workspace.Name)
            .Select(x => new WorkspaceMembership(
                x.Workspace.Id,
                x.Workspace.Name,
                x.Member.Role,
                x.Member.Status,
                x.Member.JoinedAt))
            .ToListAsync(ct);

    public Task<bool> HasActiveMembershipAsync(Guid workspaceId, Guid userId, CancellationToken ct = default)
        => LiveWorkspaces.AnyAsync(
            w => w.Id == workspaceId
                && db.WorkspaceMembers.Any(m =>
                    m.WorkspaceId == w.Id
                    && m.UserId == userId
                    && m.Status == WorkspaceMemberStatuses.Active),
            ct);

    public async Task<IReadOnlyList<WorkspaceMemberDetail>> ListMembersAsync(
        Guid workspaceId,
        CancellationToken ct = default)
        // Orden por la columna real (nombre de la cuenta) ANTES de proyectar: EF no traduce un
        // OrderBy sobre el DTO del Join y lanzaría "could not be translated" (lección de P-014).
        => await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId)
            .Join(
                db.Users,
                m => m.UserId,
                u => u.Id,
                (m, u) => new { Member = m, User = u })
            .OrderBy(x => x.User.DisplayName)
            .Select(x => new WorkspaceMemberDetail(
                x.Member.UserId,
                x.User.DisplayName,
                x.User.Email,
                x.Member.Role,
                x.Member.Status,
                x.Member.JoinedAt))
            .ToListAsync(ct);

    public Task<WorkspaceMember?> FindActiveMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default)
        => db.WorkspaceMembers.FirstOrDefaultAsync(
            m => m.WorkspaceId == workspaceId
                && m.UserId == userId
                && m.Status == WorkspaceMemberStatuses.Active,
            ct);

    public Task<int> CountActiveMembersAsync(Guid workspaceId, CancellationToken ct = default)
        => db.WorkspaceMembers.CountAsync(
            m => m.WorkspaceId == workspaceId && m.Status == WorkspaceMemberStatuses.Active,
            ct);

    public Task<int> CountActiveOwnersAsync(Guid workspaceId, CancellationToken ct = default)
        => db.WorkspaceMembers.CountAsync(
            m => m.WorkspaceId == workspaceId
                && m.Status == WorkspaceMemberStatuses.Active
                && m.Role == WorkspaceRoles.Owner,
            ct);

    // Sucesor del traspaso automático (RN-038, CA-5): el copropietario activo más antiguo. Orden y
    // `LIMIT 1` en base de datos desde MVP-501 (P-031).
    //
    // El desempate por `UserId` no es decorativo: dos personas pueden tener **el mismo** `joined_at`
    // —la resolución del reloj es de milisegundos y una alta en lote entra a la vez—, y sin él quien
    // hereda el Workspace lo decide el orden físico de las filas. CA-5 exige que sea determinista,
    // así que la regla se cierra aquí en vez de depender de la suerte (MVP-502).
    public Task<WorkspaceMember?> FindOtherActiveOwnerAsync(
        Guid workspaceId,
        Guid excludingUserId,
        CancellationToken ct = default)
        => db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId
                && m.UserId != excludingUserId
                && m.Status == WorkspaceMemberStatuses.Active
                && m.Role == WorkspaceRoles.Owner)
            .OrderBy(m => m.JoinedAt)
            .ThenBy(m => m.UserId)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<SoleOwnedWorkspace>> ListSoleOwnedAsync(
        Guid userId,
        CancellationToken ct = default)
        // Orden por la columna real del Workspace ANTES de proyectar (lección de P-014). Los
        // contadores van como subconsultas correlacionadas para resolverlo en una sola ida a la BD.
        => await db.WorkspaceMembers
            .Where(m => m.UserId == userId
                && m.Status == WorkspaceMemberStatuses.Active
                && m.Role == WorkspaceRoles.Owner)
            .Join(LiveWorkspaces, m => m.WorkspaceId, w => w.Id, (_, w) => w)
            .Where(w => db.WorkspaceMembers.Count(m =>
                m.WorkspaceId == w.Id
                && m.Status == WorkspaceMemberStatuses.Active
                && m.Role == WorkspaceRoles.Owner) == 1)
            .OrderBy(w => w.Name)
            .Select(w => new SoleOwnedWorkspace(
                w.Id,
                w.Name,
                db.WorkspaceMembers.Count(m =>
                    m.WorkspaceId == w.Id
                    && m.Status == WorkspaceMemberStatuses.Active
                    && m.UserId != userId)))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Workspace>> ListClosedByAsync(
        Guid userId,
        CancellationToken ct = default)
        => await db.Workspaces
            .Where(w => w.DeletedAt != null && w.DeletedByUserId == userId)
            .OrderBy(w => w.Name)
            .ToListAsync(ct);

    public async Task AddMemberAsync(WorkspaceMember member, CancellationToken ct = default)
        => await db.WorkspaceMembers.AddAsync(member, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
