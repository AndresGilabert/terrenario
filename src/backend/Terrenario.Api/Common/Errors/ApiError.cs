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

    public static ApiError WorkspaceScopeRequired() =>
        new(ErrorCodes.AuthWorkspaceScopeRequired, "Necesitas un Workspace activo para esta operación.");

    public static ApiError WorkspaceForbidden(string? message = null) =>
        new(ErrorCodes.AuthWorkspaceForbidden, message ?? "No tienes acceso a este recurso en tu Workspace activo.");

    public static ApiError SeasonNotFound() =>
        new(ErrorCodes.SeasonNotFound, "Tu Workspace todavía no tiene una temporada activa.");

    public static ApiError SeasonNotFoundById() =>
        new(ErrorCodes.SeasonNotFound, "La temporada no existe en tu Workspace activo.");

    public static ApiError PlotNotFound() =>
        new(ErrorCodes.ResourceNotFound, "El terreno no existe en tu Workspace activo.");

    public static ApiError WorkerNotFound() =>
        new(ErrorCodes.ResourceNotFound, "El trabajador no existe en tu Workspace activo.");

    public static ApiError TaskNotFound() =>
        new(ErrorCodes.ResourceNotFound, "La tarea no existe en el catálogo de tu Workspace activo.");
}

public sealed record ApiErrorResponse(ApiError Error);
