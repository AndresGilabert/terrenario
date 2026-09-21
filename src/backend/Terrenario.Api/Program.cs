using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Terrenario.Api.Application.Account;
using Terrenario.Api.Application.Activities;
using Terrenario.Api.Application.Auth;
using Terrenario.Api.Application.Consumptions;
using Terrenario.Api.Application.Dashboard;
using Terrenario.Api.Application.Feedback;
using Terrenario.Api.Application.Diary;
using Terrenario.Api.Application.Harvests;
using Terrenario.Api.Application.Invitations;
using Terrenario.Api.Application.Masters;
using Terrenario.Api.Application.Materials;
using Terrenario.Api.Application.Plots;
using Terrenario.Api.Application.Purchases;
using Terrenario.Api.Application.Seasons;
using Terrenario.Api.Application.Tasks;
using Terrenario.Api.Application.Workers;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Activities;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Diary;
using Terrenario.Api.Domain.Harvests;
using Terrenario.Api.Domain.Masters;
using Terrenario.Api.Domain.Materials;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Purchases;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Tasks;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workers;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Auth;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Infrastructure.Email;
using Terrenario.Api.Infrastructure.Feedback;
using Terrenario.Api.Infrastructure.Invitations;
using Terrenario.Api.Infrastructure.Telemetry;
using Terrenario.Api.Infrastructure.Telemetry.Alerts;
using Terrenario.Api.Infrastructure.Telemetry.Summary;
using Terrenario.Api.Application.Ops;
using Terrenario.Api.Application.Retention;
using Terrenario.Api.Infrastructure.Retention;
using Terrenario.Api.Infrastructure.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Logs ─────────────────────────────────────────────────────────────────────
//
// MVP-601 — Fuera de desarrollo, los logs salen en JSON con `timestamp` y con los scopes incluidos.
// `docs/05-infraestructura/observabilidad.md` exige una estructura de log con marca de tiempo y
// contexto; con el formateador de texto por defecto, las dimensiones del embudo salen interpoladas
// dentro de una frase y reconstruir el embudo pasa por analizar prosa.
//
// En desarrollo se conserva el formato legible: allí los logs se leen con los ojos.
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;   // arrastra el `RequestId` de `RequestIdMiddleware` (P-006)
        options.UseUtcTimestamp = true;
        options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
    });
}

// ── Options ─────────────────────────────────────────────────────────────────
builder.Services.Configure<GoogleOidcOptions>(
    builder.Configuration.GetSection(GoogleOidcOptions.SectionName));
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<RefreshTokenOptions>(
    builder.Configuration.GetSection(RefreshTokenOptions.SectionName));
builder.Services.Configure<InvitationOptions>(
    builder.Configuration.GetSection(InvitationOptions.SectionName));
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<WorkspaceLifecycleOptions>(
    builder.Configuration.GetSection(WorkspaceLifecycleOptions.SectionName));
builder.Services.Configure<RetentionOptions>(
    builder.Configuration.GetSection(RetentionOptions.SectionName));
builder.Services.Configure<TelemetryOptions>(
    builder.Configuration.GetSection(TelemetryOptions.SectionName));
builder.Services.Configure<OpsOptions>(
    builder.Configuration.GetSection(OpsOptions.SectionName));
builder.Services.Configure<FeedbackOptions>(
    builder.Configuration.GetSection(FeedbackOptions.SectionName));
builder.Services.Configure<DomainRedirectOptions>(
    builder.Configuration.GetSection(DomainRedirectOptions.SectionName));
// MVP-715 — La identidad del responsable del tratamiento se puede ajustar por despliegue, pero lo
// que no se configura sale del fichero versionado que comparte con las páginas legales: un campo en
// blanco dejaría un hueco en un texto que la normativa obliga a publicar.
builder.Services.AddOptions<LegalEntityOptions>()
    .Bind(builder.Configuration.GetSection(LegalEntityOptions.SectionName))
    .PostConfigure(legal => legal.FillBlanksFrom(VersionedLegalEntity.Value));

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<TerrenarioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty));

// ── HTTP clients ─────────────────────────────────────────────────────────────
builder.Services.AddHttpClient("google-oauth");

