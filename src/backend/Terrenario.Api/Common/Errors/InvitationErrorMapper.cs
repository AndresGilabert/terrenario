using Microsoft.AspNetCore.Mvc;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Common.Errors;

/// <summary>
/// Traduce los errores de dominio de invitaciones a los códigos HTTP de
/// <c>docs/02-arquitectura/contratos-api.md</c>. El dominio no conoce HTTP: la correspondencia
/// vive aquí, en el borde de transporte.
/// </summary>
public static class InvitationErrorMapper
{
    public static ObjectResult ToActionResult(InvitationException exception)
    {
        var statusCode = exception.ErrorCode switch
        {
            ErrorCodes.AuthUnauthenticated => StatusCodes.Status401Unauthorized,
            ErrorCodes.AuthInvitationEmailMismatch => StatusCodes.Status403Forbidden,
            ErrorCodes.InvitationNotFound or ErrorCodes.WorkspaceNotFound => StatusCodes.Status404NotFound,
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
