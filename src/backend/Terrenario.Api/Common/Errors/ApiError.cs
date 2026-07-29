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

    /// <summary>MVP-206 — La baja y el traspaso afectan a la propiedad: solo el propietario (CA-3).</summary>
    public static ApiError WorkspaceOwnerRequired() =>
        new(ErrorCodes.AuthWorkspaceOwnerRequired,
            "Solo el propietario del Workspace puede dar de baja o traspasar la propiedad.");

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

    /// <summary>MVP-301 — Cubre también la actividad ya eliminada (RN-037): deja de existir para el diario.</summary>
    public static ApiError ActivityNotFound() =>
        new(ErrorCodes.ResourceNotFound, "La actividad no existe en tu Workspace activo.");

    /// <summary>MVP-303 — Cubre también la compra ya eliminada (RN-037).</summary>
    public static ApiError PurchaseNotFound() =>
        new(ErrorCodes.ResourceNotFound, "La compra no existe en tu Workspace activo.");

    /// <summary>ADR-0005 — <c>PATCH</c>/<c>DELETE</c> de un registro operativo sin <c>If-Match</c>.</summary>
    public static ApiError IfMatchRequired() =>
        new(ErrorCodes.ValidationRequiredIfMatch,
            "Falta la cabecera If-Match con la versión del registro que estás modificando.");

    /// <summary>ADR-0005 — La versión enviada no es la vigente: otra persona tocó el registro antes.</summary>
    public static ApiError VersionMismatch(string message) =>
        new(ErrorCodes.ConflictVersionMismatch, message);
}

public sealed record ApiErrorResponse(ApiError Error);
