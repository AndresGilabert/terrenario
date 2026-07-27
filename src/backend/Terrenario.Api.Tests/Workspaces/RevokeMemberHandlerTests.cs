using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests de la revocación de acceso (MVP-204, HU-4/CA-7/CA-8). Cubren la transición a
/// <c>revocado</c> y las dos guardas de la invariante CA-8: no dejar el Workspace sin propietario ni
/// sin ningún miembro activo.
/// </summary>
public class RevokeMemberHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid TargetUserId = Guid.NewGuid();

    private RevokeMemberHandler CreateSut() => new(_workspaceRepository);

    [Fact]
    public async Task Deberia_RevocarMiembroActivo_Y_Persistir()
    {
        // Arrange — un miembro (no propietario) con otro activo detrás para no violar CA-8
        var member = WorkspaceMember.CreateMember(WorkspaceId, TargetUserId);
        _workspaceRepository.FindActiveMemberAsync(WorkspaceId, TargetUserId, Arg.Any<CancellationToken>())
            .Returns(member);
        _workspaceRepository.CountActiveMembersAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns(2);
        var sut = CreateSut();

        // Act
        await sut.HandleAsync(WorkspaceId, TargetUserId);

        // Assert — CA-7: cambia de estado sin borrar el vínculo
        member.Status.Should().Be(WorkspaceMemberStatuses.Revoked);
        member.IsActive.Should().BeFalse();
        await _workspaceRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Rechazar_Cuando_LaPersonaNoEsMiembroActivo()
    {
        // Arrange
        _workspaceRepository.FindActiveMemberAsync(WorkspaceId, TargetUserId, Arg.Any<CancellationToken>())
            .Returns((WorkspaceMember?)null);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(WorkspaceId, TargetUserId);

        // Assert
        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ResourceNotFound);
        await _workspaceRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Rechazar_Cuando_EsElPropietarioUnico()
    {
        // Arrange — CA-8: no se puede revocar al propietario mientras sea el único
        var owner = WorkspaceMember.CreateOwner(WorkspaceId, TargetUserId);
        _workspaceRepository.FindActiveMemberAsync(WorkspaceId, TargetUserId, Arg.Any<CancellationToken>())
            .Returns(owner);
        _workspaceRepository.CountActiveOwnersAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns(1);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(WorkspaceId, TargetUserId);

        // Assert
        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleCannotRevokeOwner);
        owner.Status.Should().Be(WorkspaceMemberStatuses.Active);
        await _workspaceRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Rechazar_Cuando_EsElUltimoMiembroActivo()
    {
        // Arrange — CA-8: no dejar el Workspace sin ningún miembro activo (aquí un miembro no propietario)
        var member = WorkspaceMember.CreateMember(WorkspaceId, TargetUserId);
        _workspaceRepository.FindActiveMemberAsync(WorkspaceId, TargetUserId, Arg.Any<CancellationToken>())
            .Returns(member);
        _workspaceRepository.CountActiveOwnersAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns(0);
        _workspaceRepository.CountActiveMembersAsync(WorkspaceId, Arg.Any<CancellationToken>()).Returns(1);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(WorkspaceId, TargetUserId);

        // Assert
        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleLastActiveMember);
        await _workspaceRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
