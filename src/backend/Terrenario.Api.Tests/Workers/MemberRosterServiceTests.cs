using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Workers;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Tests.Workers;

/// <summary>
/// Tests del servicio que mantiene el maestro de responsables alineado con la membresía (MVP-208,
/// CA-1/CA-4/CA-5). Es la pieza que cierra `P-034`: sin ella un miembro elegido como responsable no
/// se puede guardar, porque su `user_id` no es un `workers.id`.
///
/// Cubren también el desempate de nombres, que es lo que hace que el índice único ya entregado por
/// MVP-207 pueda cubrir la unión miembro/cuadrilla sin bloquear a nadie (hallazgo R-16).
/// </summary>
public sealed class MemberRosterServiceTests
{
    private readonly IWorkerRepository _workerRepository = Substitute.For<IWorkerRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private MemberRosterService CreateSut() => new(_workerRepository);

    [Fact]
    public async Task EnsureMemberAsync_Deberia_MaterializarLaFila_Cuando_NoLaTenia()
    {
        // Arrange — CA-1: el miembro entra al maestro con su nombre de Google
        var sut = CreateSut();

        // Act
        await sut.EnsureMemberAsync(WorkspaceId, UserId, "Andrés Gilabert");

        // Assert
        await _workerRepository.Received(1).AddAsync(
            Arg.Is<Worker>(w => w.WorkspaceId == WorkspaceId
                && w.UserAccountId == UserId
                && w.Name == "Andrés Gilabert"
                && w.IsActive),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureMemberAsync_Deberia_ReactivarLaFilaExistente_SinDuplicarla()
    {
        // Arrange — quien fue revocado y vuelve por una invitación nueva recupera su fila (CA-4)
        var existente = Worker.CreateForMember(WorkspaceId, UserId, "Andrés Gilabert");
        existente.SyncMembership(false);
        _workerRepository.FindByUserAccountAsync(WorkspaceId, UserId, Arg.Any<CancellationToken>())
            .Returns(existente);
        var sut = CreateSut();

        // Act
        await sut.EnsureMemberAsync(WorkspaceId, UserId, "Andrés Gilabert");

        // Assert
        existente.IsActive.Should().BeTrue();
        await _workerRepository.DidNotReceive().AddAsync(Arg.Any<Worker>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureMemberAsync_Deberia_RenombrarALaCuadrilla_Y_DejarElNombreAlMiembro()
    {
        // Arrange — CA-5/R-16: el nombre lo conserva el miembro, que no es renombrable
        var cuadrilla = Worker.Create(WorkspaceId, "Andrés Gilabert");
        _workerRepository.FindByNameAsync(
                WorkspaceId, "Andrés Gilabert", null, Arg.Any<CancellationToken>())
            .Returns(cuadrilla);
        var sut = CreateSut();

        // Act
        await sut.EnsureMemberAsync(WorkspaceId, UserId, "Andrés Gilabert");

        // Assert
        cuadrilla.Name.Should().Be("Andrés Gilabert (2)");
        await _workerRepository.Received(1).AddAsync(
            Arg.Is<Worker>(w => w.Name == "Andrés Gilabert" && w.UserAccountId == UserId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureMemberAsync_Deberia_BuscarElPrimerSufijoLibre_ParaLaCuadrilla()
    {
        // Arrange — el nombre generado puede chocar con uno que ya existía («X» junto a un «X (2)»)
        var cuadrilla = Worker.Create(WorkspaceId, "Andrés Gilabert");
        _workerRepository.FindByNameAsync(
                WorkspaceId, "Andrés Gilabert", null, Arg.Any<CancellationToken>())
            .Returns(cuadrilla);
        _workerRepository.ExistsWithNameAsync(
                WorkspaceId, "Andrés Gilabert (2)", cuadrilla.Id, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        // Act
        await sut.EnsureMemberAsync(WorkspaceId, UserId, "Andrés Gilabert");

        // Assert
        cuadrilla.Name.Should().Be("Andrés Gilabert (3)");
    }

    [Fact]
    public async Task EnsureMemberAsync_Deberia_SufijarAlQueLlega_Cuando_ElNombreLoOcupaOtroMiembro()
    {
        // Arrange — dos cuentas de Google homónimas: ninguna es renombrable, así que el sufijo lo
        // toma quien entra. Sin esto la persona no podría entrar en el Workspace.
        var otroMiembro = Worker.CreateForMember(WorkspaceId, Guid.NewGuid(), "Andrés Gilabert");
        _workerRepository.FindByNameAsync(
                WorkspaceId, "Andrés Gilabert", null, Arg.Any<CancellationToken>())
            .Returns(otroMiembro);
        var sut = CreateSut();

        // Act
        await sut.EnsureMemberAsync(WorkspaceId, UserId, "Andrés Gilabert");

        // Assert
        otroMiembro.Name.Should().Be("Andrés Gilabert");
        await _workerRepository.Received(1).AddAsync(
            Arg.Is<Worker>(w => w.Name == "Andrés Gilabert (2)" && w.UserAccountId == UserId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SuspendMemberAsync_Deberia_InactivarLaFila_SinBorrarla()
    {
        // Arrange — CA-4: revocar el acceso retira al responsable sin invalidar lo que le referencia
        var worker = Worker.CreateForMember(WorkspaceId, UserId, "Andrés Gilabert");
        _workerRepository.FindByUserAccountAsync(WorkspaceId, UserId, Arg.Any<CancellationToken>())
            .Returns(worker);
        var sut = CreateSut();

        // Act
        await sut.SuspendMemberAsync(WorkspaceId, UserId);

        // Assert
        worker.IsActive.Should().BeFalse();
        worker.Name.Should().Be("Andrés Gilabert");
    }

    [Fact]
    public async Task SuspendMemberAsync_Deberia_SerNoOp_Cuando_LaPersonaNoTieneFila()
    {
        // Miembros revocados antes de MVP-208: nunca se materializaron.
        var sut = CreateSut();

        var act = () => sut.SuspendMemberAsync(WorkspaceId, UserId);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncIdentityAsync_Deberia_PropagarElNombreATodosSusWorkspaces()
    {
        // RN-036 — el nombre del responsable con cuenta es el de su cuenta de Google
        var otroWorkspaceId = Guid.NewGuid();
        var uno = Worker.CreateForMember(WorkspaceId, UserId, "Andrés Gilabert");
        var otro = Worker.CreateForMember(otroWorkspaceId, UserId, "Andrés Gilabert");
        _workerRepository.ListByUserAccountAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new List<Worker> { uno, otro });
        var sut = CreateSut();

        // Act
        await sut.SyncIdentityAsync(UserId, "Andrés G. Ruiz");

        // Assert
        uno.Name.Should().Be("Andrés G. Ruiz");
        otro.Name.Should().Be("Andrés G. Ruiz");
    }

    [Fact]
    public async Task SyncIdentityAsync_Deberia_ApartarALaCuadrillaQueOcupabaElNombreNuevo()
    {
        // El punto que el spec pedía cerrar antes de implementar: si el nombre nuevo de Google choca,
        // el sufijo lo recibe la fila de cuadrilla, no el miembro.
        var miembro = Worker.CreateForMember(WorkspaceId, UserId, "Andrés Gilabert");
        var cuadrilla = Worker.Create(WorkspaceId, "Andrés G. Ruiz");
        _workerRepository.ListByUserAccountAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new List<Worker> { miembro });
        _workerRepository.FindByNameAsync(
                WorkspaceId, "Andrés G. Ruiz", miembro.Id, Arg.Any<CancellationToken>())
            .Returns(cuadrilla);
        var sut = CreateSut();

        // Act
        await sut.SyncIdentityAsync(UserId, "Andrés G. Ruiz");

        // Assert
        miembro.Name.Should().Be("Andrés G. Ruiz");
        cuadrilla.Name.Should().Be("Andrés G. Ruiz (2)");
    }

    [Fact]
    public async Task SyncIdentityAsync_Deberia_IgnorarUnCambioSoloDeMayusculas_SinDesempatar()
    {
        // La fila ya ocupa ese hueco del índice: compararía consigo misma.
        var miembro = Worker.CreateForMember(WorkspaceId, UserId, "Andrés Gilabert");
        _workerRepository.ListByUserAccountAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new List<Worker> { miembro });
        var sut = CreateSut();

        // Act
        await sut.SyncIdentityAsync(UserId, "ANDRÉS GILABERT");

        // Assert
        miembro.Name.Should().Be("ANDRÉS GILABERT");
        await _workerRepository.DidNotReceive().FindByNameAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }
}
