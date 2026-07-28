using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests del renombrado del Workspace activo (MVP-206, HU-1/CA-1). Permisos planos (RN-034): no hay
/// guarda de rol; la única condición es que el Workspace exista y esté vivo.
/// </summary>
public class RenameWorkspaceHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly Workspace _workspace = Workspace.Create(Guid.NewGuid(), "Finca Vieja");

    private RenameWorkspaceHandler CreateSut(Workspace? workspace)
    {
        _workspaceRepository.FindByIdAsync(_workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        return new RenameWorkspaceHandler(_workspaceRepository);
    }

    [Fact]
    public async Task Deberia_RenombrarYPersistir()
    {
        var sut = CreateSut(_workspace);

        var result = await sut.HandleAsync(_workspace.Id, "  Finca Nueva ");

        result.Name.Should().Be("Finca Nueva");
        _workspace.Name.Should().Be("Finca Nueva");
        await _workspaceRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Rechazar_UnNombreVacio()
    {
        var sut = CreateSut(_workspace);

        var act = async () => await sut.HandleAsync(_workspace.Id, "   ");

        (await act.Should().ThrowAsync<WorkspaceValidationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredWorkspaceName);
        await _workspaceRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_Rechazar_SiElWorkspaceYaNoEstaDisponible()
    {
        // Un Workspace dado de baja no lo devuelve FindByIdAsync: para el resto de la app no existe.
        var sut = CreateSut(null);

        var act = async () => await sut.HandleAsync(_workspace.Id, "Finca Nueva");

        (await act.Should().ThrowAsync<WorkspaceMemberException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.WorkspaceNotFound);
    }
}
