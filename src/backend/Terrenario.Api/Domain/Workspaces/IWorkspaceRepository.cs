namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Puerto del agregado Workspace. Desde MVP-206 **todas** las consultas de este puerto excluyen los
/// Workspaces dados de baja (<c>deleted_at</c>), salvo <see cref="FindIncludingDeletedAsync"/>: la
/// baja lógica deja de resolver contexto y de aparecer en el selector sin borrar un solo dato (CA-2).
/// </summary>
public interface IWorkspaceRepository
{
    Task AddAsync(Workspace workspace, WorkspaceMember ownerMembership, CancellationToken ct = default);

    /// <summary>Devuelve el Workspace solo si el usuario tiene membresía activa en él.</summary>
    Task<Workspace?> FindForMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Workspace por defecto del usuario: la membresía activa más reciente.
    /// Se usa cuando la sesión todavía no lleva contexto de Workspace.
    /// </summary>
    Task<Workspace?> FindDefaultForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Busca el Workspace sin exigir membresía. Lo necesita la aceptación de invitaciones
    /// (MVP-103), donde el usuario todavía no es miembro.
    /// </summary>
    Task<Workspace?> FindByIdAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>
    /// Única vía que ve también los Workspaces dados de baja (MVP-206). La necesita la reactivación,
    /// que por definición opera sobre uno inactivo. El resto de flujos usa <see cref="FindByIdAsync"/>.
    /// </summary>
    Task<Workspace?> FindIncludingDeletedAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>
    /// Membresías vigentes del usuario, ordenadas por nombre de Workspace. Las revocadas
    /// quedan fuera: no dan acceso ni deben aparecer en el selector (MVP-104).
    /// </summary>
    Task<IReadOnlyList<WorkspaceMembership>> ListActiveMembershipsAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<bool> HasActiveMembershipAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Todas las personas con membresía real del Workspace (activas y revocadas), unidas a su cuenta,
    /// para la vista de personas (MVP-204, HU-3). Las <c>invitado</c> no salen de aquí: son
    /// invitaciones por email pendientes que se combinan aparte.
    /// </summary>
    Task<IReadOnlyList<WorkspaceMemberDetail>> ListMembersAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>Membresía <c>activo</c> de un usuario en el Workspace (para revocar, MVP-204). <c>null</c> si no la tiene.</summary>
    Task<WorkspaceMember?> FindActiveMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);

    /// <summary>Número de miembros activos del Workspace. Sostiene la invariante CA-8 (no quedarse sin ninguno).</summary>
    Task<int> CountActiveMembersAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>Número de propietarios activos del Workspace. Sostiene la invariante CA-8 (no quedarse sin propietario).</summary>
    Task<int> CountActiveOwnersAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>
    /// Otro propietario activo del Workspace distinto del indicado (MVP-206, CA-5). Es el sucesor
    /// del traspaso automático: se elige el de membresía más antigua para que el resultado sea
    /// determinista. <c>null</c> si el indicado es el único propietario activo.
    /// </summary>
    Task<WorkspaceMember?> FindOtherActiveOwnerAsync(
        Guid workspaceId,
        Guid excludingUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Workspaces vivos en los que el usuario es el **único** propietario activo (MVP-206, CA-9).
    /// Sostiene la regla de no-orfandad de la baja de cuenta: cada uno debe resolverse (traspaso o
    /// baja lógica) antes de completarla.
    /// </summary>
    Task<IReadOnlyList<SoleOwnedWorkspace>> ListSoleOwnedAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Workspaces dados de baja por el usuario (MVP-206). Es la contrapartida de que la baja sea
    /// lógica y reversible: quien la dio puede volver a levantarlos sin depender de nadie, que es la
    /// única vía cuando el Workspace no tenía más miembros a los que notificar.
    /// </summary>
    Task<IReadOnlyList<Workspace>> ListClosedByAsync(Guid userId, CancellationToken ct = default);

    Task AddMemberAsync(WorkspaceMember member, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
