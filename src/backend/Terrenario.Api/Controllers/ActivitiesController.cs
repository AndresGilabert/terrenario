using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Activities;
using Terrenario.Api.Application.Activities.Commands;
using Terrenario.Api.Application.Tasks;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Activities;
using Terrenario.Api.Domain.Operations;
using Terrenario.Api.Domain.Tasks;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-301 — Actividades del diario del Workspace activo (<c>contratos-api.md §5</c>). Como el resto
/// de recursos con ámbito de Workspace se apoya en <see cref="RequireWorkspaceScopeAttribute"/>
/// (MVP-105): el Workspace se resuelve en servidor y nunca viaja como parámetro (RN-034).
///
/// Es la <b>primera entidad operativa crítica</b> del producto, así que estrena dos comportamientos
/// que reutilizarán compras, consumos y cosechas:
/// <list type="bullet">
/// <item><c>PATCH</c> y <c>DELETE</c> exigen <c>If-Match</c> con la versión vigente y responden
/// <c>409 CONFLICT_VERSION_MISMATCH</c> si no lo es (ADR-0005).</item>
/// <item><c>DELETE</c> es una <b>baja lógica</b> (RN-037): el registro desaparece del diario y de los
/// listados sin perderse en base de datos.</item>
/// </list>
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/activities")]
public sealed class ActivitiesController(
    CreateActivityHandler createActivityHandler,
    UpdateActivityHandler updateActivityHandler,
    DeleteActivityHandler deleteActivityHandler,
    ListActivitiesHandler listActivitiesHandler,
    GetActivityHandler getActivityHandler,
    IWorkspaceContext workspaceContext) : ControllerBase
{
    /// <summary>
    /// Diario de actividades del Workspace, por fecha de negocio descendente (RN-033). Filtros
    /// opcionales: <c>from</c>, <c>to</c>, <c>plot_id</c>, <c>season_id</c>, <c>worker_id</c>.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery(Name = "plot_id")] Guid? plotId,
        [FromQuery(Name = "season_id")] Guid? seasonId,
        [FromQuery(Name = "worker_id")] Guid? workerId,
        CancellationToken ct)
    {
        if (!TryParseDate(from, out var fromDate) || !TryParseDate(to, out var toDate))
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationRequired, "Las fechas de filtro deben tener el formato YYYY-MM-DD.")));

        var activities = await listActivitiesHandler.HandleAsync(
            workspaceContext.WorkspaceId,
            new ActivityFilter(fromDate, toDate, plotId, seasonId, workerId),
            ct);

        return Ok(new
        {
            data = activities.Select(activity => ToResponse(activity)),
            meta = new { total = activities.Count }
        });
    }

    /// <summary>
    /// Una actividad concreta del Workspace activo. Lo estrena <c>MVP-305</c>: el diario unificado
    /// muestra una proyección común de los tres tipos, así que para abrir el formulario de corrección
    /// necesita los campos completos de la actividad sin traerse el listado entero.
    /// </summary>
    [HttpGet("{activityId:guid}")]
    public async Task<IActionResult> GetById(Guid activityId, CancellationToken ct)
    {
        var activity = await getActivityHandler.HandleAsync(workspaceContext.WorkspaceId, activityId, ct);

        return activity is null
            ? NotFound(new ApiErrorResponse(ApiError.ActivityNotFound()))
            : Ok(ToResponse(activity));
    }

    /// <summary>Alta de actividad (HU-1, CA-1). El coste es siempre manual (RN-003).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateActivityRequest request, CancellationToken ct)
    {
        if (!TryParseDate(request.Date, out var date) || date is null)
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationActivityRequiredFields, "La fecha es obligatoria (formato YYYY-MM-DD).")));

        try
        {
            var result = await createActivityHandler.HandleAsync(
                new CreateActivityCommand(
                    workspaceContext.WorkspaceId,
                    User.GetUserId()!.Value,
                    date.Value,
                    request.PlotId,
                    request.SeasonId,
                    request.WorkerId,
                    request.TaskId,
                    request.TaskText,
                    request.Hours,
                    request.ManualCost,
                    request.Description,
                    request.SaveTaskToCatalog ?? false),
                ct);

            return CreatedAtAction(nameof(List), new { id = result.Activity.Id }, ToResponse(result));
        }
        catch (ActivityValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (TaskValidationException ex)
        {
            // MVP-302 — La tarea que se promociona la valida el catálogo (MVP-205) con sus propios
            // códigos: se dejan pasar tal cual en vez de traducirlos a un genérico de actividad.
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    /// <summary>
    /// Corrección de una actividad (HU-2). Edición parcial: un campo ausente conserva su valor.
    /// Exige <c>If-Match</c> con la versión vigente (CA-4).
    /// </summary>
    [HttpPatch("{activityId:guid}")]
    public async Task<IActionResult> Update(
        Guid activityId,
        [FromBody] Dictionary<string, JsonElement>? body,
        CancellationToken ct)
    {
        if (!IfMatchHeader.TryRead(Request.Headers, out var expectedVersion))
            return BadRequest(new ApiErrorResponse(ApiError.IfMatchRequired()));

        body ??= new Dictionary<string, JsonElement>();

        UpdateActivityCommand command;
        try
        {
            command = new UpdateActivityCommand(
                workspaceContext.WorkspaceId,
                User.GetUserId()!.Value,
                activityId,
                expectedVersion,
                ReadDate(body, "date"),
                ReadGuid(body, "plot_id"),
                ReadGuid(body, "season_id"),
                ReadGuid(body, "worker_id"),
                ReadNullableGuid(body, "task_id"),
                ReadString(body, "task_text"),
                ReadDecimal(body, "hours"),
                ReadDecimal(body, "manual_cost"),
                ReadString(body, "description"),
                ReadFlag(body, "save_task_to_catalog"));
        }
        catch (ActivityValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }

        try
        {
            var result = await updateActivityHandler.HandleAsync(command, ct);

            if (result is null)
                return NotFound(new ApiErrorResponse(ApiError.ActivityNotFound()));

            return Ok(ToResponse(result));
        }
        catch (ActivityValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (TaskValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (ConcurrencyConflictException ex)
        {
            return VersionConflict(ex);
        }
    }

    /// <summary>
    /// Eliminación <b>lógica</b> de una actividad (RN-037). Exige <c>If-Match</c>. La confirmación
    /// explícita del usuario la pone la UI (MVP-305).
    /// </summary>
    [HttpDelete("{activityId:guid}")]
    public async Task<IActionResult> Delete(Guid activityId, CancellationToken ct)
    {
        if (!IfMatchHeader.TryRead(Request.Headers, out var expectedVersion))
            return BadRequest(new ApiErrorResponse(ApiError.IfMatchRequired()));

        try
        {
            var deleted = await deleteActivityHandler.HandleAsync(
                new DeleteActivityCommand(
                    workspaceContext.WorkspaceId, User.GetUserId()!.Value, activityId, expectedVersion),
                ct);

            return deleted
                ? NoContent()
                : NotFound(new ApiErrorResponse(ApiError.ActivityNotFound()));
        }
        catch (ConcurrencyConflictException ex)
        {
            return VersionConflict(ex);
        }
    }

    /// <summary>
    /// <c>409</c> del contrato con la versión vigente en el cuerpo, para que el cliente pueda
    /// resolver el conflicto refrescando en vez de dejar al usuario sin salida (CA-4).
    /// </summary>
    private IActionResult VersionConflict(ConcurrencyConflictException ex)
        => Conflict(new
        {
            error = new
            {
                code = ErrorCodes.ConflictVersionMismatch,
                message = ex.Message,
                current_version = ex.CurrentVersion
            }
        });

    private static bool TryParseDate(string? raw, out DateOnly? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) return true;

        if (!DateOnly.TryParseExact(raw.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
            return false;

        value = parsed;
        return true;
    }

    private static FieldUpdate<string> ReadString(Dictionary<string, JsonElement> body, string key)
        => body.TryGetValue(key, out var el)
            ? FieldUpdate<string>.Set(el.ValueKind == JsonValueKind.Null ? null : el.GetString())
            : FieldUpdate<string>.Absent;

    private static FieldUpdate<DateOnly> ReadDate(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<DateOnly>.Absent;

        if (el.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(el.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
            return FieldUpdate<DateOnly>.Set(parsed);

        throw new ActivityValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser una fecha YYYY-MM-DD.");
    }

    private static FieldUpdate<Guid> ReadGuid(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<Guid>.Absent;
        if (el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out var parsed))
            return FieldUpdate<Guid>.Set(parsed);

        throw new ActivityValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser un identificador válido.");
    }

    /// <summary>Igual que <see cref="ReadGuid"/> pero admite <c>null</c> explícito (par tarea de RN-025).</summary>
    private static FieldUpdate<Guid?> ReadNullableGuid(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<Guid?>.Absent;
        if (el.ValueKind == JsonValueKind.Null) return FieldUpdate<Guid?>.Set(null);
        if (el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out var parsed))
            return FieldUpdate<Guid?>.Set(parsed);

        throw new ActivityValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser un identificador válido o null.");
    }

    /// <summary>
    /// Bandera de acción (no es un campo del recurso, así que no es un <see cref="FieldUpdate{T}"/>):
    /// ausente equivale a <c>false</c>. La usa <c>save_task_to_catalog</c> (MVP-302).
    /// </summary>
    private static bool ReadFlag(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return false;
        if (el.ValueKind is JsonValueKind.True or JsonValueKind.False) return el.GetBoolean();

        throw new ActivityValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser booleano.");
    }

    private static FieldUpdate<decimal> ReadDecimal(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<decimal>.Absent;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var parsed))
            return FieldUpdate<decimal>.Set(parsed);

        throw new ActivityValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser numérico.");
    }

    /// <summary>
    /// Respuesta de alta y edición: la actividad más qué pasó en el catálogo si se pidió guardar allí
    /// la tarea escrita a mano (MVP-302).
    /// </summary>
    private static object ToResponse(ActivitySaveResult result)
        => ToResponse(result.Activity, result.TaskCatalogOutcome);

    private static object ToResponse(ActivityView activity, TaskCatalogOutcome? taskCatalogOutcome = null) => new
    {
        id = activity.Id,
        workspace_id = activity.WorkspaceId,
        date = activity.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        plot_id = activity.PlotId,
        plot_name = activity.PlotName,
        season_id = activity.SeasonId,
        season_name = activity.SeasonName,
        worker_id = activity.WorkerId,
        worker_name = activity.WorkerName,
        task_id = activity.TaskId,
        task_name = activity.TaskName,
        task_text = activity.TaskText,
        // Texto de la tarea venga de donde venga (RN-025): evita que cada cliente rehaga el `??`.
        task = activity.Task,
        hours = activity.Hours,
        manual_cost = activity.ManualCost,
        description = activity.Description,
        // RN-023 — aviso no bloqueante de fecha fuera del rango de la temporada (CA-2).
        is_out_of_season_range = activity.IsOutOfSeasonRange,
        version = activity.Version,
        created_at = activity.CreatedAt,
        updated_at = activity.UpdatedAt,
        // MVP-302 — `created` / `reused` / `reactivated` cuando se pidió guardar la tarea en el
        // catálogo; `null` en las lecturas, donde no hay ninguna acción de catálogo asociada.
        task_catalog_outcome = taskCatalogOutcome?.ToString().ToLowerInvariant()
    };
}

/// <summary>
/// Alta de actividad (<c>contratos-api.md §5</c>). <c>task_id</c> y <c>task_text</c> son excluyentes
/// y al menos uno es obligatorio (RN-025); la validación vive en el dominio, no en anotaciones,
/// porque es una regla de negocio y no de forma.
/// </summary>
public sealed record CreateActivityRequest(
    [Required(ErrorMessage = "La fecha de la actividad es obligatoria.")]
    string Date,
    [property: JsonPropertyName("plot_id")] Guid PlotId,
    [property: JsonPropertyName("season_id")] Guid SeasonId,
    [property: JsonPropertyName("worker_id")] Guid WorkerId,
    [property: JsonPropertyName("task_id")] Guid? TaskId,
    [property: JsonPropertyName("task_text")] string? TaskText,
    decimal Hours,
    [property: JsonPropertyName("manual_cost")] decimal ManualCost,
    string? Description,
    /// <summary>
    /// MVP-302 — Guardar además <c>task_text</c> en el catálogo del Workspace para reutilizarla
    /// después (RN-026). Si el nombre ya existe se reutiliza —o se reactiva si estaba inactivada— en
    /// vez de crear una segunda tarea.
    /// </summary>
    [property: JsonPropertyName("save_task_to_catalog")] bool? SaveTaskToCatalog);
