namespace Terrenario.Api.Common.Errors;

public static class ErrorCodes
{
    // Auth errors
    public const string AuthUnauthenticated = "AUTH_UNAUTHENTICATED";
    public const string AuthGoogleTokenInvalid = "AUTH_GOOGLE_TOKEN_INVALID";

    /// <summary>
    /// MVP-713 (`P-079`) — Google respondió <c>invalid_grant</c>: el código de autorización ya se usó o
    /// caducó. Recargar la pantalla de vuelta de Google basta para provocarlo, así que es un error de
    /// <b>quien llama</b> y se responde <c>401</c>. Hasta esta historia caía en
    /// <see cref="AuthGoogleExchangeFailed"/> → <c>500</c>, contaba en el SLO de tasa de error y llegó a
    /// disparar <c>HighErrorRate</c>, que es crítica.
    /// </summary>
    public const string AuthGoogleCodeInvalid = "AUTH_GOOGLE_CODE_INVALID";

    /// <summary>
    /// MVP-713 (`P-079`) — Google respondió <c>invalid_request</c>: falta un parámetro del intercambio o
    /// viene mal formado. Los tres que aporta el cliente (<c>code</c>, <c>redirect_uri</c>,
    /// <c>code_verifier</c>) son suyos, así que es <c>400</c> y no <c>500</c>.
    /// </summary>
    public const string AuthGoogleRequestInvalid = "AUTH_GOOGLE_REQUEST_INVALID";

    /// <summary>
    /// Fallo del servidor en el intercambio con Google: configuración incorrecta
    /// (<c>invalid_client</c>, <c>unauthorized_client</c>), caída del proveedor o respuesta que no se
    /// entiende. Es el <b>caso por defecto</b> desde MVP-713: lo que no se puede atribuir con certeza a
    /// quien llama se sigue tratando como fallo propio.
    /// </summary>
    public const string AuthGoogleExchangeFailed = "AUTH_GOOGLE_EXCHANGE_FAILED";
    public const string AuthLoginCancelled = "AUTH_LOGIN_CANCELLED";
    public const string AuthRefreshTokenInvalid = "AUTH_REFRESH_TOKEN_INVALID";
    public const string AuthWorkspaceForbidden = "AUTH_WORKSPACE_FORBIDDEN";
    public const string AuthWorkspaceScopeRequired = "AUTH_WORKSPACE_SCOPE_REQUIRED";
    public const string AuthInvitationEmailMismatch = "AUTH_INVITATION_EMAIL_MISMATCH";
    // Ciclo de vida del Workspace (MVP-206): baja y traspaso afectan a la propiedad y se
    // restringen a workspace_owner, aunque en MVP el resto de permisos sean planos (RN-034).
    public const string AuthWorkspaceOwnerRequired = "AUTH_WORKSPACE_OWNER_REQUIRED";

    // Validation errors
    public const string ValidationRequired = "VALIDATION_REQUIRED";