// ── Auth ─────────────────────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var publicKeyPem = jwtSection["PublicKeyPem"] ?? string.Empty;

var rsa = RSA.Create();
if (!string.IsNullOrWhiteSpace(publicKeyPem))
    rsa.ImportFromPem(publicKeyPem);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // Keep standard JWT claim names (no "sub" → ClaimTypes.NameIdentifier mapping)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"] ?? "terrenario-api",
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"] ?? "terrenario-web",
            ValidateLifetime = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ── Application services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGoogleOidcService, GoogleOidcService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
// ── Observabilidad (MVP-601) ─────────────────────────────────────────────────
//
// El acumulador y el registro de tiempos son **singleton**: los eventos llegan desde peticiones
// distintas y un intento de login empieza en una petición y termina en otra. El emisor sigue siendo
// scoped porque su logger lo es.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TelemetryCounterAccumulator>();
// MVP-603 — Cada medida va a dos sitios: la serie diaria que se conserva y la ventana corta sobre la
// que deciden las alertas. Quien mide sigue llamando a `ITelemetryCounters` sin saberlo.
builder.Services.AddSingleton<RollingWindowMetrics>();
builder.Services.AddSingleton<ITelemetryCounters, CompositeTelemetryCounters>();
builder.Services.AddSingleton<LoginFlowTimings>();
// MKT-106 — Mismo motivo que `LoginFlowTimings`: la clasificación de entrada se fija en una petición
// (pantalla vista) y se recupera en otra (éxito).
builder.Services.AddSingleton<LoginFlowEntries>();
builder.Services.AddScoped<ITelemetryCounterStore, TelemetryCounterStore>();
builder.Services.AddHostedService<TelemetryFlushWorker>();
// Salud y vigilancia (MVP-603). El estado de las alertas es singleton: la vigilancia lo escribe y la
// revisión operativa lo lee.
builder.Services.AddScoped<HealthProbe>();
builder.Services.AddScoped<IAlertNotifier, AlertNotifier>();
builder.Services.AddSingleton<AlertStateStore>();
builder.Services.AddHostedService<AlertMonitor>();
builder.Services.AddScoped<OperationalSignalsService>();
// MKT-101 — Resumen operativo periódico. Reutiliza el mismo destinatario que las alertas
// (`Ops:AlertEmail`) y el mismo transporte/plantilla que el resto de correos del producto.
builder.Services.AddHostedService<OperationalSummaryWorker>();
builder.Services.AddScoped<ILoginTelemetry, LoginTelemetryService>();
// MVP-602 — Señales de uso del producto: comparten acumulador y almacén con el embudo de login.
builder.Services.AddScoped<IUsageTelemetry, UsageTelemetryService>();
builder.Services.AddScoped<ExchangeGoogleCodeHandler>();
builder.Services.AddScoped<RefreshTokenHandler>();
builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
builder.Services.AddScoped<IActiveWorkspaceResolver, ActiveWorkspaceResolver>();
// Contexto de Workspace por petición (MVP-105): el filtro de scope lo rellena y controllers/handlers lo leen.
builder.Services.AddScoped<WorkspaceScopeContext>();
builder.Services.AddScoped<IWorkspaceContext>(sp => sp.GetRequiredService<WorkspaceScopeContext>());
builder.Services.AddScoped<WorkspaceScopeFilter>();
builder.Services.AddScoped<CreateWorkspaceHandler>();
builder.Services.AddScoped<ListUserWorkspacesHandler>();
builder.Services.AddScoped<SwitchActiveWorkspaceHandler>();
builder.Services.AddScoped<ISeasonRepository, SeasonRepository>();
builder.Services.AddScoped<GetActiveSeasonHandler>();
builder.Services.AddScoped<ListSeasonsHandler>();
builder.Services.AddScoped<CreateSeasonHandler>();
builder.Services.AddScoped<UpdateSeasonHandler>();
builder.Services.AddScoped<ActivateSeasonHandler>();
builder.Services.AddScoped<IPlotRepository, PlotRepository>();
builder.Services.AddScoped<CreatePlotHandler>();
builder.Services.AddScoped<UpdatePlotHandler>();
builder.Services.AddScoped<ListPlotsHandler>();
// Maestro de responsables y administración de miembros (MVP-204 · MVP-208)
builder.Services.AddScoped<IWorkerRepository, WorkerRepository>();
// Mantiene el maestro alineado con la membresía (MVP-208, CA-1/CA-4). Lo usan la creación de
// Workspace, la aceptación de invitación, la revocación de acceso y el login (RN-036).
builder.Services.AddScoped<MemberRosterService>();
builder.Services.AddScoped<CreateWorkerHandler>();
builder.Services.AddScoped<UpdateWorkerHandler>();
builder.Services.AddScoped<ListWorkersHandler>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
// Guardado de una tarea libre en el catálogo desde el flujo de actividad (MVP-302). Reutiliza la
// guarda de duplicados de MVP-205 para resolver el nombre en vez de chocar contra ella.
builder.Services.AddScoped<TaskCatalogPromoter>();
builder.Services.AddScoped<CreateTaskHandler>();
builder.Services.AddScoped<UpdateTaskHandler>();
builder.Services.AddScoped<ListTasksHandler>();
// Diario de actividades (MVP-301): primera entidad operativa crítica (ADR-0005 + RN-037)
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<ActivityLinkResolver>();
builder.Services.AddScoped<CreateActivityHandler>();
builder.Services.AddScoped<UpdateActivityHandler>();
builder.Services.AddScoped<DeleteActivityHandler>();
builder.Services.AddScoped<ListActivitiesHandler>();
builder.Services.AddScoped<GetActivityHandler>();
// Libro de compras (MVP-303): segunda entidad operativa crítica, mismo patrón que las actividades
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<PurchaseSeasonResolver>();
builder.Services.AddScoped<CreatePurchaseHandler>();
builder.Services.AddScoped<UpdatePurchaseHandler>();
builder.Services.AddScoped<DeletePurchaseHandler>();
builder.Services.AddScoped<ListPurchasesHandler>();
// Vocabulario de materiales (RN-031). MVP-708 (`P-057`) lo saca del puerto de compras: se aprende de
// los dos libros, así que un método en `IPurchaseRepository` sería una firma que miente.
builder.Services.AddScoped<IMaterialRepository, MaterialRepository>();
builder.Services.AddScoped<ListMaterialSuggestionsHandler>();
// Imputación por terrenos y consumo sin compra previa (MVP-304)
builder.Services.AddScoped<IConsumptionRepository, ConsumptionRepository>();
builder.Services.AddScoped<ConsumptionLinkResolver>();
builder.Services.AddScoped<PurchaseImputationGuard>();
builder.Services.AddScoped<ImputePurchaseHandler>();
builder.Services.AddScoped<RegisterConsumptionHandler>();
builder.Services.AddScoped<UpdateConsumptionHandler>();
builder.Services.AddScoped<DeleteConsumptionHandler>();
builder.Services.AddScoped<ListConsumptionsHandler>();
// Cosechas (MVP-401): cuarta entidad operativa crítica y materia prima del dashboard
builder.Services.AddScoped<IHarvestRepository, HarvestRepository>();
builder.Services.AddScoped<HarvestLinkResolver>();
builder.Services.AddScoped<CreateHarvestHandler>();
builder.Services.AddScoped<UpdateHarvestHandler>();
builder.Services.AddScoped<DeleteHarvestHandler>();
builder.Services.AddScoped<ListHarvestsHandler>();
builder.Services.AddScoped<GetHarvestHandler>();
builder.Services.AddScoped<FindHarvestDuplicatesHandler>();
// MVP-806 — Depuración de los cuatro maestros: borrado de lo nunca usado y fusión de duplicados. Un
// solo puerto para los cuatro, porque la parte delicada —comprobar el uso contra TODAS las entidades
// que pueden referenciar la ficha— es la misma operación y solo cambia la lista de referencias.
builder.Services.AddScoped<IMasterRepository, MasterRepository>();
builder.Services.AddScoped<MasterUsageService>();
builder.Services.AddScoped<DeleteMasterHandler>();
builder.Services.AddScoped<MergeMastersHandler>();
// MVP-701 — Defecto de temporada de RN-008 en un único punto, compartido por diario, cosechas y
// compras. Antes cada vista arrancaba en «todas» por su cuenta y el dashboard resolvía el defecto en
// servidor: dos pantallas respondían distinto a «cuánto llevo esta campaña» (`P-082`).
builder.Services.AddScoped<SeasonScopeResolver>();
// Dashboard (MVP-403): agrega la producción capturada; solo lectura y sin refresco continuo (RN-006)
builder.Services.AddScoped<DashboardScopeResolver>();
builder.Services.AddScoped<DashboardQueryService>();
// MVP-707 — Lectura económica de la campaña (RN-009 ampliada). Le pregunta el gasto al diario en vez
// de recalcularlo: es donde vive la decisión de qué cuenta como gasto (`R-01` de MVP-399).
builder.Services.AddScoped<DashboardEconomicsService>();
// Diario cronológico unificado (MVP-305): agrega las cuatro entidades operativas, de solo lectura.
// MVP-401 enciende la cosecha, que es lo que completa RN-033 (hallazgo `G-4`).
// MVP-506 mueve la mezcla a SQL con su propio repositorio: paginar sobre cuatro listas ya
// materializadas no es paginar (`P-051`).
builder.Services.AddScoped<IDiaryRepository, DiaryRepository>();
builder.Services.AddScoped<DiaryQueryService>();
builder.Services.AddScoped<ListWorkspacePeopleHandler>();
builder.Services.AddScoped<RevokeMemberHandler>();
// Ciclo de vida del Workspace (MVP-206): renombrar, baja lógica, traspaso y reactivación
builder.Services.AddScoped<IWorkspaceReactivationRequestRepository, WorkspaceReactivationRequestRepository>();
builder.Services.AddScoped<RenameWorkspaceHandler>();
builder.Services.AddScoped<GetWorkspaceClosureOptionsHandler>();
builder.Services.AddScoped<CloseWorkspaceHandler>();
builder.Services.AddScoped<TransferWorkspaceOwnershipHandler>();
builder.Services.AddScoped<LeaveWorkspaceHandler>();
builder.Services.AddScoped<WorkspaceOwnershipGuard>();
builder.Services.AddScoped<PreviewReactivationHandler>();
builder.Services.AddScoped<RequestReactivationHandler>();
builder.Services.AddScoped<ListReactivationRequestsHandler>();
builder.Services.AddScoped<ResolveReactivationHandler>();
builder.Services.AddScoped<ReopenWorkspaceHandler>();
// Baja de cuenta y politica de retencion (MVP-505): el derecho de supresion, que reutiliza la
// guarda de no-orfandad de MVP-206 en vez de reimplementarla (RN-038, CA-4).
// La CSP del cliente se lee una sola vez del fichero que emite su build (`csp.policy`), en vez de
// reescribirla en C#: el backend no conoce el origen que el build inyecta en `connect-src`.
builder.Services.AddSingleton(sp =>
    SpaContentSecurityPolicy.FromWebRoot(sp.GetRequiredService<IWebHostEnvironment>()));
