using Terrenario.Api.Application.Workspaces.Commands;

namespace Terrenario.Api.Application.Workspaces;

public interface IActiveWorkspaceResolver
{
    /// <summary>
    /// Resuelve el Workspace activo del usuario. Si <paramref name="preferredWorkspaceId"/> viene
    /// informado (claim de la sesión) se valida que siga habiendo membresía activa; en caso
    /// contrario se cae al Workspace por defecto. Devuelve <c>null</c> si el usuario no tiene ninguno.
    /// </summary>
    Task<WorkspaceSummary?> ResolveAsync(Guid userId, Guid? preferredWorkspaceId = null, CancellationToken ct = default);
}