    /// <summary>
    /// MVP-502 (P-027/P-043) — El valor llegó, pero con un formato que no se puede interpretar: una
    /// fecha que no lo es, un número donde se esperaba un entero, o un cuerpo cuyos bytes no son
    /// UTF-8 válido. Se distingue de <see cref="ValidationRequired"/> ("falta") a propósito: son dos
    /// arreglos distintos para el cliente.
    /// </summary>
    public const string ValidationFormatInvalid = "VALIDATION_FORMAT_INVALID";
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
    // Ciclo de vida del Workspace (MVP-206)
    public const string ValidationRequiredReactivationContext = "VALIDATION_REQUIRED_REACTIVATION_CONTEXT";
    public const string ValidationRequiredNewOwner = "VALIDATION_REQUIRED_NEW_OWNER";
    // Actividades (MVP-301)
    public const string ValidationActivityRequiredFields = "VALIDATION_ACTIVITY_REQUIRED_FIELDS";
    public const string ValidationActivityTaskRequired = "VALIDATION_ACTIVITY_TASK_REQUIRED";
    public const string ValidationActivityTaskTextLength = "VALIDATION_ACTIVITY_TASK_TEXT_LENGTH";
    public const string ValidationActivityHoursRange = "VALIDATION_ACTIVITY_HOURS_RANGE";
    public const string ValidationActivityCostRange = "VALIDATION_ACTIVITY_COST_RANGE";
    public const string ValidationActivityDescriptionLength = "VALIDATION_ACTIVITY_DESCRIPTION_LENGTH";
    /// <summary>
    /// MVP-302 — Se pidió guardar la tarea en el catálogo sobre una actividad cuya tarea <b>ya</b>
    /// viene del catálogo: no hay nada que guardar. Se responde en vez de ignorarlo en silencio.
    /// </summary>
    public const string ValidationActivityTaskNotFreeText = "VALIDATION_ACTIVITY_TASK_NOT_FREE_TEXT";
    // Compras (MVP-303)
    public const string ValidationPurchaseRequiredFields = "VALIDATION_PURCHASE_REQUIRED_FIELDS";
    public const string ValidationPurchaseRequiredProduct = "VALIDATION_PURCHASE_REQUIRED_PRODUCT";
    public const string ValidationPurchaseProductLength = "VALIDATION_PURCHASE_PRODUCT_LENGTH";
    public const string ValidationPurchaseTotalsRange = "VALIDATION_PURCHASE_TOTALS_RANGE";
    // Consumos e imputaciones (MVP-304)
    public const string ValidationConsumptionRequiredFields = "VALIDATION_CONSUMPTION_REQUIRED_FIELDS";
    public const string ValidationConsumptionRequiredProduct = "VALIDATION_CONSUMPTION_REQUIRED_PRODUCT";
    public const string ValidationConsumptionProductLength = "VALIDATION_CONSUMPTION_PRODUCT_LENGTH";
    public const string ValidationConsumptionQuantityRange = "VALIDATION_CONSUMPTION_QUANTITY_RANGE";
    /// <summary>
    /// MVP-304 — La suma de imputaciones vivas de una compra superaría su cantidad total. No se puede
    /// repartir más material del que se compró.
    /// </summary>
    public const string ValidationConsumptionOverflow = "VALIDATION_CONSUMPTION_OVERFLOW";
    // Cosechas (MVP-401)
    public const string ValidationHarvestRequiredFields = "VALIDATION_HARVEST_REQUIRED_FIELDS";
    /// <summary>RN-004 — sin kilos no hay cosecha que medir.</summary>
    public const string ValidationHarvestKgsRequired = "VALIDATION_HARVEST_KGS_REQUIRED";
    /// <summary>
    /// RN-004 — <c>yield</c> y <c>liters</c> no pueden coexistir: son dos formas de medir lo mismo y
    /// guardar las dos permitiría que se contradijeran.
    /// </summary>
    public const string ValidationHarvestXorYieldLiters = "VALIDATION_HARVEST_XOR_YIELD_LITERS";
    public const string ValidationHarvestYieldRange = "VALIDATION_HARVEST_YIELD_RANGE";
    /// <summary>MVP-402 — La unidad de rendimiento no está en el catálogo `l_100kg` / `kg_100kg` (RN-014).</summary>
    public const string ValidationHarvestYieldUnitInvalid = "VALIDATION_HARVEST_YIELD_UNIT_INVALID";
    public const string ValidationHarvestLitersRange = "VALIDATION_HARVEST_LITERS_RANGE";

    /// <summary>MVP-707 — Precio por kilo fuera de rango (o cero explícito, que no es «sin dato»).</summary>
    public const string ValidationHarvestUnitPriceRange = "VALIDATION_HARVEST_UNIT_PRICE_RANGE";
    /// <summary>RN-030 — producto de cosecha obligatorio; el catálogo cerrado lo aplica MVP-402.</summary>
    public const string ValidationProductInvalid = "VALIDATION_PRODUCT_INVALID";
    /// <summary>RN-012 — destino de cosecha obligatorio; el catálogo cerrado lo aplica MVP-402.</summary>
    public const string ValidationDestinationInvalid = "VALIDATION_DESTINATION_INVALID";
    /// <summary>
    /// Registros operativos (ADR-0005): <c>PATCH</c>/<c>DELETE</c> exigen <c>If-Match</c> con la
    /// versión vigente. Sin cabecera no hay control de concurrencia posible, así que la petición se
    /// rechaza en vez de escribir a ciegas.
    /// </summary>
    public const string ValidationRequiredIfMatch = "VALIDATION_REQUIRED_IF_MATCH";
    /// <summary>
    /// Un vínculo del registro operativo (terreno, temporada, responsable, tarea o compra) no existe
    /// en el Workspace activo. Se responde <c>400</c> y no <c>404</c>: lo que falla es el cuerpo de la
    /// petición, no la ruta.
    /// </summary>
    public const string ForeignKeyWorkspaceMismatch = "FOREIGN_KEY_WORKSPACE_MISMATCH";

