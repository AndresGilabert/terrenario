using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Workers;
using Terrenario.Api.Application.Workers.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-204 · MVP-208 — Maestro de responsables del Workspace activo. Como el resto de recursos con
/// ámbito de Workspace, se apoya en <see cref="RequireWorkspaceScopeAttribute"/> (MVP-105): el
/// Workspace activo se resuelve en servidor y se lee de <see cref="IWorkspaceContext"/>, nunca del
/// cliente (RN-034).
///
/// Desde MVP-208 el listado devuelve <b>todas</b> las personas seleccionables como responsables
/// (CA-2): los miembros del Workspace, materializados como filas con <c>user_account_id</c>, y la
/// cuadrilla sin cuenta. El alta crea siempre cuadrilla; un miembro entra en el maestro por su
/// membresía (RN-027), no por este endpoint, y de él solo se edita la tarifa horaria (CA-4).
///
/// <c>GET /workspace-members</c> sigue siendo la superficie de <b>accesos</b> (estado de membresía,
/// invitar, revocar): lo que cambia es que ya no es también la fuente de responsables.
///
/// El borrado físico queda fuera: los trabajadores con histórico se inactivan.
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
    /// <summary>
    /// Maestro completo de responsables del Workspace: miembros y cuadrilla, con la señal
    /// <c>kind</c> que los distingue (MVP-208, CA-2). Filtro opcional: <c>is_active</c>.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery(Name = "is_active")] bool? isActive,
        CancellationToken ct)
    {
        var workers = await listWorkersHandler.HandleAsync(workspaceContext.WorkspaceId, isActive, ct);

        return Ok(new
        {
            data = workers.Select(ToResponse),
            meta = new
            {
                total = workers.Count,
                members = workers.Count(w => w.Kind == WorkerKinds.Member),
                crew = workers.Count(w => w.Kind == WorkerKinds.Crew)
            }
        });
    }

    /// <summary>Alta de trabajador de cuadrilla, sin cuenta. Solo <c>name</c> es obligatorio (CA-2).</summary>
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
    ///
    /// En un responsable con cuenta solo se admite <c>hourly_rate</c>: <c>name</c> e <c>is_active</c>
    /// responden 422 (MVP-208, CA-4).
    /// </summary>
    [HttpPatch("{workerId:guid}")]
    public async Task<IActionResult> Update(
        Guid workerId,
        [FromBody] Dictionary<string, JsonElement>? body,
        CancellationToken ct)
    {
        // Lector común del borde de transporte (MVP-502, P-027).
        var fields = PartialUpdateBody.From(body);

        if (!fields.TryReadDecimal("hourly_rate", out var hourlyRate))
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationRangeHourlyRate, "El precio por hora debe ser un número válido.")));

        if (!fields.TryReadBool("is_active", out var isActive))
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationRequired, "El campo 'is_active' debe ser booleano.")));

        try
        {
            var worker = await updateWorkerHandler.HandleAsync(
                new UpdateWorkerCommand(
                    workspaceContext.WorkspaceId,
                    workerId,
                    fields.ReadString("name"),
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
        catch (WorkerBusinessRuleException ex)
        {
            // 422, como el resto de BUSINESS_RULE_* del contrato: la petición está bien formada, pero
            // el maestro no es quien gobierna ese dato (MVP-208, CA-4).
            return UnprocessableEntity(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    private static object ToResponse(WorkerSummary worker) => new
    {
        id = worker.Id,
        workspace_id = worker.WorkspaceId,
        name = worker.Name,
        hourly_rate = worker.HourlyRate,
        is_active = worker.IsActive,
        // MVP-208 (CA-2): señal del catálogo cerrado `worker_kind` y cuenta vinculada, para que el
        // cliente distinga las dos clases de persona sin consultar otro endpoint.
        kind = worker.Kind,
        user_account_id = worker.UserAccountId
    };
}

/// <summary>Alta de trabajador de cuadrilla. Solo <c>name</c> es obligatorio (CA-2); <c>hourly_rate</c> es de referencia.</summary>
public sealed record CreateWorkerRequest(
    [RequiredField(ErrorCodes.ValidationRequiredName, "El nombre del trabajador es obligatorio.")]
    [MaxTextLength(Worker.NameMaxLength, ErrorCodes.ValidationWorkerNameLength, "El nombre del trabajador es demasiado largo.")]
    string Name,
    [property: JsonPropertyName("hourly_rate")] decimal? HourlyRate);
