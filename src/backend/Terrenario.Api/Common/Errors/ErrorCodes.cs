namespace Terrenario.Api.Common.Errors;

public static class ErrorCodes
{
    // Auth errors
    public const string AuthUnauthenticated = "AUTH_UNAUTHENTICATED";
    public const string AuthGoogleTokenInvalid = "AUTH_GOOGLE_TOKEN_INVALID";
    public const string AuthGoogleExchangeFailed = "AUTH_GOOGLE_EXCHANGE_FAILED";
    public const string AuthLoginCancelled = "AUTH_LOGIN_CANCELLED";
    public const string AuthRefreshTokenInvalid = "AUTH_REFRESH_TOKEN_INVALID";
    public const string AuthWorkspaceForbidden = "AUTH_WORKSPACE_FORBIDDEN";

    // Validation errors
    public const string ValidationRequired = "VALIDATION_REQUIRED";
    public const string ValidationRequiredWorkspaceName = "VALIDATION_REQUIRED_WORKSPACE_NOMBRE";
    public const string ValidationWorkspaceNameLength = "VALIDATION_WORKSPACE_NOMBRE_LENGTH";
    public const string ValidationRequiredWorkspaceOwner = "VALIDATION_REQUIRED_WORKSPACE_OWNER";

    // Resources
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string WorkspaceNotFound = "WORKSPACE_NOT_FOUND";

    // Generic
    public const string InternalError = "INTERNAL_ERROR";
}
