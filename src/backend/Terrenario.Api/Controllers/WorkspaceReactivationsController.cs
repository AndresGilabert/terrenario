using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-206 (HU-5/HU-6, CA-6/CA-7/CA-10) — Vuelta de un Workspace dado de baja.
///
/// A diferencia del resto de operaciones de Workspace, estas rutas **no** llevan
/// <c>[RequireWorkspaceScope]</c>: el Workspace en cuestión está dado de baja —no resuelve contexto
/// activo (CA-8)— y puede ser el único que tuvieran las personas implicadas. La autorización es por
/// titularidad: el enlace solo sirve a su destinatario y la decisión solo la toma quien dio de baja.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/workspaces/reactivations")]
public sealed class WorkspaceReactivationsController(
    PreviewReactivationHandler previewReactivationHandler,
    RequestReactivationHandler requestReactivationHandler,
    ListReactivationRequestsHandler listReactivationRequestsHandler,
    ResolveReactivationHandler resolveReactivationHandler,
    ReopenWorkspaceHandler reopenWorkspaceHandler) : ControllerBase
{
    /// <summary>
    /// Workspaces que la cuenta autenticada dio de baja y puede volver a levantar por su cuenta. Es
    /// la cara reversible de la baja lógica (CA-2) y la única vía cuando el Workspace no tenía más
    /// miembros a los que notificar.
    /// </summary>
    [HttpGet("closed")]
    public async Task<IActionResult> ListClosed(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        var workspaces = await reopenWorkspaceHandler.ListClosedAsync(userId.Value, ct);

        return Ok(new
        {
            data = workspaces.Select(w => new { id = w.Id, name = w.Name, closed_at = w.DeletedAt }),
            meta = new { total = workspaces.Count }
        });
    }

    /// <summary>Vuelve a levantar un Workspace que dio de baja la propia cuenta.</summary>
    [HttpPost("closed/{workspaceId:guid}/reopen")]
    public async Task<IActionResult> Reopen(Guid workspaceId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        try
        {
            var workspace = await reopenWorkspaceHandler.HandleAsync(workspaceId, userId.Value, ct);
            return Ok(new { id = workspace.Id, name = workspace.Name });
        }
        catch (WorkspaceMemberException ex)
        {
            return WorkspaceMemberErrorMapper.ToActionResult(ex);
        }
    }

    /// <summary>
    /// Solicitudes que esperan la decisión de la cuenta autenticada (HU-6): las de los Workspaces
    /// que ella misma dio de baja.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListPendingAuthorizations(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        var requests = await listReactivationRequestsHandler.HandleAsync(userId.Value, ct);

        return Ok(new
        {
            data = requests.Select(r => new
            {
                id = r.Id,
                workspace = new { id = r.WorkspaceId, name = r.WorkspaceName },
                requested_by = new { user_id = r.RequesterUserId, name = r.RequesterDisplayName, email = r.RequesterEmail },
                requested_at = r.RequestedAt,
                expires_at = r.ExpiresAt
            }),
            meta = new { total = requests.Count }
        });
    }

    /// <summary>Lectura del enlace sin consumirlo (HU-5): informa antes de pulsar.</summary>
    [HttpGet("{token}")]
    public async Task<IActionResult> Preview(string token, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        try
        {
            var preview = await previewReactivationHandler.HandleAsync(token, userId.Value, ct);
            return Ok(PreviewPayload(preview));
        }
        catch (WorkspaceMemberException ex)
        {
            return WorkspaceMemberErrorMapper.ToActionResult(ex);
        }
    }

    /// <summary>
    /// Consume el enlace (un solo uso, CA-10) y deja la solicitud a la espera de que la autorice
    /// quien dio de baja el Workspace.
    /// </summary>
    [HttpPost("{token}/request")]
    public async Task<IActionResult> SubmitRequest(string token, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        try
        {
            var result = await requestReactivationHandler.HandleAsync(token, userId.Value, ct);
            return Ok(PreviewPayload(result));
        }
        catch (WorkspaceMemberException ex)
        {
            return WorkspaceMemberErrorMapper.ToActionResult(ex);
        }
    }

    /// <summary>Autoriza: el Workspace vuelve y la propiedad pasa al solicitante (CA-7).</summary>
    [HttpPost("{requestId:guid}/authorize")]
    public async Task<IActionResult> Authorize(Guid requestId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        try
        {
            var outcome = await resolveReactivationHandler.AuthorizeAsync(requestId, userId.Value, ct);

            return Ok(new
            {
                workspace = new { id = outcome.WorkspaceId, name = outcome.WorkspaceName },
                new_owner_user_id = outcome.NewOwnerUserId
            });
        }
        catch (WorkspaceMemberException ex)
        {
            return WorkspaceMemberErrorMapper.ToActionResult(ex);
        }
    }

    /// <summary>Deniega: el Workspace sigue dado de baja (HU-6).</summary>
    [HttpPost("{requestId:guid}/deny")]
    public async Task<IActionResult> Deny(Guid requestId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        try
        {
            await resolveReactivationHandler.DenyAsync(requestId, userId.Value, ct);
            return NoContent();
        }
        catch (WorkspaceMemberException ex)
        {
            return WorkspaceMemberErrorMapper.ToActionResult(ex);
        }
    }

    private static object PreviewPayload(Application.Workspaces.Commands.ReactivationPreview preview) => new
    {
        id = preview.RequestId,
        workspace = new { id = preview.WorkspaceId, name = preview.WorkspaceName },
        closed_by = preview.ClosedByDisplayName,
        status = preview.Status,
        expires_at = preview.ExpiresAt,
        is_expired = preview.IsExpired,
        can_request = preview.CanRequest
    };
}
