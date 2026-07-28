using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests de la vista de personas y accesos del Workspace (MVP-204, HU-3/CA-4/CA-5). Verifica que
/// combina membresías reales (activo/revocado) con invitaciones pendientes (invitado) y que marca
/// las caducadas para sugerir el reenvío.
///
/// MVP-208 (CA-7): proyecta los dos canales, porque esta es la superficie única de administración de
/// invitaciones pendientes y un enlace compartible también hay que poder retirarlo.
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
        // Arrange — un activo, un revocado, dos invitaciones por email (una vigente, otra caducada)
        // y un enlace compartible sin destinatario
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
        var enlace = WorkspaceInvitation.Create(
            WorkspaceId, InviterId, InvitationChannels.Link, null, "hash-3", TimeSpan.FromDays(7));
        _invitationRepository.ListPendingAsync(WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceInvitation> { vigente, caducada, enlace });

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(WorkspaceId);

        // Assert
        result.Members.Should().HaveCount(2);
        result.Invited.Should().HaveCount(3);
        result.Invited.Single(i => i.Email == "nuevo@ejemplo.com").IsExpired.Should().BeFalse();
        result.Invited.Single(i => i.Email == "tarde@ejemplo.com").IsExpired.Should().BeTrue();

        // CA-7 — el enlace compartible viaja con su canal y sin destinatario: no es una persona, pero
        // sí un acceso vivo que la pantalla tiene que poder anular.
        var link = result.Invited.Single(i => i.Channel == InvitationChannels.Link);
        link.Email.Should().BeNull();
        link.InvitationId.Should().Be(enlace.Id);
    }
}
