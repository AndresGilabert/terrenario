using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Common.Workspaces;

/// <summary>
/// Guardián único de las acciones que exigen contexto de Workspace. Resuelve el Workspace activo de
/// la sesión con el mismo <see cref="IActiveWorkspaceResolver"/> que usa el resto de la API, lo deja
/// en <see cref="WorkspaceScopeContext"/> y deja continuar. Corta antes de la acción con:
/// <list type="bullet">
///   <item><c>401 AUTH_UNAUTHENTICATED</c> si la sesión no identifica usuario.</item>
///   <item><c>403 AUTH_WORKSPACE_SCOPE_REQUIRED</c> si el usuario no tiene ningún Workspace activo.</item>
/// </list>
/// Sustituye a los chequeos manuales repetidos por controller (MVP-105, CA-1).
/// </summary>
public sealed class WorkspaceScopeFilter(
    IActiveWorkspaceResolver activeWorkspaceResolver,
    WorkspaceScopeContext workspaceContext) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userId = context.HttpContext.User.GetUserId();

        if (userId is null)
        {
            context.Result = new UnauthorizedObjectResult(new ApiErrorResponse(ApiError.Unauthenticated()));
            return;
        }

        var workspace = await activeWorkspaceResolver.ResolveAsync(
            userId.Value,
            context.HttpContext.User.GetWorkspaceId(),
            context.HttpContext.RequestAborted);

        if (workspace is null)
        {
            context.Result = new ObjectResult(new ApiErrorResponse(ApiError.WorkspaceScopeRequired()))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        workspaceContext.Set(workspace);
        await next();
    }
}
