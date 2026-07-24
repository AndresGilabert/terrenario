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
    AcceptInvitationHandler acceptInvitationHandler) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<IActionResult> Preview(string token, CancellationToken ct)
    {
        try
        {
            var invitation = await previewInvitationHandler.HandleAsync(token, ct);

            return Ok(new
            {
                id = invitation.Id,
                channel = invitation.Channel,
                status = invitation.Status,
                workspace = new { id = invitation.Workspace.Id, name = invitation.Workspace.Name },
                invited_by = invitation.InvitedByDisplayName,
                expires_at = invitation.ExpiresAt,
                is_expired = invitation.IsExpired
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

            return Ok(new
            {
                workspace = new { id = result.Workspace.Id, name = result.Workspace.Name },
                access_token = result.AccessToken,
                expires_in = result.ExpiresIn,
                already_member = result.AlreadyMember
            });
        }
        catch (InvitationException ex)
        {
            return InvitationErrorMapper.ToActionResult(ex);
        }
    }
}
