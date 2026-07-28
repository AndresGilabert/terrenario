using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests del traspaso explícito de propiedad (MVP-206, HU-3/CA-4): la alternativa a dar de baja del
/// propietario único. El Workspace sigue vivo, la persona elegida pasa a propietaria y quien
/// traspasa se queda como miembro normal (decisión de producto).
/// </summary>
public class TransferWorkspaceOwnershipHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();

    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();

    private readonly Workspace _workspace = Workspace.Create(OwnerId, "Finca El Olivar");

    private TransferWorkspaceOwnershipHandler CreateSut()
    {
        _workspaceRepository.FindByIdAsync(_workspace.Id, Arg.Any<CancellationToken>()).Returns(_workspace);
        return new TransferWorkspaceOwnershipHandler(_workspaceRepository);
    }

    private void GivenActiveMember(Guid userId, WorkspaceMember? member)
        => _workspaceRepository.FindActiveMemberAsync(_workspace.Id, userId, Arg.Any<CancellationToken>())
            .Returns(member);

    private TransferOwnershipCommand Command(Guid newOwnerId) => new(_workspace.Id, OwnerId, newOwnerId);

    [Fact]
    public async Task Deberia_TraspasarYDejarAlAnteriorComoMiembro()
    {
        var acting = WorkspaceMember.CreateOwner(_workspace.Id, OwnerId);
        var target = WorkspaceMember.CreateMember(_workspace.Id, TargetId);
        GivenActiveMember(OwnerId, acting);
        GivenActiveMember(TargetId, target);
        _workspaceRepository.ListMembersAsync(_workspace.Id, Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceMemberDetail>
            {
                new(TargetId, "Marta", "marta@ejemplo.com", WorkspaceRoles.Owner,
                    WorkspaceMemberStatuses.Active, DateTimeOffset.UtcNow)
            });
        var sut = CreateSut();

        var result = await sut.HandleAsync(Command(TargetId));

        result.Outcome.Should().Be(WorkspaceClosureOutcomes.Transferred);
        result.NewOwnerDisplayName.Should().Be("Marta");
        _workspace.OwnerId.Should().Be(TargetId);
        _workspace.IsDeleted.Should().BeFalse();
        target.Role.Should().Be(WorkspaceRoles.Owner);
        acting.Role.Should().Be(WorkspaceRoles.Member);
        // Traspasar no es irse: quien cede la propiedad conserva el acceso.
        acting.Status.Should().Be(WorkspaceMemberStatuses.Active);
        await _workspaceRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Rechazar_ASiQuienTraspasaNoEsPropietario()
    {
        GivenActiveMember(OwnerId, WorkspaceMember.CreateMember(_workspace.Id, OwnerId));
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(Command(TargetId));

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.AuthWorkspaceOwnerRequired);
    }

    [Fact]
    public async Task Deberia_Rechazar_UnDestinatarioQueNoEsMiembroActivo()
    {
        GivenActiveMember(OwnerId, WorkspaceMember.CreateOwner(_workspace.Id, OwnerId));
        GivenActiveMember(TargetId, null);
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(Command(TargetId));

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ResourceNotFound);
        await _workspaceRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Rechazar_ElTraspasoASiMismo()
    {
        var acting = WorkspaceMember.CreateOwner(_workspace.Id, OwnerId);
        GivenActiveMember(OwnerId, acting);
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(Command(OwnerId));

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleOwnershipTransferToSelf);
    }
}
