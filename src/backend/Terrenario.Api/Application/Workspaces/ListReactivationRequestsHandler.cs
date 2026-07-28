using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces;

/// <summary>
/// MVP-206 (HU-6) — Bandeja de decisiones de quien dio de baja Workspaces: solicitudes de traspaso
/// y reactivación que esperan su autorización. No exige Workspace activo, precisamente porque el
/// Workspace en cuestión está dado de baja y puede que sea el único que tuviera.
/// </summary>
public sealed class ListReactivationRequestsHandler(
    IWorkspaceReactivationRequestRepository reactivationRepository)
{
    public Task<IReadOnlyList<ReactivationRequestDetail>> HandleAsync(
        Guid actingUserId,
        CancellationToken ct = default)
        => reactivationRepository.ListPendingAuthorizationsAsync(actingUserId, ct);
}
