using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Masters;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Masters;

namespace Terrenario.Api.Tests.Masters;

/// <summary>
/// MVP-806 (CA-1/CA-2) — Reglas del borrado físico de una ficha de maestro, con el repositorio
/// mockeado: aquí se comprueba <b>qué se decide</b>. Que el recuento se traduzca bien a SQL es cosa de
/// <c>MasterRepositoryPostgresTests</c>, y que las cuatro superficies respondan lo mismo, de
/// <c>MasterDepurationIntegrationTests</c>.
/// </summary>
public sealed class DeleteMasterHandlerTests
{
    private readonly IMasterRepository _masters = Substitute.For<IMasterRepository>();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private DeleteMasterHandler CreateSut() => new(_masters);

    private void Existing(MasterKind kind, Guid id, string name, bool identityManaged = false)
        => _masters.FindAsync(kind, WorkspaceId, id, Arg.Any<CancellationToken>())
            .Returns(new MasterRecord(id, name, identityManaged));

    [Fact]
    public async Task Deberia_DevolverNull_Cuando_LaFichaNoEstaEnElWorkspace()
    {
        var id = Guid.NewGuid();
        _masters.FindAsync(MasterKind.Plot, WorkspaceId, id, Arg.Any<CancellationToken>())
            .Returns((MasterRecord?)null);

        var result = await CreateSut().HandleAsync(MasterKind.Plot, WorkspaceId, id);

        result.Should().BeNull();
        await _masters.DidNotReceive().DeleteAsync(
            Arg.Any<MasterKind>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_BorrarLaFichaSinUso()
    {
        var id = Guid.NewGuid();
        Existing(MasterKind.Plot, id, "Bancal de arriba");
        _masters.CountUsageAsync(MasterKind.Plot, WorkspaceId, id, Arg.Any<CancellationToken>())
            .Returns(MasterUsage.None);

        var result = await CreateSut().HandleAsync(MasterKind.Plot, WorkspaceId, id);

        result!.Name.Should().Be("Bancal de arriba");
        await _masters.Received(1).DeleteAsync(
            MasterKind.Plot, WorkspaceId, id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_ComprobarElUsoAntesDeBorrar_Y_DecirCuantosRegistrosLoReferencian()
    {
        // CA-2 — el mensaje tiene que traer la cifra: un «no se puede» a secas deja al usuario sin
        // saber dónde mirar. Y el desglose por tipo es lo que delata si faltara una referencia.
        var id = Guid.NewGuid();
        Existing(MasterKind.Plot, id, "Bancal de arriba");
        _masters.CountUsageAsync(MasterKind.Plot, WorkspaceId, id, Arg.Any<CancellationToken>())
            .Returns(new MasterUsage([
                new MasterUsageReference("actividad", "actividades", 2),
                new MasterUsageReference("cosecha", "cosechas", 1)
            ]));

        var act = () => CreateSut().HandleAsync(MasterKind.Plot, WorkspaceId, id);

        var error = (await act.Should().ThrowAsync<MasterOperationException>()).Which;
        error.ErrorCode.Should().Be(ErrorCodes.BusinessRuleMasterInUse);
        error.Message.Should().Be(
            "No se puede eliminar el terreno «Bancal de arriba»: 2 actividades y 1 cosecha lo referencian.");
        await _masters.DidNotReceive().DeleteAsync(
            Arg.Any<MasterKind>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deberia_ConcordarElMensaje_ConElGeneroDelMaestro()
    {
        // Un mensaje que lee el usuario no puede estar mal concordado, y con cuatro maestros de dos
        // géneros es justo lo que pasa si se escribe una sola plantilla «el … lo referencian».
        var id = Guid.NewGuid();
        Existing(MasterKind.Task, id, "Poda");
        _masters.CountUsageAsync(MasterKind.Task, WorkspaceId, id, Arg.Any<CancellationToken>())
            .Returns(new MasterUsage([new MasterUsageReference("actividad", "actividades", 1)]));

        var act = () => CreateSut().HandleAsync(MasterKind.Task, WorkspaceId, id);

        (await act.Should().ThrowAsync<MasterOperationException>()).Which.Message.Should().Be(
            "No se puede eliminar la tarea «Poda»: 1 actividad la referencia.");
    }

    [Fact]
    public async Task Deberia_RechazarBorrarLaFichaDeUnMiembro_AunqueNoTengaHistorico()
    {
        // MVP-208 (CA-4) — su ficha la gobierna la membresía: borrarla dejaría a alguien con acceso al
        // Workspace sin fila de responsable, contra el índice único parcial que garantiza que la tiene.
        var id = Guid.NewGuid();
        Existing(MasterKind.Worker, id, "Andrés Gilabert", identityManaged: true);
        _masters.CountUsageAsync(MasterKind.Worker, WorkspaceId, id, Arg.Any<CancellationToken>())
            .Returns(MasterUsage.None);

        var act = () => CreateSut().HandleAsync(MasterKind.Worker, WorkspaceId, id);

        (await act.Should().ThrowAsync<MasterOperationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleWorkerMembershipManaged);
        await _masters.DidNotReceive().DeleteAsync(
            Arg.Any<MasterKind>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
