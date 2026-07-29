using FluentAssertions;
using NSubstitute;
using Terrenario.Api.Application.Activities;
using Terrenario.Api.Application.Activities.Commands;
using Terrenario.Api.Application.Tasks;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Activities;
using Terrenario.Api.Domain.Operations;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Tasks;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Tests.Activities;

/// <summary>
/// Tests de los casos de uso de actividad (MVP-301) con repositorios mockeados: aislamiento por
/// Workspace (404), guarda de vínculos (<c>FOREIGN_KEY_WORKSPACE_MISMATCH</c>), edición parcial y
/// concurrencia optimista (CA-4). La traducción a SQL se cubre aparte contra SQLite real (P-014).
/// </summary>
public class ActivityHandlersTests
{
    private readonly IActivityRepository _activities = Substitute.For<IActivityRepository>();
    private readonly IPlotRepository _plots = Substitute.For<IPlotRepository>();
    private readonly ISeasonRepository _seasons = Substitute.For<ISeasonRepository>();
    private readonly IWorkerRepository _workers = Substitute.For<IWorkerRepository>();
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();

    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PlotId = Guid.NewGuid();
    private static readonly Guid SeasonId = Guid.NewGuid();
    private static readonly Guid WorkerId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 9, 20);

    public ActivityHandlersTests()
    {
        // Por defecto todos los vínculos existen en el Workspace activo.
        _plots.FindByIdAsync(WorkspaceId, PlotId, Arg.Any<CancellationToken>())
            .Returns(Plot.Create(WorkspaceId, "Olivar Alto", "propia"));
        _seasons.FindByIdAsync(WorkspaceId, SeasonId, Arg.Any<CancellationToken>())
            .Returns(Season.Create(WorkspaceId, "2026/2027", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28)));
        _workers.FindByIdAsync(WorkspaceId, WorkerId, Arg.Any<CancellationToken>())
            .Returns(Worker.Create(WorkspaceId, "Antonio", null));
    }

    private ActivityLinkResolver Resolver() => new(_plots, _seasons, _workers, _tasks);

    private TaskCatalogPromoter Promoter() => new(_tasks);

    private CreateActivityHandler CreateSut() => new(_activities, Resolver(), Promoter());

    private UpdateActivityHandler UpdateSut() => new(_activities, Resolver(), Promoter());

    private DeleteActivityHandler DeleteSut() => new(_activities);

    private static CreateActivityCommand ValidCreate(Guid? taskId = null, string? taskText = "Poda")
        => new(WorkspaceId, UserId, Date, PlotId, SeasonId, WorkerId, taskId, taskText, 4m, 70m, null);

    private static Activity Existing(long version = 1)
    {
        var activity = Activity.Create(
            WorkspaceId, PlotId, SeasonId, WorkerId, Date, 4m, null, "Poda", 70m, null, UserId);

        for (var i = 1; i < version; i++)
            activity.Update(PlotId, SeasonId, WorkerId, Date, 4m, null, "Poda", 70m, null, UserId);

        return activity;
    }

    private static ActivityView ViewOf(Activity activity) => new(
        activity.Id, WorkspaceId, PlotId, "Olivar Alto", SeasonId, "2026/2027",
        new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28), WorkerId, "Antonio",
        activity.Date, activity.Hours, activity.TaskId, null, activity.TaskText, activity.ManualCost,
        activity.Description, activity.Version, activity.CreatedAt, activity.UpdatedAt);

    // ── Alta ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Deberia_PersistirActividad()
    {
        Activity? added = null;
        await _activities.AddAsync(Arg.Do<Activity>(a => added = a), Arg.Any<CancellationToken>());
        _activities.GetViewAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(added!));

        var result = await CreateSut().HandleAsync(ValidCreate());

        result.Activity.Should().NotBeNull();
        result.TaskCatalogOutcome.Should().BeNull();
        added.Should().NotBeNull();
        added!.WorkspaceId.Should().Be(WorkspaceId);
        added.Version.Should().Be(1);
        await _activities.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Deberia_RechazarTerrenoDeOtroWorkspace_SinPersistir()
    {
        _plots.FindByIdAsync(WorkspaceId, PlotId, Arg.Any<CancellationToken>()).Returns((Plot?)null);

        var act = () => CreateSut().HandleAsync(ValidCreate());

        (await act.Should().ThrowAsync<ActivityValidationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ForeignKeyWorkspaceMismatch);
        await _activities.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Deberia_RechazarTareaDeCatalogoInexistente()
    {
        var taskId = Guid.NewGuid();
        _tasks.FindByIdAsync(WorkspaceId, taskId, Arg.Any<CancellationToken>()).Returns((TaskItem?)null);

        var act = () => CreateSut().HandleAsync(ValidCreate(taskId: taskId, taskText: null));

        (await act.Should().ThrowAsync<ActivityValidationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ForeignKeyWorkspaceMismatch);
    }

    [Fact]
    public async Task Create_Deberia_ValidarElDominio_AntesDeConsultarLosMaestros()
    {
        // El 400 de forma va antes que las cuatro consultas a los maestros
        var invalid = ValidCreate() with { Hours = 0m };

        var act = () => CreateSut().HandleAsync(invalid);

        await act.Should().ThrowAsync<ActivityValidationException>();
        await _plots.DidNotReceive().FindByIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── Edición ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Deberia_Devolver404_SiNoEstaEnElWorkspace()
    {
        _activities.FindByIdAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Activity?)null);

        var result = await UpdateSut().HandleAsync(UpdateCommand(Guid.NewGuid(), 1));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_Deberia_Rechazar409_SiLaVersionEstaDesfasada()
    {
        // CA-4 — la edición con versión vieja no sobrescribe en silencio
        var activity = Existing(version: 3);
        _activities.FindByIdAsync(WorkspaceId, activity.Id, Arg.Any<CancellationToken>()).Returns(activity);

        var act = () => UpdateSut().HandleAsync(UpdateCommand(activity.Id, expectedVersion: 2));

        (await act.Should().ThrowAsync<ConcurrencyConflictException>())
            .Which.CurrentVersion.Should().Be(3);
        await _activities.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_Deberia_ConservarLosCamposAusentes()
    {
        // Regresión de PATCH parcial: cambiar solo las horas no toca la tarea ni el coste
        var activity = Existing();
        _activities.FindByIdAsync(WorkspaceId, activity.Id, Arg.Any<CancellationToken>()).Returns(activity);
        _activities.GetViewAsync(WorkspaceId, activity.Id, Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(activity));

        await UpdateSut().HandleAsync(
            UpdateCommand(activity.Id, 1) with { Hours = FieldUpdate<decimal>.Set(6m) });

        activity.Hours.Should().Be(6m);
        activity.TaskText.Should().Be("Poda");
        activity.ManualCost.Should().Be(70m);
        activity.Version.Should().Be(2);
    }

    [Fact]
    public async Task Update_Deberia_SustituirElParDeTareaCompleto()
    {
        // RN-025 — enviar solo `task_id` sobre una actividad con texto libre limpia el texto,
        // en vez de dejar los dos informados y hacer que el dominio rechace la petición.
        var activity = Existing();
        var taskId = Guid.NewGuid();
        _activities.FindByIdAsync(WorkspaceId, activity.Id, Arg.Any<CancellationToken>()).Returns(activity);
        _tasks.FindByIdAsync(WorkspaceId, taskId, Arg.Any<CancellationToken>())
            .Returns(TaskItem.Create(WorkspaceId, "Poda"));
        _activities.GetViewAsync(WorkspaceId, activity.Id, Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(activity));

        await UpdateSut().HandleAsync(
            UpdateCommand(activity.Id, 1) with { TaskId = FieldUpdate<Guid?>.Set(taskId) });

        activity.TaskId.Should().Be(taskId);
        activity.TaskText.Should().BeNull();
    }

    // ── Guardado de la tarea libre en el catálogo (MVP-302) ─────────────────

    [Fact]
    public async Task Create_Deberia_GuardarLaTareaLibreEnElCatalogo_Y_Referenciarla()
    {
        // CA-1/CA-2 — la tarea escrita a mano se guarda sin salir del flujo de actividad, y la
        // actividad pasa a referenciarla por id en vez de arrastrar el texto suelto.
        Activity? added = null;
        TaskItem? addedTask = null;
        await _activities.AddAsync(Arg.Do<Activity>(a => added = a), Arg.Any<CancellationToken>());
        await _tasks.AddAsync(Arg.Do<TaskItem>(t => addedTask = t), Arg.Any<CancellationToken>());
        _activities.GetViewAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(added!));

        var result = await CreateSut().HandleAsync(
            ValidCreate(taskText: "Poda de formación") with { SaveTaskToCatalog = true });

        result.TaskCatalogOutcome.Should().Be(TaskCatalogOutcome.Created);
        addedTask.Should().NotBeNull();
        addedTask!.Name.Should().Be("Poda de formación");
        added!.TaskId.Should().Be(addedTask.Id);
        added.TaskText.Should().BeNull();
        // CA-3 — una sola unidad de trabajo: la tarea y la actividad se guardan juntas
        await _activities.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _tasks.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Deberia_ReutilizarLaTareaExistente_SinRomperLaActividad()
    {
        var existing = TaskItem.Create(WorkspaceId, "Poda");
        _tasks.FindByNameAsync(
                WorkspaceId,
                Arg.Is<string>(n => string.Equals(n, "Poda", StringComparison.OrdinalIgnoreCase)),
                Arg.Any<CancellationToken>())
            .Returns(existing);
        Activity? added = null;
        await _activities.AddAsync(Arg.Do<Activity>(a => added = a), Arg.Any<CancellationToken>());
        _activities.GetViewAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(added!));

        var result = await CreateSut().HandleAsync(
            ValidCreate(taskText: "poda") with { SaveTaskToCatalog = true });

        result.TaskCatalogOutcome.Should().Be(TaskCatalogOutcome.Reused);
        added!.TaskId.Should().Be(existing.Id);
        await _tasks.DidNotReceive().AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Deberia_Rechazar_SiLaTareaYaVieneDelCatalogo()
    {
        // Pedir guardar en el catálogo algo que ya está en él no es una operación silenciosa: se dice
        var taskId = Guid.NewGuid();
        _tasks.FindByIdAsync(WorkspaceId, taskId, Arg.Any<CancellationToken>())
            .Returns(TaskItem.Create(WorkspaceId, "Poda"));

        var act = () => CreateSut().HandleAsync(
            ValidCreate(taskId: taskId, taskText: null) with { SaveTaskToCatalog = true });

        (await act.Should().ThrowAsync<ActivityValidationException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ValidationActivityTaskNotFreeText);
        await _activities.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_Deberia_PromocionarLaTareaDeUnaActividadYaRegistrada()
    {
        // CA-3 — promocionar la labor de una actividad existente sin reescribirla y sin romperla:
        // `PATCH { save_task_to_catalog: true }` a secas.
        var activity = Existing();
        TaskItem? addedTask = null;
        _activities.FindByIdAsync(WorkspaceId, activity.Id, Arg.Any<CancellationToken>()).Returns(activity);
        await _tasks.AddAsync(Arg.Do<TaskItem>(t => addedTask = t), Arg.Any<CancellationToken>());
        _activities.GetViewAsync(WorkspaceId, activity.Id, Arg.Any<CancellationToken>())
            .Returns(ci => ViewOf(activity));

        var result = await UpdateSut().HandleAsync(
            UpdateCommand(activity.Id, 1) with { SaveTaskToCatalog = true });

        result!.TaskCatalogOutcome.Should().Be(TaskCatalogOutcome.Created);
        addedTask!.Name.Should().Be("Poda");
        activity.TaskId.Should().Be(addedTask.Id);
        activity.TaskText.Should().BeNull();
        // El usuario hizo un solo cambio: la versión sube una sola vez
        activity.Version.Should().Be(2);
    }

    // ── Borrado lógico ──────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Deberia_MarcarBajaLogica_SinBorrarLaFila()
    {
        // RN-037 — el registro desaparece del diario pero no de la base de datos
        var activity = Existing();
        _activities.FindByIdAsync(WorkspaceId, activity.Id, Arg.Any<CancellationToken>()).Returns(activity);

        var deleted = await DeleteSut().HandleAsync(
            new DeleteActivityCommand(WorkspaceId, UserId, activity.Id, 1));

        deleted.Should().BeTrue();
        activity.IsDeleted.Should().BeTrue();
        await _activities.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Deberia_Devolver404_SiYaNoExisteViva()
    {
        _activities.FindByIdAsync(WorkspaceId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Activity?)null);

        var deleted = await DeleteSut().HandleAsync(
            new DeleteActivityCommand(WorkspaceId, UserId, Guid.NewGuid(), 1));

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_Deberia_Rechazar409_SiLaVersionEstaDesfasada()
    {
        // CA-4 — borrar es lo menos reversible del diario: es donde más importa no pisar a otro
        var activity = Existing(version: 2);
        _activities.FindByIdAsync(WorkspaceId, activity.Id, Arg.Any<CancellationToken>()).Returns(activity);

        var act = () => DeleteSut().HandleAsync(
            new DeleteActivityCommand(WorkspaceId, UserId, activity.Id, 1));

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
        activity.IsDeleted.Should().BeFalse();
        await _activities.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static UpdateActivityCommand UpdateCommand(Guid activityId, long expectedVersion) => new(
        WorkspaceId,
        UserId,
        activityId,
        expectedVersion,
        FieldUpdate<DateOnly>.Absent,
        FieldUpdate<Guid>.Absent,
        FieldUpdate<Guid>.Absent,
        FieldUpdate<Guid>.Absent,
        FieldUpdate<Guid?>.Absent,
        FieldUpdate<string>.Absent,
        FieldUpdate<decimal>.Absent,
        FieldUpdate<decimal>.Absent,
        FieldUpdate<string>.Absent);
}
