using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Tasks;

namespace Terrenario.Api.Tests.Tasks;

/// <summary>
/// Tests del agregado <see cref="TaskItem"/> (MVP-205). Cubren el alta con el dato mínimo (nombre),
/// la normalización, las validaciones y la inactivación reversible (CA-3).
/// </summary>
public sealed class TaskItemTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    [Fact]
    public void Create_Deberia_DarDeAltaConSoloNombre()
    {
        var task = TaskItem.Create(WorkspaceId, "Poda de mantenimiento");

        task.Id.Should().NotBeEmpty();
        task.WorkspaceId.Should().Be(WorkspaceId);
        task.Name.Should().Be("Poda de mantenimiento");
        task.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Deberia_NormalizarElNombre()
    {
        var task = TaskItem.Create(WorkspaceId, "  Riego por goteo  ");

        task.Name.Should().Be("Riego por goteo");
    }

    [Fact]
    public void Create_Deberia_PermitirNacerInactiva()
    {
        var task = TaskItem.Create(WorkspaceId, "Vendimia", isActive: false);

        task.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Deberia_RechazarNombreVacio(string name)
    {
        var act = () => TaskItem.Create(WorkspaceId, name);

        act.Should().Throw<TaskValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredTaskName);
    }

    [Fact]
    public void Create_Deberia_RechazarNombreLargo()
    {
        var act = () => TaskItem.Create(WorkspaceId, new string('x', TaskItem.NameMaxLength + 1));

        act.Should().Throw<TaskValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationTaskNameLength);
    }

    [Fact]
    public void Create_Deberia_RechazarWorkspaceInvalido()
    {
        var act = () => TaskItem.Create(Guid.Empty, "Poda");

        act.Should().Throw<TaskValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredTaskWorkspace);
    }

    [Fact]
    public void Rename_Deberia_CambiarNombre_SinCambiarEstado()
    {
        var task = TaskItem.Create(WorkspaceId, "Poda");
        var createdAt = task.CreatedAt;

        task.Rename("Poda en verde");

        task.Name.Should().Be("Poda en verde");
        task.IsActive.Should().BeTrue();
        task.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void SetActive_Deberia_InactivarYReactivarSinBorrar()
    {
        // CA-3 — una tarea con histórico se inactiva (reversible), nunca se borra.
        var task = TaskItem.Create(WorkspaceId, "Poda");

        task.SetActive(false);
        task.IsActive.Should().BeFalse();
        task.Name.Should().Be("Poda");

        task.SetActive(true);
        task.IsActive.Should().BeTrue();
    }

    [Fact]
    public void NormalizeName_Deberia_ValidarSinMutarNadaYRecortar()
    {
        TaskItem.NormalizeName("  Abonado  ").Should().Be("Abonado");

        var act = () => TaskItem.NormalizeName("   ");
        act.Should().Throw<TaskValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationRequiredTaskName);
    }
}