    // Business rules
    public const string BusinessRuleInvitationExpired = "BUSINESS_RULE_INVITATION_EXPIRED";
    public const string BusinessRuleInvitationAlreadyAccepted = "BUSINESS_RULE_INVITATION_ALREADY_ACCEPTED";
    public const string BusinessRuleInvitationAlreadyRejected = "BUSINESS_RULE_INVITATION_ALREADY_REJECTED";
    /// <summary>MVP-207 (CA-4) — El Workspace emisor anuló la invitación: su enlace ya no sirve.</summary>
    public const string BusinessRuleInvitationCancelled = "BUSINESS_RULE_INVITATION_CANCELLED";
    public const string BusinessRuleInvitationAlreadyMember = "BUSINESS_RULE_INVITATION_ALREADY_MEMBER";
    // Administración de miembros (MVP-204, CA-8)
    public const string BusinessRuleLastActiveMember = "BUSINESS_RULE_LAST_ACTIVE_MEMBER";
    public const string BusinessRuleCannotRevokeOwner = "BUSINESS_RULE_CANNOT_REVOKE_OWNER";
    // Maestro de responsables (MVP-208, CA-4): lo que un responsable con cuenta no admite editar,
    // porque lo gobiernan su identidad de Google (RN-036) y su membresía (RN-027), no el maestro.
    public const string BusinessRuleWorkerIdentityManaged = "BUSINESS_RULE_WORKER_IDENTITY_MANAGED";
    public const string BusinessRuleWorkerMembershipManaged = "BUSINESS_RULE_WORKER_MEMBERSHIP_MANAGED";
    /// <summary>
    /// MVP-304 — No se da de baja una compra que todavía tiene imputaciones vivas: esos consumos son
    /// registros operativos propios que están en el diario, y borrarlos en cascada eliminaría datos
    /// que nadie pidió eliminar. Primero se retiran las imputaciones.
    /// </summary>
    public const string BusinessRulePurchaseHasConsumptions = "BUSINESS_RULE_PURCHASE_HAS_CONSUMPTIONS";
    // Ciclo de vida del Workspace (MVP-206)
    public const string BusinessRuleWorkspaceDeleted = "BUSINESS_RULE_WORKSPACE_DELETED";
    public const string BusinessRuleWorkspaceNotDeleted = "BUSINESS_RULE_WORKSPACE_NOT_DELETED";
    public const string BusinessRuleOwnershipTransferToSelf = "BUSINESS_RULE_OWNERSHIP_TRANSFER_TO_SELF";
    public const string BusinessRuleReactivationAlreadyUsed = "BUSINESS_RULE_REACTIVATION_ALREADY_USED";
    public const string BusinessRuleReactivationExpired = "BUSINESS_RULE_REACTIVATION_EXPIRED";
    public const string BusinessRuleReactivationNotRequested = "BUSINESS_RULE_REACTIVATION_NOT_REQUESTED";
    /// <summary>Baja de cuenta con Workspaces de propiedad única sin resolver (CA-9).</summary>
    public const string BusinessRuleWorkspaceOwnershipUnresolved = "BUSINESS_RULE_WORKSPACE_OWNERSHIP_UNRESOLVED";

    // Conflictos
    // Nombre repetido dentro del mismo Workspace, ignorando mayúsculas. Cada maestro tiene su propio
    // código para que la UI pueda explicar el conflicto en los términos del recurso. La guarda nació
    // en el catálogo de tareas (MVP-205) y MVP-207 la extiende al resto de maestros (CA-2).
    public const string ConflictTaskNameDuplicate = "CONFLICT_TASK_NAME_DUPLICATE";
    public const string ConflictSeasonNameDuplicate = "CONFLICT_SEASON_NAME_DUPLICATE";
    public const string ConflictWorkerNameDuplicate = "CONFLICT_WORKER_NAME_DUPLICATE";
    public const string ConflictPlotNameDuplicate = "CONFLICT_PLOT_NAME_DUPLICATE";
    /// <summary>
    /// Edición o borrado de un registro operativo con una versión desfasada (ADR-0005). Es un único
    /// código para todas las entidades críticas: actividades (MVP-301), compras (MVP-303),
    /// imputaciones y consumos (MVP-304) y cosechas (MVP-401).
    /// </summary>
    public const string ConflictVersionMismatch = "CONFLICT_VERSION_MISMATCH";

    // Resources
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string WorkspaceNotFound = "WORKSPACE_NOT_FOUND";
    public const string InvitationNotFound = "INVITATION_NOT_FOUND";
    public const string SeasonNotFound = "SEASON_NOT_FOUND";
    public const string ReactivationRequestNotFound = "REACTIVATION_REQUEST_NOT_FOUND";

    // Generic
    public const string InternalError = "INTERNAL_ERROR";
}
