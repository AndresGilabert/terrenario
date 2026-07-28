using Terrenario.Api.Application.Workspaces.Commands;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Common.Workspaces;

/// <summary>
/// Implementación por petición de <see cref="IWorkspaceContext"/>. Vive como servicio scoped: el
/// filtro de scope lo rellena con <see cref="Set"/> y el resto de la petición lo lee.
/// </summary>
public sealed class WorkspaceScopeContext : IWorkspaceContext
{
    private WorkspaceSummary? _workspace;

    public bool HasWorkspace => _workspace is not null;

    public WorkspaceSummary Workspace => _workspace
        ?? throw new InvalidOperationException(
            "No hay Workspace activo en el contexto: marca la acción con [RequireWorkspaceScope].");

    public Guid WorkspaceId => Workspace.Id;

    /// <summary>
    /// Lo invoca el filtro de scope una vez resuelto el Workspace activo de la sesión. No se expone
    /// en <see cref="IWorkspaceContext"/>: quien consume el contexto solo lee, nunca lo fija.
    /// </summary>
    public void Set(WorkspaceSummary workspace) => _workspace = workspace;

    public void EnsureInScope(Guid resourceWorkspaceId)
    {
        if (resourceWorkspaceId != WorkspaceId)
            throw new WorkspaceAccessDeniedException("El recurso no pertenece a tu Workspace activo.");
    }
}
