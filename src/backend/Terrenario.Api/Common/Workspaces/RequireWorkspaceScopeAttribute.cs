using Microsoft.AspNetCore.Mvc.Filters;

namespace Terrenario.Api.Common.Workspaces;

/// <summary>
/// Exige que la sesión esté situada en un Workspace activo para ejecutar la acción o el controller.
/// Delega en <see cref="WorkspaceScopeFilter"/>, que resuelve el contexto y lo publica en
/// <see cref="IWorkspaceContext"/>. Cualquier operación de negocio Workspace-first se marca con este
/// atributo en lugar de repetir el chequeo de scope (MVP-105, CA-1).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireWorkspaceScopeAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        => ActivatorUtilities.GetServiceOrCreateInstance<WorkspaceScopeFilter>(serviceProvider);
}
