using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
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
using Terrenario.Api.Infrastructure.Tokens;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<ILoginTelemetry, LoginTelemetryService>();
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
app.UseMiddleware<SecurityHeadersMiddleware>(); // Headers de seguridad HTTP (P-005)

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Auto-migrate on startup in development ────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TerrenarioDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
