using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests de la regla de no-orfandad de la baja de cuenta (MVP-206, CA-9). El flujo completo de baja
/// de cuenta es alcance de otra historia (P-024); lo que se entrega y se prueba aquí es la guarda que
/// impedirá completarla mientras la cuenta sea propietaria única de algún Workspace.
/// </summary>
public class WorkspaceOwnershipGuardTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private static readonly Guid UserId = Guid.NewGuid();

    private WorkspaceOwnershipGuard CreateSut(params SoleOwnedWorkspace[] pending)
    {
        _workspaceRepository.ListSoleOwnedAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(pending.ToList());
        return new WorkspaceOwnershipGuard(_workspaceRepository);
    }

    [Fact]
    public async Task Deberia_ImpedirLaBaja_MientrasQuedeUnWorkspaceSinResolver()
    {
        var sut = CreateSut(new SoleOwnedWorkspace(Guid.NewGuid(), "Finca El Olivar", OtherActiveMembers: 2));

        var act = async () => await sut.EnsureAccountClosureAllowedAsync(UserId);

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleWorkspaceOwnershipUnresolved);
    }

    [Fact]
    public async Task Deberia_PermitirLaBaja_CuandoNoQuedaNingunaPropiedadUnica()
    {
        var sut = CreateSut();

        var act = async () => await sut.EnsureAccountClosureAllowedAsync(UserId);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Deberia_AnticiparSiCabeTraspasar_EnCadaObligacion()
    {
        var sut = CreateSut(
            new SoleOwnedWorkspace(Guid.NewGuid(), "Con miembros", OtherActiveMembers: 3),
            new SoleOwnedWorkspace(Guid.NewGuid(), "Sin nadie más", OtherActiveMembers: 0));

        var obligations = await sut.ListObligationsAsync(UserId);

        obligations.IsClear.Should().BeFalse();
        obligations.Workspaces.Should().HaveCount(2);
        // Sin más miembros activos solo cabe la baja lógica: no hay a quién traspasar.
        obligations.Workspaces.Last().OtherActiveMembers.Should().Be(0);
    }
}
