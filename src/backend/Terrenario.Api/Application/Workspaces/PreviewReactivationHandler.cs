using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Tokens;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-206 (HU-5) — Lectura del enlace de reactivación antes de usarlo. Igual que el preview de
/// invitación (MVP-107): informa de la aptitud (<c>can_request</c>) para no dejar al usuario pulsar
/// a ciegas, sin consumir el enlace. Un enlace dirigido a otra persona se trata como inexistente.
/// </summary>
public sealed class PreviewReactivationHandler(
    IWorkspaceReactivationRequestRepository reactivationRepository,
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IOneTimeTokenService tokenService)
{
    public async Task<ReactivationPreview> HandleAsync(
        string token,
        Guid actingUserId,
        CancellationToken ct = default)
    {
        var request = await reactivationRepository.FindByTokenHashAsync(tokenService.Hash(token), ct);

        if (request is null || request.RecipientUserId != actingUserId)
            throw new WorkspaceMemberException(
                ErrorCodes.ReactivationRequestNotFound,
                "Este enlace de reactivación no existe o ya no es válido.");

        var workspace = await workspaceRepository.FindIncludingDeletedAsync(request.WorkspaceId, ct)
            ?? throw new WorkspaceMemberException(
                ErrorCodes.WorkspaceNotFound,
                "El Workspace de este enlace ya no está disponible.");

        var closedBy = workspace.DeletedByUserId is { } deletedBy
            ? await userRepository.FindByIdAsync(deletedBy, ct)
            : null;

        var isExpired = request.IsExpiredAt(DateTimeOffset.UtcNow);

        return new ReactivationPreview(
            request.Id,
            workspace.Id,
            workspace.Name,
            closedBy?.DisplayName,
            request.Status,
            request.ExpiresAt,
            isExpired,
            CanRequest: workspace.IsDeleted
                && request.Status == ReactivationRequestStatuses.Pending
                && !isExpired);
    }
}
