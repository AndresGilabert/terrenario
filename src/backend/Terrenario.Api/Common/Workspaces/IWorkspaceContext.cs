using Terrenario.Api.Application.Workspaces.Commands;

namespace Terrenario.Api.Common.Workspaces;

/// <summary>
/// Contexto de Workspace activo de la petición en curso. Lo puebla el filtro
/// <see cref="RequireWorkspaceScopeAttribute"/> antes de ejecutar la acción; controllers y
/// handlers lo consumen para acotar toda operación al Workspace en el que está situada la sesión
/// (MVP-105, CA-1). El Workspace activo nunca viaja como parámetro de negocio: se resuelve en
/// servidor desde el claim de la sesión (RN-034).
/// </summary>
public interface IWorkspaceContext
{
    /// <summary>Workspace activo resuelto para la petición.</summary>
    WorkspaceSummary Workspace { get; }

    /// <summary>Id del Workspace activo. Atajo de <see cref="Workspace"/>.Id.</summary>
    Guid WorkspaceId { get; }

    /// <summary><c>true</c> cuando el filtro de scope ya resolvió un Workspace activo.</summary>
    bool HasWorkspace { get; }

    /// <summary>
    /// Rechaza el acceso a un recurso que no pertenece al Workspace activo lanzando
    /// <see cref="Terrenario.Api.Domain.Workspaces.WorkspaceAccessDeniedException"/>, que el borde
    /// de transporte traduce a <c>403 AUTH_WORKSPACE_FORBIDDEN</c>. Es el punto único que usarán las
    /// operaciones de negocio de las épicas siguientes para no cruzar datos entre explotaciones.
    /// </summary>
    void EnsureInScope(Guid resourceWorkspaceId);
}
