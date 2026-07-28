using Terrenario.Api.Application.Invitations.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Invitations;

/// <summary>
/// MVP-207 (HU-2, CA-4) — Anula una invitación pendiente del Workspace activo. Cierra la asimetría
/// que dejó MVP-204: «Miembros y accesos» sabía reenviar y revocar, pero no retirar a una persona en
/// estado <c>invitado</c>, así que una invitación enviada al email equivocado seguía viva y
/// aceptable hasta caducar.
///
/// A diferencia del reenvío, no se limita al canal <c>email</c>: un enlace compartible que se ha ido
/// de las manos es justo el caso en el que hace falta retirarlo. Tras anularla, el enlace deja de
/// permitir la aceptación y la persona desaparece de la lista de personas del Workspace (que solo
/// proyecta invitaciones pendientes).
///
/// Cualquier invitación inexistente, de otro Workspace o que ya no esté pendiente responde 404, como
/// el reenvío: no se revela el estado de invitaciones ajenas.
/// </summary>
public sealed class CancelInvitationHandler(IWorkspaceInvitationRepository invitationRepository)
{
    public async Task HandleAsync(CancelInvitationCommand command, CancellationToken ct = default)
    {
        var invitation = await invitationRepository.FindByIdAsync(command.InvitationId, ct);

        if (invitation is null
            || invitation.WorkspaceId != command.WorkspaceId
            || invitation.Status != InvitationStatuses.Pending)
            throw new InvitationException(
                ErrorCodes.InvitationNotFound,
                "La invitación no existe o ya no está disponible en tu Workspace.");

        invitation.Cancel(command.ActingUserId, DateTimeOffset.UtcNow);
        await invitationRepository.SaveChangesAsync(ct);
    }
}
