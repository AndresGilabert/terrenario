using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
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
                new CreateWorkspaceCommand(userId.Value, User.GetDisplayName(), request.Nombre),
                ct);

            return CreatedAtAction(
                nameof(GetActive),
                new
                {
                    workspace = new { id = result.Workspace.Id, nombre = result.Workspace.Name },
                    access_token = result.AccessToken,
                    expires_in = result.ExpiresIn
                });
        }
        catch (WorkspaceValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    [HttpGet("activo")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        var workspace = await activeWorkspaceResolver.ResolveAsync(userId.Value, User.GetWorkspaceId(), ct);

        if (workspace is null)
            return NotFound(new ApiErrorResponse(ApiError.WorkspaceNotFound()));

        return Ok(new { id = workspace.Id, nombre = workspace.Name });
    }
}

public sealed record CreateWorkspaceRequest(
    [Required(ErrorMessage = "El nombre del Workspace es obligatorio.")]
    [StringLength(Workspace.NameMaxLength, ErrorMessage = "El nombre del Workspace es demasiado largo.")]
    string Nombre);
