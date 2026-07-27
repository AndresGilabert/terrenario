using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests de la vista de personas del Workspace (MVP-204, HU-3/CA-4/CA-5). Verifica que combina
/// membresías reales (activo/revocado) con invitaciones por email pendientes (invitado) y que marca
/// las caducadas para sugerir el reenvío.
/// </summary>
public class ListWorkspacePeopleHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IWorkspaceInvitationRepository _invitationRepository =
        Substitute.For<IWorkspaceInvitationRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid InviterId = Guid.NewGuid();

    private ListWorkspacePeopleHandler CreateSut() => new(_workspaceRepository, _invitationRepository);

    [Fact]
    public async Task Deberia_CombinarMiembrosEInvitaciones_Y_MarcarCaducadas()
    {
        // Arrange — un activo, un revocado y dos invitaciones por email (una vigente, otra caducada)
        var members = new List<WorkspaceMemberDetail>
        {
            new(Guid.NewGuid(), "Andrés", "andres@ejemplo.com", WorkspaceRoles.Owner, WorkspaceMemberStatuses.Active, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), "Bruno", "bruno@ejemplo.com", WorkspaceRoles.Member, WorkspaceMemberStatuses.Revoked, DateTimeOffset.UtcNow),
        };
        _workspaceRepository.ListMembersAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns(members);

        var vigente = WorkspaceInvitation.Create(
            WorkspaceId, InviterId, InvitationChannels.Email, "nuevo@ejemplo.com", "hash-1", TimeSpan.FromDays(7));
        var caducada = WorkspaceInvitation.Create(
            WorkspaceId, InviterId, InvitationChannels.Email, "tarde@ejemplo.com", "hash-2", TimeSpan.FromDays(-1));
        _invitationRepository.ListPendingEmailAsync(WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceInvitation> { vigente, caducada });

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(WorkspaceId);

        // Assert
        result.Members.Should().HaveCount(2);
        result.Invited.Should().HaveCount(2);
        result.Invited.Single(i => i.Email == "nuevo@ejemplo.com").IsExpired.Should().BeFalse();
        result.Invited.Single(i => i.Email == "tarde@ejemplo.com").IsExpired.Should().BeTrue();
    }
}