builder.Services.AddSingleton<AccountRetentionPolicy>();
builder.Services.AddScoped<CloseAccountHandler>();
// MVP-504 (B-3): quien **ejecuta** RN-041. Hasta aqui el plazo estaba declarado en tres sitios y no
// lo aplicaba nadie, asi que la fecha de purga que devuelve la baja de cuenta no llegaba nunca.
builder.Services.AddScoped<RetentionPurgeService>();
builder.Services.AddHostedService<RetentionPurgeWorker>();
builder.Services.AddScoped<IWorkspaceInvitationRepository, WorkspaceInvitationRepository>();
builder.Services.AddScoped<InvitationTokenService>();
builder.Services.AddScoped<IInvitationTokenService>(sp => sp.GetRequiredService<InvitationTokenService>());
// Mismo esquema de token para los dos enlaces de un solo uso del producto: invitación y reactivación.
builder.Services.AddScoped<IOneTimeTokenService>(sp => sp.GetRequiredService<InvitationTokenService>());
builder.Services.AddScoped<SmtpMailer>();
// MVP-715 — La única forma de componer un correo del producto. Singleton: no tiene estado, solo
// opciones, y así ningún emisor puede acabar con una plantilla distinta.
builder.Services.AddSingleton<ProductEmailTemplate>();
// MVP-711 — Canal de feedback. El limitador es **singleton**: su cuenta es por usuario y tiene que
// sobrevivir a la petición, igual que el estado de las alertas (`AlertStateStore`).
builder.Services.AddSingleton<FeedbackRateLimiter>();
builder.Services.AddScoped<IFeedbackEmailSender, SmtpFeedbackEmailSender>();
builder.Services.AddScoped<SubmitFeedbackHandler>();
builder.Services.AddScoped<IInvitationEmailSender, SmtpInvitationEmailSender>();
builder.Services.AddScoped<IWorkspaceLifecycleEmailSender, SmtpWorkspaceLifecycleEmailSender>();
builder.Services.AddScoped<CreateInvitationHandler>();
builder.Services.AddScoped<ListWorkspaceInvitationsHandler>();
builder.Services.AddScoped<ResendInvitationHandler>();
builder.Services.AddScoped<CancelInvitationHandler>();
builder.Services.AddScoped<PreviewInvitationHandler>();
builder.Services.AddScoped<AcceptInvitationHandler>();
builder.Services.AddScoped<RejectInvitationHandler>();
builder.Services.AddScoped<ListReceivedInvitationsHandler>();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .GetChildren()
            .Select(c => c.Value ?? string.Empty)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();
        if (allowedOrigins.Length == 0) allowedOrigins = ["http://localhost:5173"];

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // MVP-711 — `AllowAnyHeader` es de **petición**: las de respuesta hay que exponerlas una
            // a una o el navegador no deja leerlas. El canal de feedback adjunta el `X-Request-Id`
            // de la última petición fallida, y sin esto ese dato sería `null` en cualquier despliegue
            // con front y API en orígenes distintos (que es el de desarrollo: 5173 contra 5127).
            .WithExposedHeaders(RequestIdMiddleware.HeaderName)
            .AllowCredentials();
    });
});

