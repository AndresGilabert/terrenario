using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Workers;
using Terrenario.Api.Application.Workers.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-204 — Maestro de trabajadores del Workspace activo. Como el resto de recursos con ámbito de
/// Workspace, se apoya en <see cref="RequireWorkspaceScopeAttribute"/> (MVP-105): el Workspace activo
/// se resuelve en servidor y se lee de <see cref="IWorkspaceContext"/>, nunca del cliente (RN-034).
///
/// Alcance: alta, edición, listado e inactivación de trabajadores <b>sin cuenta vinculada</b>
/// (HU-1/HU-2, CA-2/CA-3). Los miembros del Workspace se exponen como seleccionables desde la vista
/// de personas (<c>/api/v1/workspace-members</c>), no como filas de este maestro (RN-027). El borrado
/// físico queda fuera: los trabajadores con histórico se inactivan.
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/workers")]
public sealed class WorkersController(
    CreateWorkerHandler createWorkerHandler,
    UpdateWorkerHandler updateWorkerHandler,
    ListWorkersHandler listWorkersHandler,
    IWorkspaceContext workspaceContext) : ControllerBase
{
    /// <summary>Lista los trabajadores del Workspace. Filtro opcional: <c>is_active</c>.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery(Name = "is_active")] bool? isActive,
        CancellationToken ct)
    {
        var workers = await listWorkersHandler.HandleAsync(workspaceContext.WorkspaceId, isActive, ct);

        return Ok(new
        {
            data = workers.Select(ToResponse),
            meta = new { total = workers.Count }
        });
    }

    /// <summary>Alta de trabajador sin cuenta. Solo <c>name</c> es obligatorio (CA-2).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkerRequest request, CancellationToken ct)
    {
        try
        {
            var worker = await createWorkerHandler.HandleAsync(
                new CreateWorkerCommand(
                    workspaceContext.WorkspaceId,
                    request.Name,
                    request.HourlyRate),
                ct);

            return CreatedAtAction(nameof(List), new { id = worker.Id }, ToResponse(worker));
        }
        catch (WorkerValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (WorkerConflictException ex)
        {
            return Conflict(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    /// <summary>
    /// Edición parcial de un trabajador o cambio de su estado de actividad (inactivación CA-3 con
    /// <c>is_active: false</c>). Solo se modifican los campos presentes en el cuerpo: omitir un campo
    /// mantiene su valor; enviarlo vacío lo limpia.
    /// </summary>
    [HttpPatch("{workerId:guid}")]
    public async Task<IActionResult> Update(
        Guid workerId,
        [FromBody] Dictionary<string, JsonElement>? body,
        CancellationToken ct)
    {
        body ??= new Dictionary<string, JsonElement>();

        FieldUpdate<decimal?> hourlyRate;
        FieldUpdate<bool> isActive;
        try
        {
            hourlyRate = ReadHourlyRate(body);
            isActive = ReadBool(body, "is_active");
        }
        catch (WorkerValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }

        try
        {
            var worker = await updateWorkerHandler.HandleAsync(
                new UpdateWorkerCommand(
                    workspaceContext.WorkspaceId,
                    workerId,
                    ReadString(body, "name"),
                    hourlyRate,
                    isActive),
                ct);

            if (worker is null)
                return NotFound(new ApiErrorResponse(ApiError.WorkerNotFound()));

            return Ok(ToResponse(worker));
        }
        catch (WorkerValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (WorkerConflictException ex)
        {
            return Conflict(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    private static FieldUpdate<string> ReadString(Dictionary<string, JsonElement> body, string key)
        => body.TryGetValue(key, out var el)
            ? FieldUpdate<string>.Set(el.ValueKind == JsonValueKind.Null ? null : el.GetString())
            : FieldUpdate<string>.Absent;

    private static FieldUpdate<decimal?> ReadHourlyRate(Dictionary<string, JsonElement> body)
    {
        if (!body.TryGetValue("hourly_rate", out var el)) return FieldUpdate<decimal?>.Absent;
        if (el.ValueKind == JsonValueKind.Null) return FieldUpdate<decimal?>.Set(null);
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var value))
            return FieldUpdate<decimal?>.Set(value);

        throw new WorkerValidationException(
            ErrorCodes.ValidationRangeHourlyRate, "La tarifa horaria debe ser un número válido.");
    }

    private static FieldUpdate<bool> ReadBool(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<bool>.Absent;
        if (el.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return FieldUpdate<bool>.Set(el.GetBoolean());

        throw new WorkerValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser booleano.");
    }

    private static object ToResponse(WorkerSummary worker) => new
    {
        id = worker.Id,
        workspace_id = worker.WorkspaceId,
        name = worker.Name,
        hourly_rate = worker.HourlyRate,
        is_active = worker.IsActive
    };
}

/// <summary>Alta de trabajador. Solo <c>name</c> es obligatorio (CA-2); <c>hourly_rate</c> es de referencia.</summary>
public sealed record CreateWorkerRequest(
    [Required(ErrorMessage = "El nombre del trabajador es obligatorio.")]
    [StringLength(Worker.NameMaxLength, ErrorMessage = "El nombre del trabajador es demasiado largo.")]
    string Name,
    [property: JsonPropertyName("hourly_rate")] decimal? HourlyRate);
