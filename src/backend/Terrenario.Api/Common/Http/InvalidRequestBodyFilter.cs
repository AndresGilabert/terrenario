using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Common.Http;

/// <summary>
/// MVP-502 (<c>P-027</c>) — Traduce <see cref="InvalidRequestBodyException"/> a <c>400</c> con el
/// contrato de error de la API, de forma uniforme y en un único sitio.
///
/// Va en el borde de transporte por el mismo motivo que
/// <see cref="Workspaces.WorkspaceAccessExceptionFilter"/>: un cuerpo mal codificado es un problema
/// de la petición, no de un dominio concreto, y repetir el <c>try/catch</c> en los ocho controladores
/// con edición parcial garantizaría que alguno se quedara sin él.
/// </summary>
public sealed class InvalidRequestBodyFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not InvalidRequestBodyException ex)
            return;

        context.Result = new BadRequestObjectResult(new ApiErrorResponse(
            ApiError.Validation(ErrorCodes.ValidationFormatInvalid, ex.Message)));
        context.ExceptionHandled = true;
    }
}
