using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Terrenario.Api.Application.Auth;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Infrastructure.Auth;
using Terrenario.Api.Infrastructure.Data;
using Terrenario.Api.Infrastructure.Data.Repositories;
using Terrenario.Api.Infrastructure.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// ── Options ─────────────────────────────────────────────────────────────────
builder.Services.Configure<GoogleOidcOptions>(
    builder.Configuration.GetSection(GoogleOidcOptions.SectionName));
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<RefreshTokenOptions>(
    builder.Configuration.GetSection(RefreshTokenOptions.SectionName));

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
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

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
