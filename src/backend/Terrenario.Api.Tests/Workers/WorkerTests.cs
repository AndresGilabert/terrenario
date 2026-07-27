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
}
