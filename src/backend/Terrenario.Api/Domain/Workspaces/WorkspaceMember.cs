namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Vínculo entre un usuario y un Workspace. El estado sale del catálogo cerrado
/// <c>worker_member_status</c> y es la única fuente de verdad sobre si la membresía da acceso:
/// solo <see cref="WorkspaceMemberStatuses.Active"/> resuelve contexto activo (MVP-104).
/// </summary>
public sealed class WorkspaceMember
{
    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = WorkspaceRoles.Member;
    public string Status { get; private set; } = WorkspaceMemberStatuses.Active;
    public DateTimeOffset JoinedAt { get; private set; }

    /// <summary>Atajo de lectura del estado; no se persiste como columna propia.</summary>
    public bool IsActive => Status == WorkspaceMemberStatuses.Active;

    private WorkspaceMember() { }

    public static WorkspaceMember CreateOwner(Guid workspaceId, Guid userId) =>
        Create(workspaceId, userId, WorkspaceRoles.Owner);

    /// <summary>
    /// Membresía de quien entra por invitación (MVP-103). En MVP los permisos son planos
    /// (RN-034), así que el rol no condiciona lo que puede hacer dentro del Workspace.
    /// </summary>
    public static WorkspaceMember CreateMember(Guid workspaceId, Guid userId) =>
        Create(workspaceId, userId, WorkspaceRoles.Member);

    /// <summary>
    /// Retira el acceso sin borrar el vínculo, para no perder la trazabilidad de quién estuvo
    /// dentro del Workspace. Una membresía revocada deja de aparecer en el selector.
    /// </summary>
    public void Revoke() => Status = WorkspaceMemberStatuses.Revoked;

    private static WorkspaceMember Create(Guid workspaceId, Guid userId, string role) =>
        new()
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = role,
            Status = WorkspaceMemberStatuses.Active,
            JoinedAt = DateTimeOffset.UtcNow
        };
}
