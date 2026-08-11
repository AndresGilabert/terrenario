using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Terrenario.Api.Application.Masters;
using Terrenario.Api.Domain.Masters;
using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Common.Errors;

/// <summary>
/// MVP-806 — Traduce al contrato lo que puede impedir depurar un maestro. Es un filtro y no cuatro
/// bloques <c>try/catch</c> por el mismo motivo que <c>WorkspaceAccessExceptionFilter</c>: los cuatro
/// maestros comparten exactamente las mismas reglas, y copiarlas cuatro veces es garantizar que la
/// quinta se olvide.
///
/// <list type="bullet">
///   <item><see cref="MasterOperationException"/> → <c>422</c> con su código (la ficha tiene
///   histórico, o es la de un miembro).</item>
///   <item><see cref="MasterLinkException"/> → <c>400 FOREIGN_KEY_WORKSPACE_MISMATCH</c>: lo que no
///   existe llega en el cuerpo, no en la ruta.</item>
///   <item><see cref="ConcurrencyConflictException"/> → <c>409 CONFLICT_VERSION_MISMATCH</c>. Los
///   controladores de las entidades operativas lo siguen atrapando ellos mismos, porque su respuesta
///   lleva además <c>current_version</c>; la fusión no edita un registro concreto, así que solo tiene
///   el código y el mensaje.</item>
/// </list>
/// </summary>
public sealed class MasterDepurationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        // Nunca se toca `context.Result` si la excepción no es de esta historia: otro filtro pudo
        // haberlo resuelto ya, y sobrescribirlo con `null` lo convertiría en un 500.
        IActionResult? result = context.Exception switch
        {
            MasterOperationException ex => new ObjectResult(
                new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)))
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity
            },
            MasterLinkException ex => new BadRequestObjectResult(
                new ApiErrorResponse(ApiError.Validation(ErrorCodes.ForeignKeyWorkspaceMismatch, ex.Message))),
            ConcurrencyConflictException ex => new ConflictObjectResult(
                new ApiErrorResponse(ApiError.VersionMismatch(ex.Message))),
            _ => null
        };

        if (result is null) return;

        context.Result = result;
        context.ExceptionHandled = true;
    }
}
