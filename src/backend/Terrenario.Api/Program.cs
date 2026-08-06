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
using Terrenario.Api.Application.Diary;
using Terrenario.Api.Application.Harvests;
using Terrenario.Api.Application.Invitations;
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
using Terrenario.Api.Infrastructure.Invitations;
using Terrenario.Api.Infrastructure.Telemetry;
using Terrenario.Api.Infrastructure.Telemetry.Alerts;
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
builder.Services.AddScoped<ITelemetryCounterStore, TelemetryCounterStore>();
builder.Services.AddHostedService<TelemetryFlushWorker>();
// Salud y vigilancia (MVP-603). El estado de las alertas es singleton: la vigilancia lo escribe y la
// revisión operativa lo lee.
builder.Services.AddScoped<HealthProbe>();
builder.Services.AddScoped<IAlertNotifier, AlertNotifier>();
builder.Services.AddSingleton<AlertStateStore>();
builder.Services.AddHostedService<AlertMonitor>();
builder.Services.AddScoped<OperationalSignalsService>();
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
builder.Services.AddScoped<ListPurchaseProductsHandler>();
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
// Dashboard (MVP-403): agrega la producción capturada; solo lectura y sin refresco continuo (RN-006)
builder.Services.AddScoped<DashboardScopeResolver>();
builder.Services.AddScoped<DashboardQueryService>();
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
            .AllowCredentials();
    });
});

// ── Controllers & OpenAPI ────────────────────────────────────────────────────
// El filtro de excepción traduce el rechazo de scope de Workspace (dominio) a 403 uniforme (MVP-105).
builder.Services.AddControllers(options =>
{
    options.Filters.Add<WorkspaceAccessExceptionFilter>();
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

// Sin cuenta de envío las invitaciones por email no salen: la API lo dice con email_sent=false,
// pero conviene verlo también al arrancar el entorno.
if (!(builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new()).IsConfigured)
    app.Logger.LogWarning(
        "Sin cuenta de envío de email configurada ('Email:Host' y 'Email:FromAddress'). "
        + "Las invitaciones se emiten pero deben compartirse por enlace.");

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

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
        context.Response.StatusCode = StatusCodes.Status404NotFound;
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
