using Terrenario.Api.Application.Workspaces.Commands;

namespace Terrenario.Api.Application.Workspaces;

public interface IActiveWorkspaceResolver
{
    /// <summary>
    /// Resuelve el Workspace activo del usuario por orden de preferencia: el
    /// <paramref name="preferredWorkspaceId"/> del claim de la sesión, el último Workspace
    /// que el usuario dejó activo (MVP-104) y, por último, el Workspace por defecto. Cada
    /// candidato se valida contra la membresía activa, así que una membresía revocada nunca
    /// resuelve contexto. Devuelve <c>null</c> si el usuario no tiene ningún Workspace.
    /// </summary>
    Task<WorkspaceSummary?> ResolveAsync(Guid userId, Guid? preferredWorkspaceId = null, CancellationToken ct = default);
}
