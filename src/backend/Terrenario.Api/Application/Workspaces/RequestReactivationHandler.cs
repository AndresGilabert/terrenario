using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Email;
using Terrenario.Api.Infrastructure.Tokens;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-206 (HU-5, CA-7/CA-10) — Un miembro usa su enlace de un solo uso para pedir que le traspasen
/// el Workspace dado de baja y se reactive. Aquí solo se <b>solicita</b>: la reactivación real la
/// autoriza quien dio de baja (<see cref="ResolveReactivationHandler"/>), que recibe un aviso por
/// correo para enterarse de que tiene una decisión pendiente.
/// </summary>
public sealed class RequestReactivationHandler(
    IWorkspaceReactivationRequestRepository reactivationRepository,
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IOneTimeTokenService tokenService,
    IWorkspaceLifecycleEmailSender emailSender,
    IOptions<WorkspaceLifecycleOptions> options,
    ILogger<RequestReactivationHandler> logger)
{
    private readonly WorkspaceLifecycleOptions _options = options.Value;

    public async Task<ReactivationPreview> HandleAsync(
        string token,
        Guid actingUserId,
        CancellationToken ct = default)
    {
        var request = await reactivationRepository.FindByTokenHashAsync(tokenService.Hash(token), ct);

        // Un enlace inexistente o dirigido a otra persona se oculta igual: no revela ni que existe.
        if (request is null || request.RecipientUserId != actingUserId)
            throw new WorkspaceMemberException(
                ErrorCodes.ReactivationRequestNotFound,
                "Este enlace de reactivación no existe o ya no es válido.");

        var workspace = await workspaceRepository.FindIncludingDeletedAsync(request.WorkspaceId, ct)
            ?? throw new WorkspaceMemberException(
                ErrorCodes.WorkspaceNotFound,
                "El Workspace de este enlace ya no está disponible.");

        if (!workspace.IsDeleted)
            throw new WorkspaceMemberException(
                ErrorCodes.BusinessRuleWorkspaceNotDeleted,
                "Este Workspace ya está activo: no hace falta solicitar su reactivación.");

        // Consume el enlace (un solo uso, CA-10) y deja la solicitud a la espera de autorización.
        request.Submit(actingUserId, DateTimeOffset.UtcNow);
        await reactivationRepository.SaveChangesAsync(ct);

        var authorizer = await userRepository.FindByIdAsync(request.AuthorizerUserId, ct);
        await TryNotifyAuthorizerAsync(request, workspace, authorizer, actingUserId, ct);

        return new ReactivationPreview(
            request.Id,
            workspace.Id,
            workspace.Name,
            // Se mantiene el mismo dato que mostraba el preview: la pantalla no debe perder de vista
            // quién dio de baja el Workspace justo cuando confirma que le pide el traspaso.
            authorizer?.DisplayName,
            request.Status,
            request.ExpiresAt,
            IsExpired: false,
            CanRequest: false);
    }

    /// <summary>
    /// El aviso es la vía por la que quien dio de baja se entera de la solicitud. Si el correo falla
    /// la solicitud sigue viva y visible en su bandeja: no se pierde, solo tarda más en verse.
    /// </summary>
    private async Task TryNotifyAuthorizerAsync(
        WorkspaceReactivationRequest request,
        Workspace workspace,
        User? authorizer,
        Guid requesterUserId,
        CancellationToken ct)
    {
        if (!emailSender.IsEnabled)
        {
            logger.LogWarning(
                "Sin cuenta de envío configurada: la solicitud de reactivación {RequestId} no se avisa por correo.",
                request.Id);
            return;
        }

        try
        {
            var requester = await userRepository.FindByIdAsync(requesterUserId, ct);
            if (authorizer is null || requester is null) return;

            await emailSender.SendReactivationRequestedAsync(
                new ReactivationRequestedEmail(
                    authorizer.Email,
                    workspace.Name,
                    requester.DisplayName,
                    _options.BuildAuthorizationsUrl()),
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "No se pudo avisar de la solicitud de reactivación {RequestId}.",
                request.Id);
        }
    }
}
