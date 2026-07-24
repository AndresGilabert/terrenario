namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Vínculo entre un usuario y un Workspace. El catálogo completo de estados de membresía
/// (<c>invitado</c>, <c>revocado</c>) llega con MVP-104; hasta entonces la membresía nace
/// activa, tanto la del creador (MVP-102) como la derivada de una invitación (MVP-103).
/// </summary>
public sealed class WorkspaceMember
{
    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = WorkspaceRoles.Member;
    public bool IsActive { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    private WorkspaceMember() { }

    public static WorkspaceMember CreateOwner(Guid workspaceId, Guid userId) =>
        Create(workspaceId, userId, WorkspaceRoles.Owner);

    /// <summary>
    /// Membresía de quien entra por invitación (MVP-103). En MVP los permisos son planos
    /// (RN-034), así que el rol no condiciona lo que puede hacer dentro del Workspace.
    /// </summary>
    public static WorkspaceMember CreateMember(Guid workspaceId, Guid userId) =>
        Create(workspaceId, userId, WorkspaceRoles.Member);

    private static WorkspaceMember Create(Guid workspaceId, Guid userId, string role) =>
        new()
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = role,
            IsActive = true,
            JoinedAt = DateTimeOffset.UtcNow
        };
}
