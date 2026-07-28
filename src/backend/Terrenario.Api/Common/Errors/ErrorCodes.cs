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
    public const string ValidationRequiredSeasonName = "VALIDATION_REQUIRED_SEASON_NAME";
    public const string ValidationSeasonNameLength = "VALIDATION_SEASON_NAME_LENGTH";
    public const string ValidationSeasonDateRange = "VALIDATION_SEASON_DATE_RANGE";
    public const string ValidationRequiredSeasonWorkspace = "VALIDATION_REQUIRED_SEASON_WORKSPACE";
    // Plots (terrenos, MVP-202)
    public const string ValidationRequiredName = "VALIDATION_REQUIRED_NAME";
    public const string ValidationPlotNameLength = "VALIDATION_PLOT_NAME_LENGTH";
    public const string ValidationRequiredPlotOwnershipType = "VALIDATION_REQUIRED_PLOT_OWNERSHIP_TYPE";
    public const string ValidationPlotOwnershipTypeInvalid = "VALIDATION_PLOT_OWNERSHIP_TYPE_INVALID";
    public const string ValidationPlotAliasLength = "VALIDATION_PLOT_ALIAS_LENGTH";
    public const string ValidationPlotOwnerNameLength = "VALIDATION_PLOT_OWNER_NAME_LENGTH";
    public const string ValidationPlotCadastralLength = "VALIDATION_PLOT_CADASTRAL_LENGTH";
    public const string ValidationPlotLocationLength = "VALIDATION_PLOT_LOCATION_LENGTH";
    public const string ValidationRangeTreeCount = "VALIDATION_RANGE_TREE_COUNT";
    public const string ValidationRequiredPlotWorkspace = "VALIDATION_REQUIRED_PLOT_WORKSPACE";
    // Workers (trabajadores, MVP-204)
    public const string ValidationWorkerNameLength = "VALIDATION_WORKER_NAME_LENGTH";
    public const string ValidationRangeHourlyRate = "VALIDATION_RANGE_HOURLY_RATE";
    public const string ValidationRequiredWorkerWorkspace = "VALIDATION_REQUIRED_WORKER_WORKSPACE";
    // Tasks (catálogo de tareas, MVP-205)
    public const string ValidationRequiredTaskName = "VALIDATION_REQUIRED_TASK_NAME";
    public const string ValidationTaskNameLength = "VALIDATION_TASK_NAME_LENGTH";
    public const string ValidationRequiredTaskWorkspace = "VALIDATION_REQUIRED_TASK_WORKSPACE";

    // Business rules
    public const string BusinessRuleInvitationExpired = "BUSINESS_RULE_INVITATION_EXPIRED";
    public const string BusinessRuleInvitationAlreadyAccepted = "BUSINESS_RULE_INVITATION_ALREADY_ACCEPTED";
    public const string BusinessRuleInvitationAlreadyRejected = "BUSINESS_RULE_INVITATION_ALREADY_REJECTED";
    public const string BusinessRuleInvitationAlreadyMember = "BUSINESS_RULE_INVITATION_ALREADY_MEMBER";
    // Administración de miembros (MVP-204, CA-8)
    public const string BusinessRuleLastActiveMember = "BUSINESS_RULE_LAST_ACTIVE_MEMBER";
    public const string BusinessRuleCannotRevokeOwner = "BUSINESS_RULE_CANNOT_REVOKE_OWNER";

    // Conflictos
    // Catálogo de tareas (MVP-205): nombre repetido en el mismo Workspace, ignorando mayúsculas.
    public const string ConflictTaskNameDuplicate = "CONFLICT_TASK_NAME_DUPLICATE";

    // Resources
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string WorkspaceNotFound = "WORKSPACE_NOT_FOUND";
    public const string InvitationNotFound = "INVITATION_NOT_FOUND";
    public const string SeasonNotFound = "SEASON_NOT_FOUND";

    // Generic
    public const string InternalError = "INTERNAL_ERROR";
}
