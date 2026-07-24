namespace Terrenario.Api.Common.Errors;

public sealed record ApiError(string Code, string Message)
{
    public static ApiError GoogleTokenInvalid() =>
        new(ErrorCodes.AuthGoogleTokenInvalid, "Autenticación no válida. Por favor inténtalo de nuevo.");

    public static ApiError GoogleExchangeFailed() =>
        new(ErrorCodes.AuthGoogleExchangeFailed, "Error al completar el acceso. Por favor inténtalo de nuevo.");

    public static ApiError LoginCancelled() =>
        new(ErrorCodes.AuthLoginCancelled, "El acceso fue cancelado.");

    public static ApiError RefreshTokenInvalid() =>
        new(ErrorCodes.AuthRefreshTokenInvalid, "La sesión ha expirado. Por favor vuelve a iniciar sesión.");

    public static ApiError Unauthenticated() =>
        new(ErrorCodes.AuthUnauthenticated, "Token de acceso ausente o no válido.");

    public static ApiError Validation(string code, string message) => new(code, message);

    public static ApiError WorkspaceNotFound() =>
        new(ErrorCodes.WorkspaceNotFound, "Todavía no tienes ningún Workspace activo.");
}

public sealed record ApiErrorResponse(ApiError Error);
