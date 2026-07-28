using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-204 (HU-3/HU-4, CA-4/CA-5/CA-7/CA-8) — Personas del Workspace activo y su estado de membresía.
/// Como el resto de recursos con ámbito de Workspace, se apoya en
/// <see cref="RequireWorkspaceScopeAttribute"/> (MVP-105): el Workspace se resuelve en servidor y se
/// lee de <see cref="IWorkspaceContext"/>, nunca del cliente (RN-034). Permisos planos (RN-034):
/// cualquier miembro activo puede ver la lista y revocar a otro.
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/workspace-members")]
public sealed class WorkspaceMembersController(
    ListWorkspacePeopleHandler listWorkspacePeopleHandler,
    RevokeMemberHandler revokeMemberHandler,
    IWorkspaceContext workspaceContext) : ControllerBase
{
    /// <summary>
    /// Lista unificada de personas y accesos pendientes del Workspace con su estado (<c>activo</c>,
    /// <c>invitado</c>, <c>revocado</c>). El orden agrupa: primero activos, luego invitados y por
    /// último revocados.
    ///
    /// MVP-208 — Los responsables seleccionables ya no salen de aquí, sino de <c>GET /workers</c>
    /// (CA-2). Esta sigue siendo la superficie de <b>accesos</b>, y desde CA-7 la única de
    /// invitaciones pendientes: incluye los dos canales, con <c>channel</c> para distinguirlos.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var actingUserId = User.GetUserId();
        var people = await listWorkspacePeopleHandler.HandleAsync(workspaceContext.WorkspaceId, ct);

        var active = people.Members
            .Where(m => m.Status == WorkspaceMemberStatuses.Active)
            .Select(m => MemberResponse(m, actingUserId));
        var invited = people.Invited.Select(InvitedResponse);
        var revoked = people.Members
            .Where(m => m.Status == WorkspaceMemberStatuses.Revoked)
            .Select(m => MemberResponse(m, actingUserId));

        var data = active.Concat(invited).Concat(revoked).ToList();

        return Ok(new
        {
            data,
            meta = new
            {
                total = data.Count,
                active = people.Members.Count(m => m.Status == WorkspaceMemberStatuses.Active),
                invited = people.Invited.Count,
                revoked = people.Members.Count(m => m.Status == WorkspaceMemberStatuses.Revoked)
            }
        });
    }

    /// <summary>
    /// Retira el acceso de un miembro activo (CA-7). Su membresía pasa a <c>revocado</c> sin borrar el
    /// vínculo. CA-8: no se puede revocar al último miembro activo ni al propietario único.
    /// </summary>
    [HttpPost("{userId:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid userId, CancellationToken ct)
    {
        try
        {
            await revokeMemberHandler.HandleAsync(workspaceContext.WorkspaceId, userId, ct);
            return NoContent();
        }
        catch (WorkspaceMemberException ex)
        {
            return WorkspaceMemberErrorMapper.ToActionResult(ex);
        }
    }

    private static object MemberResponse(WorkspaceMemberDetail member, Guid? actingUserId) => new
    {
        kind = "member",
        status = member.Status,
        user_id = member.UserId,
        name = member.DisplayName,
        email = member.Email,
        role = member.Role,
        joined_at = member.JoinedAt,
        is_self = actingUserId == member.UserId,
        can_revoke = member.Status == WorkspaceMemberStatuses.Active
            && member.Role != WorkspaceRoles.Owner
    };

    private static object InvitedResponse(Application.Workspaces.Commands.WorkspaceInvitedDetail invited) => new
    {
        kind = "invitation",
        status = WorkspaceMemberStatuses.Invited,
        invitation_id = invited.InvitationId,
        name = (string?)null,
        email = invited.Email,
        // MVP-208 (CA-7): el canal viaja para que la UI distinga a quién se invitó (email) de un
        // enlace compartible sin destinatario, y ofrezca en cada caso las acciones que aplican.
        channel = invited.Channel,
        invited_at = invited.CreatedAt,
        expires_at = invited.ExpiresAt,
        is_expired = invited.IsExpired
    };
}
