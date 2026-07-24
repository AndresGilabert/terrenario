namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Vínculo entre un usuario y un Workspace. Los estados de membresía completos
/// (<c>invitado</c>, <c>revocado</c>) llegan con MVP-103/MVP-104; en MVP-102 solo se
/// crea la membresía activa del creador.
/// </summary>
public sealed class WorkspaceMember
{
    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = WorkspaceRoles.Member;
    public bool Active { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    private WorkspaceMember() { }

    public static WorkspaceMember CreateOwner(Guid workspaceId, Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = WorkspaceRoles.Owner,
            Active = true,
            JoinedAt = DateTimeOffset.UtcNow
        };
}