// ── Controllers & OpenAPI ────────────────────────────────────────────────────
// El filtro de excepción traduce el rechazo de scope de Workspace (dominio) a 403 uniforme (MVP-105).
builder.Services.AddControllers(options =>
{
    options.Filters.Add<WorkspaceAccessExceptionFilter>();
    // MVP-806 — Lo que impide depurar un maestro se traduce igual en los cuatro: 422 con su código de
    // regla, 400 si la ficha del cuerpo no existe y 409 si la fusión pisó una edición ajena.
    options.Filters.Add<MasterDepurationExceptionFilter>();
    // Un cuerpo que el cliente envió mal codificado es un 400, no un 500 (MVP-502, P-027).
    options.Filters.Add<InvalidRequestBodyFilter>();
});
builder.Services.AddOpenApi();

// Los errores de validación de modelo deben respetar el contrato { error: { code, message } }
// definido en docs/02-arquitectura/contratos-api.md, en lugar del ProblemDetails por defecto.
// La traducción vive en ModelStateErrorTranslator (MVP-502, P-043): emite el código de dominio que
// declara cada anotación y no deja salir a la UI los mensajes en inglés del binder.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
        new BadRequestObjectResult(new ApiErrorResponse(
            ModelStateErrorTranslator.Translate(context.ModelState)));
});

