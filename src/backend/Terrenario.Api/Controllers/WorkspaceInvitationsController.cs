using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Terrenario.Api.Application.Invitations;
using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-103 — Invitaciones del Workspace activo. Cualquier miembro puede invitar: en MVP los
/// permisos son planos (RN-034). El Workspace de origen no viaja en la petición: lo resuelve
/// <see cref="RequireWorkspaceScopeAttribute"/> desde la sesión y lo publica en
/// <see cref="IWorkspaceContext"/> (MVP-105).
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/workspaces/invitations")]
public sealed class WorkspaceInvitationsController(
    CreateInvitationHandler createInvitationHandler,
    ListWorkspaceInvitationsHandler listWorkspaceInvitationsHandler,
    IWorkspaceContext workspaceContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvitationRequest request, CancellationToken ct)
    {
        var workspace = workspaceContext.Workspace;

        try
        {
            var result = await createInvitationHandler.HandleAsync(
                new CreateInvitationCommand(
                    workspace.Id,
                    workspace.Name,
                    User.GetUserId()!.Value,
                    User.GetDisplayName(),
                    request.Channel,
                    request.Email),
                ct);

            return Created(string.Empty, new
            {
                id = result.Id,
                channel = result.Channel,
                email = result.Email,
                status = result.Status,
                accept_url = result.AcceptUrl,
                expires_at = result.ExpiresAt,
                email_sent = result.EmailSent
            });
        }
        catch (InvitationException ex)
        {
            return InvitationErrorMapper.ToActionResult(ex);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ListPending(CancellationToken ct)
    {
        var invitations = await listWorkspaceInvitationsHandler.HandleAsync(workspaceContext.WorkspaceId, ct);

        return Ok(new
        {
            data = invitations.Select(invitation => new
            {
                id = invitation.Id,
                channel = invitation.Channel,
                email = invitation.Email,
                status = invitation.Status,
                expires_at = invitation.ExpiresAt,
                created_at = invitation.CreatedAt
            }),
            meta = new { total = invitations.Count }
        });
    }
}

public sealed record CreateInvitationRequest(
    [Required(ErrorMessage = "El canal de invitación es obligatorio.")]
    string Channel,
    [StringLength(WorkspaceInvitation.EmailMaxLength, ErrorMessage = "El email de la persona invitada es demasiado largo.")]
    string? Email);
