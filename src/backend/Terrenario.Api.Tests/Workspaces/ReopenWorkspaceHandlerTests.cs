using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests de la reapertura por quien dio de baja (MVP-206). Es la cara reversible de la baja lógica
/// (CA-2) y la única vía cuando el Workspace no tenía más miembros a los que notificar; para
/// cualquier otra cuenta, el Workspace dado de baja no existe.
/// </summary>
public class ReopenWorkspaceHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IWorkspaceReactivationRequestRepository _reactivationRepository =
        Substitute.For<IWorkspaceReactivationRequestRepository>();

    private static readonly Guid OwnerId = Guid.NewGuid();
    private readonly Workspace _workspace = Workspace.Create(OwnerId, "Finca El Olivar");

    public ReopenWorkspaceHandlerTests()
    {
        _workspace.SoftDelete(OwnerId, DateTimeOffset.UtcNow);
        _workspaceRepository.FindIncludingDeletedAsync(_workspace.Id, Arg.Any<CancellationToken>())
            .Returns(_workspace);
        _reactivationRepository.ListOpenForWorkspaceAsync(_workspace.Id, Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceReactivationRequest>());
    }

    private ReopenWorkspaceHandler CreateSut() =>
        new(_workspaceRepository, _reactivationRepository);

    [Fact]
    public async Task Deberia_LevantarElWorkspaceConSuPropiedadIntacta()
    {
        var member = WorkspaceMember.CreateOwner(_workspace.Id, OwnerId);
        _workspaceRepository.FindActiveMemberAsync(_workspace.Id, OwnerId, Arg.Any<CancellationToken>())
            .Returns(member);
        var sut = CreateSut();

        var result = await sut.HandleAsync(_workspace.Id, OwnerId);

        result.Name.Should().Be("Finca El Olivar");
        _workspace.IsDeleted.Should().BeFalse();
        _workspace.OwnerId.Should().Be(OwnerId);
        member.Role.Should().Be(WorkspaceRoles.Owner);
    }

    [Fact]
    public async Task Deberia_CerrarLosEnlacesDeReactivacionAunVivos()
    {
        var member = WorkspaceMember.CreateOwner(_workspace.Id, OwnerId);
        _workspaceRepository.FindActiveMemberAsync(_workspace.Id, OwnerId, Arg.Any<CancellationToken>())
            .Returns(member);
        var pending = WorkspaceReactivationRequest.Issue(
            _workspace.Id, Guid.NewGuid(), OwnerId, "hash", TimeSpan.FromDays(7));
        _reactivationRepository.ListOpenForWorkspaceAsync(_workspace.Id, Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceReactivationRequest> { pending });
        var sut = CreateSut();

        await sut.HandleAsync(_workspace.Id, OwnerId);

        pending.Status.Should().Be(ReactivationRequestStatuses.Closed);
    }

    [Fact]
    public async Task Deberia_OcultarElWorkspaceAQuienNoLoDioDeBaja()
    {
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(_workspace.Id, Guid.NewGuid());

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.WorkspaceNotFound);
        _workspace.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Deberia_Rechazar_UnWorkspaceQueNoEstaDadoDeBaja()
    {
        _workspace.Reactivate();
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(_workspace.Id, OwnerId);

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.WorkspaceNotFound);
    }
}