var app = builder.Build();

var emailOptions = builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new();

// Sin cuenta de envío las invitaciones por email no salen: la API lo dice con email_sent=false,
// pero conviene verlo también al arrancar el entorno.
if (!emailOptions.IsConfigured)
    app.Logger.LogWarning(
        "Sin cuenta de envío de email configurada ('Email:Host' y 'Email:FromAddress'). "
        + "Las invitaciones se emiten pero deben compartirse por enlace.");

// P-100 — Mismo criterio que el aviso de arriba, y por un motivo peor: un modo de seguridad mal
// escrito no deja el envío sin hacer, lo hace por un transporte distinto del que se pidió. Falla del
// lado seguro (StartTLS), pero en silencio, y el síntoma llega el día de la primera entrega fallida.
if (!emailOptions.IsSecurityModeKnown)
    app.Logger.LogWarning(
        "Modo de seguridad de email no reconocido ('Email:SecurityMode' = «{SecurityMode}»): se "
        + "conectará con «{AppliedMode}». Valores admitidos: {KnownModes}.",
        emailOptions.SecurityMode,
        EmailSecurityModes.StartTls,
        string.Join(", ", EmailSecurityModes.All));

// MVP-603 — Una vigilancia encendida sin destinatario es el peor estado posible: parece que hay
// alertas, y lo que hay es una anotación en un log que nadie lee. Igual que el aviso de arriba, se
// dice al arrancar en lugar de descubrirse el día del incidente.
var opsConfigurados = builder.Configuration.GetSection(OpsOptions.SectionName).Get<OpsOptions>() ?? new();

