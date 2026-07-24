using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workspaces")]
public sealed class WorkspacesController(
    CreateWorkspaceHandler createWorkspaceHandler,
    ListUserWorkspacesHandler listUserWorkspacesHandler,
    SwitchActiveWorkspaceHandler switchActiveWorkspaceHandler,
    IActiveWorkspaceResolver activeWorkspaceResolver) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkspaceRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        try
        {
            var result = await createWorkspaceHandler.HandleAsync(
                new CreateWorkspaceCommand(userId.Value, User.GetDisplayName(), request.Name),
                ct);

            return CreatedAtAction(
                nameof(GetActive),
                new
                {
                    workspace = new { id = result.Workspace.Id, name = result.Workspace.Name },
                    access_token = result.AccessToken,
                    expires_in = result.ExpiresIn
                });
        }
        catch (WorkspaceValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    /// <summary>MVP-104 — Workspaces disponibles del usuario y cuál está activo (HU-1, CA-1).</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        var result = await listUserWorkspacesHandler.HandleAsync(
            new ListUserWorkspacesQuery(userId.Value, User.GetWorkspaceId()),
            ct);

        return Ok(new
        {
            data = result.Workspaces.Select(w => new
            {
                id = w.Id,
                name = w.Name,
                role = w.Role,
                status = w.Status,
                is_active = w.Id == result.ActiveWorkspaceId,
                joined_at = w.JoinedAt
            }),
            meta = new
            {
                total = result.Workspaces.Count,
                active_workspace_id = result.ActiveWorkspaceId
            }
        });
    }

    /// <summary>MVP-104 — Cambia el Workspace activo y reemite la sesión situada en él (HU-2, CA-2, CA-3).</summary>
    [HttpPut("active")]
    public async Task<IActionResult> SetActive([FromBody] SetActiveWorkspaceRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        try
        {
            var result = await switchActiveWorkspaceHandler.HandleAsync(
                new SwitchActiveWorkspaceCommand(userId.Value, User.GetDisplayName(), request.WorkspaceId!.Value),
                ct);

            return Ok(new
            {
                workspace = new { id = result.Workspace.Id, name = result.Workspace.Name },
                access_token = result.AccessToken,
                expires_in = result.ExpiresIn
            });
        }
        catch (WorkspaceAccessDeniedException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ApiErrorResponse(ApiError.Validation(ErrorCodes.AuthWorkspaceForbidden, ex.Message)));
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        var workspace = await activeWorkspaceResolver.ResolveAsync(userId.Value, User.GetWorkspaceId(), ct);

        if (workspace is null)
            return NotFound(new ApiErrorResponse(ApiError.WorkspaceNotFound()));

        return Ok(new { id = workspace.Id, name = workspace.Name });
    }
}

public sealed record CreateWorkspaceRequest(
    [Required(ErrorMessage = "El nombre del Workspace es obligatorio.")]
    [StringLength(Workspace.NameMaxLength, ErrorMessage = "El nombre del Workspace es demasiado largo.")]
    string Name);

public sealed record SetActiveWorkspaceRequest(
    [Required(ErrorMessage = "Indica el Workspace que quieres activar.")]
    [property: JsonPropertyName("workspace_id")]
    Guid? WorkspaceId);
