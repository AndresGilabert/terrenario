using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Email;
using Terrenario.Api.Infrastructure.Tokens;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-206 (HU-2/HU-4, CA-2/CA-5/CA-6) — Baja del Workspace por su propietario. Implementa el árbol
/// de decisión del spec:
/// <list type="number">
///   <item><b>Hay otros propietarios activos</b> → el Workspace se reasigna al copropietario más
///   antiguo y <b>sigue vivo</b>; quien lo pidió cede la propiedad y sale (su membresía pasa a
///   <c>revocado</c>), que es lo que espera al pedir dejar de verlo (CA-5).</item>
///   <item><b>Propietario único</b> → <b>baja lógica</b> (<c>deleted_at</c>, nunca borrado físico) y
///   se avisa por email al resto de miembros activos con un enlace de un solo uso para solicitar el
///   traspaso y la reactivación (CA-2/CA-6). Sin más miembros no hay a quién avisar.</item>
/// </list>
/// La invariante rectora es que el Workspace <b>nunca queda sin propietario</b>: o lo hereda alguien
/// o queda dado de baja con quien lo dio de baja como única persona capaz de devolverlo (CA-10).
/// </summary>
public sealed class CloseWorkspaceHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceReactivationRequestRepository reactivationRepository,
    IOneTimeTokenService tokenService,
    IWorkspaceLifecycleEmailSender emailSender,
    IOptions<WorkspaceLifecycleOptions> options,
    ILogger<CloseWorkspaceHandler> logger)
{
    private readonly WorkspaceLifecycleOptions _options = options.Value;

    public async Task<WorkspaceClosureResult> HandleAsync(
        CloseWorkspaceCommand command,
        CancellationToken ct = default)
    {
        var workspace = await workspaceRepository.FindByIdAsync(command.WorkspaceId, ct)
            ?? throw new WorkspaceMemberException(
                ErrorCodes.WorkspaceNotFound,
                "El Workspace ya no está disponible.");

        var actingMember = await EnsureOwnerAsync(workspace.Id, command.ActingUserId, ct);

        var successor = await workspaceRepository.FindOtherActiveOwnerAsync(
            workspace.Id, command.ActingUserId, ct);

        return successor is null
            ? await SoftDeleteAsync(workspace, command, ct)
            : await ReassignAsync(workspace, actingMember, successor, ct);
    }

    /// <summary>
    /// CA-5 — Con copropietarios el Workspace no se da de baja: cambia de manos y quien lo pidió
    /// sale. Ceder la propiedad y quedarse dentro es otra acción distinta (el traspaso explícito).
    /// </summary>
    private async Task<WorkspaceClosureResult> ReassignAsync(
        Workspace workspace,
        WorkspaceMember actingMember,
        WorkspaceMember successor,
        CancellationToken ct)
    {
        workspace.TransferOwnershipTo(successor.UserId);
        successor.PromoteToOwner();
        actingMember.DemoteToMember();
        actingMember.Revoke();

        await workspaceRepository.SaveChangesAsync(ct);

        var successorName = (await workspaceRepository.ListMembersAsync(workspace.Id, ct))
            .FirstOrDefault(m => m.UserId == successor.UserId)?.DisplayName;

        return new WorkspaceClosureResult(
            WorkspaceClosureOutcomes.Transferred,
            workspace.Id,
            workspace.Name,
            successorName,
            NotifiedMembers: 0,
            EmailsSent: 0);
    }

    /// <summary>
    /// CA-2/CA-6 — Baja lógica y aviso a los demás miembros activos. Cada uno recibe su propio
    /// enlace de un solo uso: así el traspaso queda atado a quien lo pide y no a un enlace común.
    /// </summary>
    private async Task<WorkspaceClosureResult> SoftDeleteAsync(
        Workspace workspace,
        CloseWorkspaceCommand command,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        workspace.SoftDelete(command.ActingUserId, now);

        var recipients = (await workspaceRepository.ListMembersAsync(workspace.Id, ct))
            .Where(m => m.Status == WorkspaceMemberStatuses.Active && m.UserId != command.ActingUserId)
            .ToList();

        var links = new List<(WorkspaceMemberDetail Recipient, string Url)>();
        var requests = new List<WorkspaceReactivationRequest>();

        foreach (var recipient in recipients)
        {
            var token = tokenService.Generate();
            requests.Add(WorkspaceReactivationRequest.Issue(
                workspace.Id,
                recipient.UserId,
                command.ActingUserId,
                token.Hash,
                _options.ReactivationLifetime));

            links.Add((recipient, _options.BuildReactivationUrl(token.Value)));
        }

        await reactivationRepository.AddRangeAsync(requests, ct);

        // Baja, solicitudes y roles comparten el DbContext de la petición: una sola transacción.
        await workspaceRepository.SaveChangesAsync(ct);

        var emailsSent = 0;
        foreach (var (recipient, url) in links)
        {
            if (await TrySendClosedEmailAsync(workspace, command, recipient, url, ct)) emailsSent++;
        }

        return new WorkspaceClosureResult(
            WorkspaceClosureOutcomes.Deleted,
            workspace.Id,
            workspace.Name,
            NewOwnerDisplayName: null,
            recipients.Count,
            emailsSent);
    }

    /// <summary>
    /// Ni la falta de cuenta de envío ni un fallo del proveedor invalidan la baja: ya está hecha y
    /// la solicitud de reactivación sigue viva para quien reciba el enlace por otra vía.
    /// </summary>
    private async Task<bool> TrySendClosedEmailAsync(
        Workspace workspace,
        CloseWorkspaceCommand command,
        WorkspaceMemberDetail recipient,
        string reactivationUrl,
        CancellationToken ct)
    {
        if (!emailSender.IsEnabled)
        {
            logger.LogWarning(
                "Sin cuenta de envío configurada: el aviso de baja del Workspace {WorkspaceId} no sale por correo.",
                workspace.Id);
            return false;
        }

        try
        {
            await emailSender.SendWorkspaceClosedAsync(
                new WorkspaceClosedEmail(
                    recipient.Email,
                    command.WorkspaceName,
                    command.ActingDisplayName,
                    reactivationUrl),
                ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "No se pudo avisar de la baja del Workspace {WorkspaceId} a un miembro.",
                workspace.Id);
            return false;
        }
    }

    private async Task<WorkspaceMember> EnsureOwnerAsync(Guid workspaceId, Guid userId, CancellationToken ct)
    {
        var member = await workspaceRepository.FindActiveMemberAsync(workspaceId, userId, ct)
            ?? throw new WorkspaceMemberException(
                ErrorCodes.ResourceNotFound,
                "No eres miembro activo de este Workspace.");

        // La baja y el traspaso afectan a la propiedad: se restringen a workspace_owner aunque el
        // resto de permisos del MVP sean planos (RN-034).
        if (member.Role != WorkspaceRoles.Owner)
            throw new WorkspaceMemberException(
                ErrorCodes.AuthWorkspaceOwnerRequired,
                "Solo el propietario del Workspace puede darlo de baja.");

        return member;
    }
}
