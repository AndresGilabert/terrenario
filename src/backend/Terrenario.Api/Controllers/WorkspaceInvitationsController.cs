using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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
    ResendInvitationHandler resendInvitationHandler,
    CancelInvitationHandler cancelInvitationHandler,
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

    /// <summary>
    /// MVP-204 (HU-5/CA-6) — Reemite una invitación pendiente. Rota el token (un solo uso) y renueva
    /// la caducidad, igual que la emisión original. <c>deliver_email: false</c> hace el reenvío "por
    /// enlace": no reenvía el correo y solo devuelve el nuevo <c>accept_url</c> para compartirlo por
    /// otro medio. Desde MVP-208 (CA-7) cubre también el canal <c>enlace</c>, que no tiene
    /// destinatario: allí <c>email_sent</c> es siempre <c>false</c>.
    /// </summary>
    [HttpPost("{invitationId:guid}/resend")]
    public async Task<IActionResult> Resend(
        Guid invitationId,
        [FromBody] ResendInvitationRequest? request,
        CancellationToken ct)
    {
        var workspace = workspaceContext.Workspace;

        try
        {
            var result = await resendInvitationHandler.HandleAsync(
                new ResendInvitationCommand(
                    workspace.Id,
                    workspace.Name,
                    User.GetUserId()!.Value,
                    User.GetDisplayName(),
                    invitationId,
                    request?.DeliverEmail ?? true),
                ct);

            return Ok(new
            {
                id = result.Id,
                channel = result.Channel,
                email = result.Email,
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

    /// <summary>
    /// MVP-207 (HU-2/CA-4) — Anula una invitación pendiente del Workspace activo: su enlace deja de
    /// permitir la aceptación y la persona desaparece de la lista de personas. Es la contrapartida de
    /// «retirar acceso» para quien todavía no ha entrado, y del rechazo de MVP-107 (que ejecuta la
    /// persona invitada, no el Workspace emisor). Cualquier miembro puede hacerlo (RN-034).
    /// </summary>
    [HttpPost("{invitationId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid invitationId, CancellationToken ct)
    {
        try
        {
            await cancelInvitationHandler.HandleAsync(
                new CancelInvitationCommand(
                    workspaceContext.WorkspaceId,
                    User.GetUserId()!.Value,
                    invitationId),
                ct);

            return NoContent();
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

/// <summary>
/// Reenvío de invitación (MVP-204). <c>deliver_email</c> distingue reenviar por email (por defecto)
/// de reenviar por enlace (solo devuelve el nuevo <c>accept_url</c>).
/// </summary>
public sealed record ResendInvitationRequest(
    [property: JsonPropertyName("deliver_email")] bool DeliverEmail = true);
