using Microsoft.AspNetCore.Mvc;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Common.Errors;

/// <summary>
/// Traduce los errores de dominio de membresía y ciclo de vida del Workspace (MVP-204/MVP-206) a los
/// códigos HTTP de <c>docs/02-arquitectura/contratos-api.md</c>. Se extrae del
/// <c>WorkspaceMembersController</c> al aparecer el segundo consumidor: el dominio no conoce HTTP y
/// la correspondencia debe vivir en un único sitio del borde de transporte.
/// </summary>
public static class WorkspaceMemberErrorMapper
{
    public static ObjectResult ToActionResult(WorkspaceMemberException exception)
    {
        var statusCode = exception.ErrorCode switch
        {
            ErrorCodes.AuthWorkspaceOwnerRequired => StatusCodes.Status403Forbidden,
            ErrorCodes.ResourceNotFound
                or ErrorCodes.WorkspaceNotFound
                or ErrorCodes.ReactivationRequestNotFound => StatusCodes.Status404NotFound,
            _ when exception.ErrorCode.StartsWith("BUSINESS_RULE_", StringComparison.Ordinal)
                => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return new ObjectResult(new ApiErrorResponse(new ApiError(exception.ErrorCode, exception.Message)))
        {
            StatusCode = statusCode
        };
    }
}
