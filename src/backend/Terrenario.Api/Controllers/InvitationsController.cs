using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Terrenario.Api.Application.Invitations;
using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-103 — Lado de quien recibe la invitación. Exige sesión iniciada: el enlace compartible
/// no abre ninguna vía de acceso fuera del flujo autenticado del MVP.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/invitations")]
public sealed class InvitationsController(
    PreviewInvitationHandler previewInvitationHandler,
    AcceptInvitationHandler acceptInvitationHandler,
    RejectInvitationHandler rejectInvitationHandler,
    ListReceivedInvitationsHandler listReceivedInvitationsHandler) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<IActionResult> Preview(string token, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        try
        {
            var invitation = await previewInvitationHandler.HandleAsync(token, userId.Value, ct);

            return Ok(new
            {
                id = invitation.Id,
                channel = invitation.Channel,
                status = invitation.Status,
                workspace = new { id = invitation.Workspace.Id, name = invitation.Workspace.Name },
                invited_by = invitation.InvitedByDisplayName,
                expires_at = invitation.ExpiresAt,
                is_expired = invitation.IsExpired,
                // Aptitud de la cuenta autenticada (MVP-107, R-C): anticipa el resultado de aceptar.
                viewer = new { can_accept = invitation.ViewerCanAccept, reason = invitation.ViewerReason }
            });
        }
        catch (InvitationException ex)
        {
            return InvitationErrorMapper.ToActionResult(ex);
        }
    }

    [HttpPost("{token}/accept")]
    public async Task<IActionResult> Accept(string token, CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        try
        {
            var result = await acceptInvitationHandler.HandleAsync(
                new AcceptInvitationCommand(userId.Value, token),
                ct);

            return Ok(AcceptPayload(result));
        }
        catch (InvitationException ex)
        {
            return InvitationErrorMapper.ToActionResult(ex);
        }
    }

    [HttpPost("{token}/reject")]
    public async Task<IActionResult> Reject(string token, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        try
        {
            await rejectInvitationHandler.HandleByTokenAsync(userId.Value, token, ct);
            return NoContent();
        }
        catch (InvitationException ex)
        {
            return InvitationErrorMapper.ToActionResult(ex);
        }
    }

    /// <summary>
    /// MVP-107 — Bandeja de invitaciones recibidas por la cuenta autenticada (HU-3). No exige
    /// Workspace activo: es la vía por la que un usuario sin ninguno descubre y acepta el primero.
    /// </summary>
    [HttpGet("received")]
    public async Task<IActionResult> ListReceived(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        var invitations = await listReceivedInvitationsHandler.HandleAsync(userId.Value, ct);

        return Ok(new
        {
            data = invitations.Select(invitation => new
            {
                id = invitation.Id,
                channel = invitation.Channel,
                workspace = new { id = invitation.Workspace.Id, name = invitation.Workspace.Name },
                invited_by = invitation.InvitedByDisplayName,
                expires_at = invitation.ExpiresAt,
                created_at = invitation.CreatedAt
            }),
            meta = new { total = invitations.Count }
        });
    }

    [HttpPost("received/{id:guid}/accept")]
    public async Task<IActionResult> AcceptReceived(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        try
        {
            var result = await acceptInvitationHandler.HandleByIdAsync(userId.Value, id, ct);
            return Ok(AcceptPayload(result));
        }
        catch (InvitationException ex)
        {
            return InvitationErrorMapper.ToActionResult(ex);
        }
    }

    [HttpPost("received/{id:guid}/reject")]
    public async Task<IActionResult> RejectReceived(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        try
        {
            await rejectInvitationHandler.HandleByIdAsync(userId.Value, id, ct);
            return NoContent();
        }
        catch (InvitationException ex)
        {
            return InvitationErrorMapper.ToActionResult(ex);
        }
    }

    private static object AcceptPayload(Application.Invitations.Commands.AcceptInvitationResult result) => new
    {
        workspace = new { id = result.Workspace.Id, name = result.Workspace.Name },
        access_token = result.AccessToken,
        expires_in = result.ExpiresIn,
        already_member = result.AlreadyMember
    };
}
