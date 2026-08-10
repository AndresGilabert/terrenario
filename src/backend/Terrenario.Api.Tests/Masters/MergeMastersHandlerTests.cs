using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Masters;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Masters;

namespace Terrenario.Api.Tests.Masters;

/// <summary>
/// MVP-806 (CA-3/CA-4) — Reglas que deciden si una fusión es legítima. El reapuntado real y su
/// transacción se ejercitan contra PostgreSQL en <c>MasterRepositoryPostgresTests</c>.
/// </summary>
public sealed class MergeMastersHandlerTests
{
    private readonly IMasterRepository _masters = Substitute.For<IMasterRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private MergeMastersHandler CreateSut() => new(_masters);

    private void Existing(MasterKind kind, Guid id, string name, bool identityManaged = false)
        => _masters.FindAsync(kind, WorkspaceId, id, Arg.Any<CancellationToken>())
            .Returns(new MasterRecord(id, name, identityManaged));

    [Fact]
    public async Task Deberia_DevolverNull_Cuando_LaFichaSupervivienteNoEstaEnElWorkspace()
    {
        var survivor = Guid.NewGuid();
        _masters.FindAsync(MasterKind.Worker, WorkspaceId, survivor, Arg.Any<CancellationToken>())
            .Returns((MasterRecord?)null);

        var result = await CreateSut().HandleAsync(
            MasterKind.Worker, WorkspaceId, UserId, survivor, Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task Deberia_Rechazar_ConUnaFichaAbsorbidaQueNoEstaEnElWorkspace()
    {
        // 400 y no 404: lo que no existe llega en el cuerpo, no en la ruta — mismo criterio que
        // FOREIGN_KEY_WORKSPACE_MISMATCH en las entidades operativas.
        var survivor = Guid.NewGuid();
        Existing(MasterKind.Worker, survivor, "Juan Pérez");
        _masters.FindAsync(MasterKind.Worker, WorkspaceId, Arg.Is<Guid>(id => id != survivor),
                Arg.Any<CancellationToken>())
            .Returns((MasterRecord?)null);

        var act = () => CreateSut().HandleAsync(
            MasterKind.Worker, WorkspaceId, UserId, survivor, Guid.NewGuid());

        await act.Should().ThrowAsync<MasterLinkException>();
        await _masters.DidNotReceive().MergeAsync(
            Arg.Any<MasterKind>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_RechazarFusionarUnaFichaConsigoMisma()
    {
        var id = Guid.NewGuid();
        Existing(MasterKind.Plot, id, "Bancal de arriba");

        var act = () => CreateSut().HandleAsync(MasterKind.Plot, WorkspaceId, UserId, id, id);

        (await act.Should().ThrowAsync<MasterOperationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleMasterMergeSelf);
    }

    [Fact]
    public async Task Deberia_FusionarLaCuadrillaEnElMiembro_Y_DevolverCuantosSeReapuntaron()
    {
        // CA-4, el escenario que motiva la historia: la cuadrilla que MVP-207 renombró « (2)» al
        // materializar al miembro homónimo de MVP-208.
        var member = Guid.NewGuid();
        var crew = Guid.NewGuid();
        Existing(MasterKind.Worker, member, "Juan Pérez", identityManaged: true);
        Existing(MasterKind.Worker, crew, "Juan Pérez (2)");
        _masters.MergeAsync(MasterKind.Worker, WorkspaceId, member, crew, UserId, Arg.Any<CancellationToken>())
            .Returns(7);

        var result = await CreateSut().HandleAsync(
            MasterKind.Worker, WorkspaceId, UserId, member, crew);

        result!.Survivor.Id.Should().Be(member);
        result.Absorbed.Name.Should().Be("Juan Pérez (2)");
        result.ReassignedCount.Should().Be(7);
    }

    [Fact]
    public async Task Deberia_RechazarAbsorberLaFichaDeUnMiembro()
    {
        // CA-4 por el otro lado: si sobreviviera la cuadrilla, el miembro se quedaría sin ficha en un
        // Workspace al que sigue teniendo acceso, y su nombre no es renombrable (RN-036). La regla se
        // enuncia sobre el absorbido, así que cubre también dos cuentas homónimas.
        var member = Guid.NewGuid();
        var crew = Guid.NewGuid();
        Existing(MasterKind.Worker, crew, "Juan Pérez (2)");
        Existing(MasterKind.Worker, member, "Juan Pérez", identityManaged: true);

        var act = () => CreateSut().HandleAsync(MasterKind.Worker, WorkspaceId, UserId, crew, member);

        (await act.Should().ThrowAsync<MasterOperationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleMasterMergeMemberSurvives);
        await _masters.DidNotReceive().MergeAsync(
            Arg.Any<MasterKind>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }
}
