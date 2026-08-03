using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Terrenario.Api.Application.Account;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-505 (HU-3, CA-3/CA-4) — <b>Baja de cuenta</b>: el derecho de supresión ejercido desde la
/// propia aplicación.
///
/// No lleva <c>[RequireWorkspaceScope]</c> a propósito, a diferencia del resto de recursos: la baja
/// es de la <b>cuenta</b>, no de un Workspace, y quien no tenga ninguno —o lo haya perdido— también
/// tiene derecho a ejercerla. Exigir contexto de Workspace dejaría fuera justo a quien más fácil lo
/// tiene para querer irse.
///
/// El verbo es <c>POST</c> sobre <c>/closure</c> y no <c>DELETE /account</c>, por coherencia con la
/// baja de Workspace de <c>MVP-206</c> (<c>POST /workspaces/active/closure</c>) y porque la operación
/// necesita cuerpo: la confirmación explícita que exige CA-3.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/account")]
public sealed class AccountController(CloseAccountHandler closeAccountHandler) : ControllerBase
{
    /// <summary>
    /// Frase que hay que escribir para confirmar. Es el patrón de las operaciones irreversibles: no
    /// se puede completar por un clic de más, hay que teclear la intención.
    /// </summary>
    public const string ConfirmationPhrase = "ELIMINAR MI CUENTA";

    /// <summary>
    /// Qué pasará si se confirma y qué lo bloquea. La confirmación tiene que ser <b>informada</b>:
    /// decir «esto es irreversible» sin decir qué se lleva por delante no es informar.
    /// </summary>
    [HttpGet("closure")]
    public async Task<IActionResult> GetClosure(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        var preview = await closeAccountHandler.PreviewAsync(userId.Value, ct);

        return Ok(new
        {
            // CA-4 — RN-038: la baja no puede dejar Workspaces huérfanos. Mientras esta lista no esté
            // vacía, la baja se rechaza y la UI puede llevar a resolver cada uno.
            is_clear = preview.IsClear,
            obligations = preview.Obligations.Select(ToObligationResponse),
            active_memberships = preview.ActiveMemberships,
            active_sessions = preview.ActiveSessions,
            confirmation_phrase = ConfirmationPhrase,
            retention_months = AccountRetentionPolicy.RetentionMonths
        });
    }

    /// <summary>
    /// Ejecuta la baja. Es <b>irreversible</b>: no hay periodo de gracia ni papelera, y volver a
    /// entrar con la misma cuenta de Google crea una cuenta nueva y vacía.
    /// </summary>
    [HttpPost("closure")]
    public async Task<IActionResult> CloseAccount(
        [FromBody] CloseAccountRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized(new ApiErrorResponse(ApiError.Unauthenticated()));

        // La comprobación se hace también en servidor, no solo en el diálogo: una operación
        // irreversible no puede depender de que el cliente se porte bien.
        if (!string.Equals(request.Confirmation?.Trim(), ConfirmationPhrase, StringComparison.Ordinal))
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationRequired,
                $"Para eliminar la cuenta escribe exactamente «{ConfirmationPhrase}».")));

        try
        {
            var result = await closeAccountHandler.HandleAsync(userId.Value, ct);

            // La cookie de refresco muere con la cuenta: si no, el navegador seguiría presentándola.
            RemoveRefreshTokenCookie();

            return Ok(new
            {
                revoked_sessions = result.RevokedSessions,
                revoked_memberships = result.RevokedMemberships,
                cancelled_invitations = result.CancelledInvitations,
                // RN-041 — cuándo se purgará físicamente lo que queda anonimizado. Se devuelve para
                // que la persona sepa qué se conserva y hasta cuándo, no solo que «se ha borrado».
                purge_after = result.PurgeAfter
            });
        }
        catch (WorkspaceMemberException ex)
        {
            // CA-4 — quedan Workspaces de propiedad única sin resolver (RN-038). 422: la petición es
            // correcta, es el estado del sistema el que no permite completarla.
            return UnprocessableEntity(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (AccountClosureException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    private static object ToObligationResponse(SoleOwnedWorkspace workspace) => new
    {
        workspace_id = workspace.WorkspaceId,
        name = workspace.Name,
        other_active_members = workspace.OtherActiveMembers
    };

    private void RemoveRefreshTokenCookie() =>
        Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth"
        });
}

/// <summary>Confirmación explícita de la baja (CA-3).</summary>
public sealed record CloseAccountRequest(
    [Required(ErrorMessage = "Escribe la frase de confirmación para eliminar la cuenta.")]
    string? Confirmation);
