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
    public const string AuthWorkspaceScopeRequired = "AUTH_WORKSPACE_SCOPE_REQUIRED";
    public const string AuthInvitationEmailMismatch = "AUTH_INVITATION_EMAIL_MISMATCH";

    // Validation errors
    public const string ValidationRequired = "VALIDATION_REQUIRED";
    public const string ValidationRequiredWorkspaceName = "VALIDATION_REQUIRED_WORKSPACE_NAME";
    public const string ValidationWorkspaceNameLength = "VALIDATION_WORKSPACE_NAME_LENGTH";
    public const string ValidationRequiredWorkspaceOwner = "VALIDATION_REQUIRED_WORKSPACE_OWNER";
    public const string ValidationRequiredInvitationContext = "VALIDATION_REQUIRED_INVITATION_CONTEXT";
    public const string ValidationRequiredInvitationEmail = "VALIDATION_REQUIRED_INVITATION_EMAIL";
    public const string ValidationInvitationEmailInvalid = "VALIDATION_INVITATION_EMAIL_INVALID";
    public const string ValidationInvitationChannelInvalid = "VALIDATION_INVITATION_CHANNEL_INVALID";

    // Business rules
    public const string BusinessRuleInvitationExpired = "BUSINESS_RULE_INVITATION_EXPIRED";
    public const string BusinessRuleInvitationAlreadyAccepted = "BUSINESS_RULE_INVITATION_ALREADY_ACCEPTED";
    public const string BusinessRuleInvitationAlreadyMember = "BUSINESS_RULE_INVITATION_ALREADY_MEMBER";

    // Resources
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string WorkspaceNotFound = "WORKSPACE_NOT_FOUND";
    public const string InvitationNotFound = "INVITATION_NOT_FOUND";

    // Generic
    public const string InternalError = "INTERNAL_ERROR";
}
