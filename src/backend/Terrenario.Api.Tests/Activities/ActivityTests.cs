using FluentAssertions;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Activities;
using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Tests.Activities;

/// <summary>
/// Tests de dominio del agregado de actividad (MVP-301): reglas RN-001/RN-002/RN-003/RN-025 y el
/// patrón de concurrencia y borrado lógico que estrena para toda la épica (ADR-0005, RN-037).
/// </summary>
public class ActivityTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid PlotId = Guid.NewGuid();
    private static readonly Guid SeasonId = Guid.NewGuid();
    private static readonly Guid WorkerId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 9, 20);

    private static Activity CreateValid(
        Guid? taskId = null,
        string? taskText = "Poda de mantenimiento",
        decimal hours = 4.5m,
        decimal manualCost = 70m,
        string? description = null)
        => Activity.Create(
            WorkspaceId, PlotId, SeasonId, WorkerId, Date, hours, taskId, taskText, manualCost, description, UserId);

    [Fact]
    public void Create_Deberia_RegistrarActividadCompleta()
    {
        // CA-1 — todos los campos obligatorios que fija la KB
        var activity = CreateValid(description: "  Sector norte  ");

        activity.WorkspaceId.Should().Be(WorkspaceId);
        activity.PlotId.Should().Be(PlotId);
        activity.SeasonId.Should().Be(SeasonId);
        activity.WorkerId.Should().Be(WorkerId);
        activity.Date.Should().Be(Date);
        activity.Hours.Should().Be(4.5m);
        activity.TaskText.Should().Be("Poda de mantenimiento");
        activity.TaskId.Should().BeNull();
        activity.ManualCost.Should().Be(70m);
        activity.Description.Should().Be("Sector norte");
        activity.CreatedBy.Should().Be(UserId);
        activity.UpdatedBy.Should().Be(UserId);
        activity.Version.Should().Be(1);
        activity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_Deberia_AdmitirTareaDelCatalogo()
    {
        // RN-025 — la tarea puede venir del catálogo del Workspace (MVP-205)
        var taskId = Guid.NewGuid();

        var activity = CreateValid(taskId: taskId, taskText: null);

        activity.TaskId.Should().Be(taskId);
        activity.TaskText.Should().BeNull();
    }

    [Fact]
    public void Create_Deberia_RechazarActividadSinTarea()
    {
        // RN-025 — la tarea es obligatoria: del catálogo o en texto libre
        var act = () => CreateValid(taskId: null, taskText: "   ");

        act.Should().Throw<ActivityValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationActivityTaskRequired);
    }

    [Fact]
    public void Create_Deberia_RechazarTareaDuplicada_CatalogoYTextoLibre()
    {
        // Las dos a la vez podrían divergir y el diario no sabría cuál mostrar
        var act = () => CreateValid(taskId: Guid.NewGuid(), taskText: "Poda");

        act.Should().Throw<ActivityValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationActivityTaskRequired);
    }

    [Fact]
    public void Create_Deberia_RechazarTextoDeTareaDemasiadoLargo()
    {
        // La cota es la del catálogo, para que una tarea libre siempre quepa al guardarse (MVP-302)
        var act = () => CreateValid(taskText: new string('a', Activity.TaskTextMaxLength + 1));

        act.Should().Throw<ActivityValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationActivityTaskTextLength);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)]
    public void Create_Deberia_RechazarHorasFueraDeRango(decimal hours)
    {
        // RN-002 — sin tiempo dedicado no hay actividad
        var act = () => CreateValid(hours: hours);

        act.Should().Throw<ActivityValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationActivityHoursRange);
    }

    [Fact]
    public void Create_Deberia_RechazarCosteNegativo_PeroAdmitirCero()
    {
        // RN-003 — el coste es manual y obligatorio; 0 es un valor legítimo (labor propia sin coste)
        var act = () => CreateValid(manualCost: -1m);
        act.Should().Throw<ActivityValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationActivityCostRange);

        CreateValid(manualCost: 0m).ManualCost.Should().Be(0m);
    }

    [Fact]
    public void Create_Deberia_RechazarVinculosVacios()
    {
        // RN-001/RN-002/RN-021 — terreno, temporada y responsable forman el registro mínimo
        var act = () => Activity.Create(
            WorkspaceId, Guid.Empty, SeasonId, WorkerId, Date, 1m, null, "Poda", 0m, null, UserId);

        act.Should().Throw<ActivityValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationActivityRequiredFields);
    }

    [Fact]
    public void Create_Deberia_RechazarDescripcionDemasiadoLarga()
    {
        var act = () => CreateValid(description: new string('a', Activity.DescriptionMaxLength + 1));

        act.Should().Throw<ActivityValidationException>()
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationActivityDescriptionLength);
    }

    [Fact]
    public void Update_Deberia_IncrementarVersion_Y_CambiarLaTarea()
    {
        // ADR-0005 — cada mutación mueve la versión: quien tenga la anterior recibirá 409
        var activity = CreateValid();
        var taskId = Guid.NewGuid();

        activity.Update(PlotId, SeasonId, WorkerId, Date, 2m, taskId, null, 30m, "Corregido", UserId);

        activity.Version.Should().Be(2);
        activity.TaskId.Should().Be(taskId);
        activity.TaskText.Should().BeNull();
        activity.Hours.Should().Be(2m);
        activity.ManualCost.Should().Be(30m);
    }

    [Fact]
    public void EnsureVersion_Deberia_AceptarLaVigente_Y_RechazarLaDesfasada()
    {
        // CA-4 — editar con versión desfasada no sobrescribe en silencio
        var activity = CreateValid();
        activity.Update(PlotId, SeasonId, WorkerId, Date, 2m, null, "Riego", 10m, null, UserId);

        activity.Invoking(a => a.EnsureVersion(2)).Should().NotThrow();

        var conflict = activity.Invoking(a => a.EnsureVersion(1))
            .Should().Throw<ConcurrencyConflictException>().Which;
        conflict.CurrentVersion.Should().Be(2);
    }

    [Fact]
    public void Delete_Deberia_SerLogico_Y_Idempotente()
    {
        // RN-037 — la eliminación marca `deleted_at`; nunca hay borrado físico
        var activity = CreateValid();

        activity.Delete(UserId);

        activity.IsDeleted.Should().BeTrue();
        activity.DeletedAt.Should().NotBeNull();
        activity.Version.Should().Be(2);

        // Repetir no vuelve a mover la versión: no hay nada nuevo que registrar
        activity.Delete(UserId);
        activity.Version.Should().Be(2);
    }

    [Fact]
    public void Hours_Y_Coste_Deberian_RedondearseALaPrecisionPersistida()
    {
        // decimal(5,2) y decimal(10,2): se redondea en el dominio para que lo leído coincida con lo escrito
        var activity = CreateValid(hours: 1.239m, manualCost: 10.005m);

        activity.Hours.Should().Be(1.24m);
        activity.ManualCost.Should().Be(10.01m);
    }
}
