using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Tests.Workers;

/// <summary>
/// Tests del agregado <see cref="Worker"/> (MVP-204). Cubren el alta mínima (solo nombre), la tarifa
/// horaria de referencia, las validaciones y la inactivación reversible (CA-3).
/// </summary>
public sealed class WorkerTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    [Fact]
    public void Create_Deberia_DarDeAltaConSoloNombre()
    {
        var worker = Worker.Create(WorkspaceId, "Antonio Jornalero");

        worker.Id.Should().NotBeEmpty();
        worker.WorkspaceId.Should().Be(WorkspaceId);
        worker.Name.Should().Be("Antonio Jornalero");
        worker.HourlyRate.Should().BeNull();
        worker.UserAccountId.Should().BeNull();
        worker.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Deberia_NormalizarNombreYAceptarTarifa()
    {
        var worker = Worker.Create(WorkspaceId, "  María  ", hourlyRate: 12.50m);

        worker.Name.Should().Be("María");
        worker.HourlyRate.Should().Be(12.50m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Deberia_RechazarNombreVacio(string name)
    {
        var act = () => Worker.Create(WorkspaceId, name);

        act.Should().Throw<WorkerValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredName);
    }

    [Fact]
    public void Create_Deberia_RechazarNombreLargo()
    {
        var act = () => Worker.Create(WorkspaceId, new string('x', Worker.NameMaxLength + 1));

        act.Should().Throw<WorkerValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationWorkerNameLength);
    }

    [Fact]
    public void Create_Deberia_RechazarTarifaNegativa()
    {
        var act = () => Worker.Create(WorkspaceId, "Antonio", hourlyRate: -1m);

        act.Should().Throw<WorkerValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRangeHourlyRate);
    }

    [Fact]
    public void Create_Deberia_RechazarWorkspaceInvalido()
    {
        var act = () => Worker.Create(Guid.Empty, "Antonio");

        act.Should().Throw<WorkerValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredWorkerWorkspace);
    }

    [Fact]
    public void Update_Deberia_ReemplazarDatos_SinCambiarEstado()
    {
        var worker = Worker.Create(WorkspaceId, "Antonio", hourlyRate: 10m);
        var createdAt = worker.CreatedAt;

        worker.Update("Antonio Podador", hourlyRate: 15m);

        worker.Name.Should().Be("Antonio Podador");
        worker.HourlyRate.Should().Be(15m);
        worker.IsActive.Should().BeTrue();
        worker.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Update_Deberia_PermitirLimpiarLaTarifa()
    {
        var worker = Worker.Create(WorkspaceId, "Antonio", hourlyRate: 10m);

        worker.Update("Antonio", hourlyRate: null);

        worker.HourlyRate.Should().BeNull();
    }

    [Fact]
    public void SetActive_Deberia_InactivarYReactivarSinBorrar()
    {
        var worker = Worker.Create(WorkspaceId, "Antonio");

        worker.SetActive(false);
        worker.IsActive.Should().BeFalse();

        worker.SetActive(true);
        worker.IsActive.Should().BeTrue();
    }

    // ── MVP-208 · el responsable con cuenta ────────────────────────────────────────────────────

    [Fact]
    public void CreateForMember_Deberia_NacerActivo_ConCuentaVinculada_Y_SinTarifa()
    {
        var userId = Guid.NewGuid();

        var worker = Worker.CreateForMember(WorkspaceId, userId, "  Andrés Gilabert  ");

        worker.UserAccountId.Should().Be(userId);
        worker.HasAccount.Should().BeTrue();
        worker.Name.Should().Be("Andrés Gilabert");
        worker.HourlyRate.Should().BeNull();
        worker.IsActive.Should().BeTrue();
        WorkerKinds.Of(worker).Should().Be(WorkerKinds.Member);
    }

    [Fact]
    public void Update_Deberia_RechazarRenombrarUnMiembro()
    {
        // CA-4 — el nombre llega de la identidad de Google (RN-036), no del maestro.
        var worker = Worker.CreateForMember(WorkspaceId, Guid.NewGuid(), "Andrés Gilabert");

        var act = () => worker.Update("Otro Nombre", hourlyRate: null);

        act.Should().Throw<WorkerBusinessRuleException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleWorkerIdentityManaged);
        worker.Name.Should().Be("Andrés Gilabert");
    }

    [Fact]
    public void SetActive_Deberia_RechazarInactivarUnMiembro()
    {
        // CA-4 — RN-027 obliga a que todo miembro sea seleccionable: la vía de retirarlo es revocar
        // su acceso, no inactivarlo a mano en el maestro.
        var worker = Worker.CreateForMember(WorkspaceId, Guid.NewGuid(), "Andrés Gilabert");

        var act = () => worker.SetActive(false);

        act.Should().Throw<WorkerBusinessRuleException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.BusinessRuleWorkerMembershipManaged);
        worker.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateHourlyRate_Deberia_PermitirseEnUnMiembro()
    {
        // CA-4 — la tarifa sí es editable: es dato operativo, no parte de su identidad.
        var worker = Worker.CreateForMember(WorkspaceId, Guid.NewGuid(), "Andrés Gilabert");

        worker.UpdateHourlyRate(21m);

        worker.HourlyRate.Should().Be(21m);
        worker.Name.Should().Be("Andrés Gilabert");
    }

    [Fact]
    public void SyncMembership_Deberia_SeguirALaMembresia_SinBorrarNada()
    {
        var worker = Worker.CreateForMember(WorkspaceId, Guid.NewGuid(), "Andrés Gilabert");

        worker.SyncMembership(false);
        worker.IsActive.Should().BeFalse();

        worker.SyncMembership(true);
        worker.IsActive.Should().BeTrue();
        worker.Name.Should().Be("Andrés Gilabert");
    }

    [Fact]
    public void SyncIdentityName_Deberia_AdoptarElNombreDeLaCuenta()
    {
        var worker = Worker.CreateForMember(WorkspaceId, Guid.NewGuid(), "Andrés Gilabert");

        worker.SyncIdentityName("Andrés G. Ruiz");

        worker.Name.Should().Be("Andrés G. Ruiz");
    }

    [Fact]
    public void WithSuffix_Deberia_RecortarParaNoDesbordarLaColumna()
    {
        var largo = new string('x', Worker.NameMaxLength);

        var resultado = Worker.WithSuffix(largo, 2);

        resultado.Should().EndWith(" (2)");
        resultado.Length.Should().Be(Worker.NameMaxLength);
    }
}
