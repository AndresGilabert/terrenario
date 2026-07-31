using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Tests.Workspaces;

/// <summary>
/// Tests del árbol de decisión de la baja resuelto en servidor (MVP-206). Es lo que permite a la UI
/// **exigir la decisión** al propietario único (CA-3) y anunciar el sucesor real en la reasignación
/// automática (CA-5) sin reimplementar la regla de propiedad en cliente.
/// </summary>
public class GetWorkspaceClosureOptionsHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();

    private static readonly Guid ActingUserId = Guid.NewGuid();
    private readonly Workspace _workspace = Workspace.Create(ActingUserId, "Finca El Olivar");

    private GetWorkspaceClosureOptionsHandler CreateSut(params WorkspaceMemberDetail[] members)
    {
        _workspaceRepository.FindByIdAsync(_workspace.Id, Arg.Any<CancellationToken>()).Returns(_workspace);
        _workspaceRepository.ListMembersAsync(_workspace.Id, Arg.Any<CancellationToken>())
            .Returns(members.ToList());

        // MVP-506 — el sucesor lo decide el repositorio, no este handler: reproducir el criterio en
        // memoria era incorrecto, porque el desempate por identificador no ordena igual en .NET que
        // en PostgreSQL. El doble aplica aquí el mismo criterio que la consulta real.
        var successor = members
            .Where(m => m.Status == WorkspaceMemberStatuses.Active
                && m.UserId != ActingUserId
                && m.Role == WorkspaceRoles.Owner)
            .OrderBy(m => m.JoinedAt)
            .ThenBy(m => m.UserId)
            .Select(m => WorkspaceMember.CreateOwner(_workspace.Id, m.UserId))
            .FirstOrDefault();

        _workspaceRepository
            .FindOtherActiveOwnerAsync(_workspace.Id, ActingUserId, Arg.Any<CancellationToken>())
            .Returns(successor);

        return new GetWorkspaceClosureOptionsHandler(_workspaceRepository);
    }

    private static WorkspaceMemberDetail Member(
        Guid userId,
        string name,
        string role,
        string status = WorkspaceMemberStatuses.Active,
        int joinedDaysAgo = 0) =>
        new(userId, name, $"{name.ToLowerInvariant()}@ejemplo.com", role, status,
            DateTimeOffset.UtcNow.AddDays(-joinedDaysAgo));

    [Fact]
    public async Task ConOtroPropietarioActivo_Deberia_SerReasignacionAutomatica()
    {
        var coOwnerId = Guid.NewGuid();
        var sut = CreateSut(
            Member(ActingUserId, "Antonio", WorkspaceRoles.Owner),
            Member(coOwnerId, "Marta", WorkspaceRoles.Owner, joinedDaysAgo: 30));

        var options = await sut.HandleAsync(_workspace.Id, ActingUserId);

        options.Mode.Should().Be(WorkspaceClosureModes.AutoTransfer);
        options.SuccessorDisplayName.Should().Be("Marta");
        options.ActiveOwners.Should().Be(2);
        options.IsOwner.Should().BeTrue();
    }

    [Fact]
    public async Task ConVariosCopropietarios_Deberia_AnunciarAlMasAntiguo()
    {
        // Mismo criterio que el traspaso automático del repositorio: el copropietario más antiguo.
        var recientId = Guid.NewGuid();
        var antiguoId = Guid.NewGuid();
        var sut = CreateSut(
            Member(ActingUserId, "Antonio", WorkspaceRoles.Owner),
            Member(recientId, "Zoe", WorkspaceRoles.Owner, joinedDaysAgo: 2),
            Member(antiguoId, "Bruno", WorkspaceRoles.Owner, joinedDaysAgo: 400));

        var options = await sut.HandleAsync(_workspace.Id, ActingUserId);

        options.SuccessorDisplayName.Should().Be("Bruno");
    }

    [Fact]
    public async Task ComoPropietarioUnicoConMiembros_Deberia_ExigirElegir()
    {
        var memberId = Guid.NewGuid();
        var sut = CreateSut(
            Member(ActingUserId, "Antonio", WorkspaceRoles.Owner),
            Member(memberId, "Lucia", WorkspaceRoles.Member));

        var options = await sut.HandleAsync(_workspace.Id, ActingUserId);

        options.Mode.Should().Be(WorkspaceClosureModes.Choose);
        options.SuccessorDisplayName.Should().BeNull();
        options.Candidates.Should().ContainSingle().Which.UserId.Should().Be(memberId);
    }

    [Fact]
    public async Task ComoPropietarioUnicoSinNadieMas_Deberia_DejarSoloLaBaja()
    {
        var revocadoId = Guid.NewGuid();
        var sut = CreateSut(
            Member(ActingUserId, "Antonio", WorkspaceRoles.Owner),
            Member(revocadoId, "Bruno", WorkspaceRoles.Member, WorkspaceMemberStatuses.Revoked));

        var options = await sut.HandleAsync(_workspace.Id, ActingUserId);

        // Una membresía revocada no es candidata: no daría un propietario con acceso.
        options.Mode.Should().Be(WorkspaceClosureModes.OnlyDelete);
        options.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task SinSerPropietario_Deberia_QuedarFueraDeLaAccion()
    {
        var ownerId = Guid.NewGuid();
        var sut = CreateSut(
            Member(ownerId, "Marta", WorkspaceRoles.Owner),
            Member(ActingUserId, "Lucia", WorkspaceRoles.Member));

        var options = await sut.HandleAsync(_workspace.Id, ActingUserId);

        options.Mode.Should().Be(WorkspaceClosureModes.NotOwner);
        options.IsOwner.Should().BeFalse();
    }
}
