using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Terrenario.Api.Application.Auth;
using Terrenario.Api.Application.Invitations;
using Terrenario.Api.Application.Plots;
using Terrenario.Api.Application.Seasons;
using Terrenario.Api.Application.Workers;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workers;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Auth;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Infrastructure.Invitations;
using Terrenario.Api.Infrastructure.Telemetry;

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
// Maestro de trabajadores y administración de miembros (MVP-204)
builder.Services.AddScoped<IWorkerRepository, WorkerRepository>();
builder.Services.AddScoped<CreateWorkerHandler>();
builder.Services.AddScoped<UpdateWorkerHandler>();
builder.Services.AddScoped<ListWorkersHandler>();
builder.Services.AddScoped<ListWorkspacePeopleHandler>();
builder.Services.AddScoped<RevokeMemberHandler>();
builder.Services.AddScoped<IWorkspaceInvitationRepository, WorkspaceInvitationRepository>();
builder.Services.AddScoped<IInvitationTokenService, InvitationTokenService>();
builder.Services.AddScoped<IInvitationEmailSender, SmtpInvitationEmailSender>();
builder.Services.AddScoped<CreateInvitationHandler>();
builder.Services.AddScoped<ListWorkspaceInvitationsHandler>();
builder.Services.AddScoped<ResendInvitationHandler>();
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
});
builder.Services.AddOpenApi();

// Los errores de validación de modelo deben respetar el contrato { error: { code, message } }
// definido en docs/02-arquitectura/contratos-api.md, en lugar del ProblemDetails por defecto.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var firstError = context.ModelState
            .SelectMany(entry => entry.Value?.Errors ?? [])
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));

        return new BadRequestObjectResult(new ApiErrorResponse(
            ApiError.Validation(ErrorCodes.ValidationRequired, firstError ?? "Datos de entrada no válidos.")));
    };
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