if (opsConfigurados.AlertsEnabled && string.IsNullOrWhiteSpace(opsConfigurados.AlertEmail))
    app.Logger.LogWarning(
        "Vigilancia de alertas activa sin destinatario ('Ops:AlertEmail'). "
        + "Las alertas solo quedarán en la traza: nadie recibirá aviso.");

// MKT-101 — Mismo criterio que el aviso de arriba: sin destinatario, el resumen periódico no se puede
// entregar y conviene saberlo al arrancar, no el día que se eche en falta.
if (opsConfigurados.SummaryEnabled && string.IsNullOrWhiteSpace(opsConfigurados.AlertEmail))
    app.Logger.LogWarning(
        "Resumen operativo activo sin destinatario ('Ops:AlertEmail'). "
        + "No se enviará ningún resumen diario ni semanal.");

// MVP-711 — Mismo criterio que los dos avisos de arriba: el destinatario del canal de feedback es un
// secreto de despliegue (el repositorio es público), así que lo normal en una máquina de trabajo es
// que falte. Lo que no puede pasar es que falte en producción sin que nadie se entere: sin buzón, la
// aplicación ofrece un canal que responde «no disponible» a quien intenta usarlo.
if (!(builder.Configuration.GetSection(FeedbackOptions.SectionName).Get<FeedbackOptions>() ?? new()).IsConfigured)
    app.Logger.LogWarning(
        "Sin buzón del canal de feedback ('Feedback:Recipient'). "
        + "«Sugerencias e incidencias» responderá que el canal no está disponible.");

if (!opsConfigurados.IsSignalsEndpointEnabled)
    app.Logger.LogWarning(
        "Sin llave de operación ('Ops:ApiKey'): 'GET /api/v1/ops/signals' responderá 404 "
        + "y la revisión operativa no se podrá consultar.");

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// PLT-101 — Antes que nada: un dominio comprado solo para no perderlo (terrenario.com/.es y sus
// www) no necesita traza, métricas ni CORS propios, solo la redirección permanente al canonico.
app.UseMiddleware<AlternateDomainRedirectMiddleware>();

// Transversales primero, para que cubran también respuestas de error y redirecciones (MVP-105).
app.UseMiddleware<RequestIdMiddleware>();       // X-Request-Id + scope de logging (P-006)
app.UseMiddleware<RequestMetricsMiddleware>();  // Peticiones, 5xx y latencia P95 (MVP-603)
app.UseMiddleware<SecurityHeadersMiddleware>(); // Headers de seguridad HTTP (P-005)

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");

// ── El cliente, servido por la propia API ─────────────────────────────────────
//
// Un solo origen, y no por comodidad: Azure Static Web Apps **no tiene región europea disponible**
// para altas nuevas, y servir el cliente desde EE. UU. haría falsas dos frases de la Política de
// Privacidad ya publicada. Sirviéndolo desde aquí, todo queda en Spain Central.
//
// De regalo desaparecen dos problemas: no hay CORS que configurar, y la cookie de refresco
// `SameSite=Strict` deja de estar en riesgo, porque ya no hay nada cross-site.

