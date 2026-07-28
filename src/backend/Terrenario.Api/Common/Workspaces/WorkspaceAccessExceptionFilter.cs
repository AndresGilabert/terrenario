using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Common.Workspaces;

/// <summary>
/// Traduce <see cref="WorkspaceAccessDeniedException"/> a <c>403 AUTH_WORKSPACE_FORBIDDEN</c> de
/// forma uniforme. Así, cualquier operación que rechace un recurso fuera del Workspace activo —hoy
/// el cambio de Workspace, mañana terrenos, temporadas, etc.— devuelve el mismo contrato sin repetir
/// el try/catch en cada controller (MVP-105, CA-1). El dominio no conoce HTTP: la correspondencia
/// vive aquí, en el borde de transporte.
/// </summary>
public sealed class WorkspaceAccessExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not WorkspaceAccessDeniedException ex)
            return;

        context.Result = new ObjectResult(new ApiErrorResponse(ApiError.WorkspaceForbidden(ex.Message)))
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
        context.ExceptionHandled = true;
    }
}