// MKT-102 — La home pública (`/`) es una landing pre-renderizada propia (`home.html`), no el
// `index.html` que `MapFallback` sirve para el resto de rutas de la SPA (`/app/diario` incluida).
// React arranca con `createRoot(...).render(...)` — reemplaza `#root`, no lo hidrata—, así que si la
// home se sirviera desde `index.html` cada ruta autenticada mostraría un parpadeo de contenido de
// marketing antes de que React lo sustituyera. Middleware explícito y no un cambio en
// `UseDefaultFiles`: si `home.html` no existe —build de frontend no ejecutado, típico en
// desarrollo— cae al comportamiento de siempre sin romper nada (`ADR-0012`).
app.Use(async (context, next) =>
{
    if (HttpMethods.IsGet(context.Request.Method) && context.Request.Path == "/")
    {
        var home = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "home.html");
        if (File.Exists(home))
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(home);
            return;
        }
    }

    await next();
});

// MKT-106 (CA-1) — Cuenta la visita antes de que `UseStaticFiles` sirva la landing; no toca la
// respuesta, solo suma un contador.
app.UseMiddleware<LandingViewMiddleware>();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Las rutas del SPA (`/app/diario`, `/legal/privacidad`) no existen como fichero: solo viven en el
// router del cliente. Sin esto, entrar directo o recargar da 404.
//
// `/api` se excluye a propósito: devolver `index.html` con un 200 ante un endpoint inexistente
// convertiría un error de integración en una respuesta aparentemente válida, que es de las cosas más
// caras de diagnosticar.
app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        // MVP-811 (`P-117`) — `contratos-api.md` dice que las respuestas de error son **siempre** JSON
        // con `{ error: { code, message } }`. Este borde respondía `404` con el cuerpo vacío y sin
        // `Content-Type`, así que un cliente que lee el error para saber qué ha pasado se encontraba
        // nada. Los 404 de dominio sí cumplían: la excepción era el transporte, igual que `P-027` y
        // `P-043`, que `MVP-502` cerró en este mismo borde.
        //
        // Aquí caen tres cosas a la vez, y las tres se benefician del mismo envoltorio: una ruta que no
        // existe, un **método no permitido** sobre una que sí (`DELETE /api/v1/seasons`) y un parámetro
        // de ruta que no cumple su restricción (`/api/v1/plots/no-es-un-guid`). Las tres siguen
        // respondiendo `404` —no se introduce un `405` que el contrato no declara—: lo que cambia es
        // que ahora **dicen** algo.
        //
        // Se escribe a mano y no con un `Results`: aquí no hay endpoint, así que no hay resultado de
        // acción que ejecutar. El envoltorio es el mismo tipo que usan los controladores, así que el
        // contrato no se duplica.
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(
            new ApiErrorResponse(new ApiError(
                ErrorCodes.ResourceNotFound,
                "El recurso solicitado no existe en esta API.")),
            context.RequestAborted);
        return;
    }

    var indice = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "index.html");
    if (!File.Exists(indice))
    {
        // API desplegada sin cliente: es un estado legítimo en desarrollo, y decirlo es mejor que
        // servir un 404 sin explicación.
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(indice);
});

// ── Migraciones al arrancar ───────────────────────────────────────────────────
//
// Hasta la primera publicación esto solo corría en Development, así que un despliegue real habría
// arrancado contra una base **vacía**: la aplicación levantaba y fallaba en la primera consulta.
//
// Se activa en todos los entornos, con interruptor para poder apagarlo. Es la opción simple y en
// este producto es segura porque la API corre en **una sola instancia**; si algún día escala, dos
// réplicas migrando a la vez es un problema real y habrá que mover esto al pipeline.
//
// Que una migración fallida impida arrancar es **deliberado**: es preferible a servir peticiones
// contra un esquema que no es el que el código espera.
if (builder.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TerrenarioDbContext>();
    var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();

    if (pending.Length > 0)
    {
        app.Logger.LogInformation(
            "Aplicando {Count} migraciones pendientes: {Migrations}", pending.Length, string.Join(", ", pending));
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Migraciones aplicadas.");
    }
}

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
