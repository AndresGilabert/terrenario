using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Cryptography;
using Terrenario.Api.Infrastructure.Auth;
using Terrenario.Api.Infrastructure.Data;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-501 — Arnés de <b>integración y smoke E2E de API</b> (CA-2/CA-3).
///
/// Levanta la aplicación real —el mismo <c>Program.cs</c>, con sus filtros, su autenticación JWT, su
/// pipeline de middlewares y sus controladores— contra una base de datos <b>PostgreSQL real</b>
/// (<see cref="PostgresTestServer"/>), propia de cada clase de test. Es la diferencia con los tests
/// de handler que ya existían: allí los repositorios van mockeados y por eso pasaron 130 tests con
/// <c>GET /workspaces</c> devolviendo 500 (<c>P-014</c>); aquí la consulta se traduce al SQL de
/// producción y el fallo sale a la primera.
///
/// Lo único que se sustituye es <b>Google</b> (<see cref="IGoogleOidcService"/>): es un proveedor
/// externo, no se puede automatizar su consentimiento y no aporta nada probarlo aquí. Todo lo demás
/// del login —creación de usuario, emisión del JWT, cookie de refresco, resolución de Workspace— sí
/// se ejercita.
///
/// Las claves RS256 se generan por proceso: el arnés no puede depender de secretos de la máquina.
/// </summary>
public sealed class TerrenarioApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private string _connectionString = string.Empty;

    /// <summary>Identidad que devolverá el doble de Google en el siguiente intercambio de código.</summary>
    public FakeGoogleOidcService Google { get; } = new();

    private static readonly Lazy<(string Private, string Public)> KeyPair = new(() =>
    {
        using var rsa = RSA.Create(2048);
        return (rsa.ExportPkcs8PrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());
    });

    /// <summary>Prepara la base de datos antes de que arranque el host. Lo llama xUnit.</summary>
    public async Task InitializeAsync()
    {
        _connectionString = await PostgresTestServer.CreateDatabaseAsync();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // «Testing» y no «Development»: en Development el arranque vuelve a aplicar migraciones
        // (`Program.cs`), que el arnés ya dejó aplicadas al crear la base.
        builder.UseEnvironment("Testing");

        // `UseSetting` entra en la configuración del host, que es la que lee `Program.cs` al construir
        // la clave de validación del JWT antes de que exista el contenedor de servicios.
        builder.UseSetting("Auth:Jwt:PrivateKeyPem", KeyPair.Value.Private);
        builder.UseSetting("Auth:Jwt:PublicKeyPem", KeyPair.Value.Public);
        builder.UseSetting("Auth:Jwt:Issuer", "terrenario-api");
        builder.UseSetting("Auth:Jwt:Audience", "terrenario-web");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Invitations:BaseUrl", "https://terrenario.test");
        builder.UseSetting("Workspaces:BaseUrl", "https://terrenario.test");

        // MVP-504 (B-3) — La rutina de expurgo se apaga en el arnés: un proceso que borra filas por
        // su cuenta mientras se ejercitan otros casos convierte cualquier fallo en irreproducible.
        // Sus propios tests la invocan directamente, que además es la única forma de controlar el
        // instante y no tener que esperar 24 meses.
        builder.UseSetting("Retention:Enabled", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGoogleOidcService>();
            services.AddSingleton<IGoogleOidcService>(Google);
        });
    }

    /// <summary>Abre un contexto contra la misma base que usa la API, para sembrar o comprobar datos.</summary>
    public TerrenarioDbContext CreateDbContext() => PostgresTestServer.CreateDbContext(_connectionString);

    /// <summary>
    /// Cliente HTTP que <b>no sigue redirecciones</b>: un 302 inesperado debe fallar el test, no
    /// disimularse siguiéndolo.
    /// </summary>
    public HttpClient CreateApiClient()
        => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}

/// <summary>
/// Doble del proveedor de identidad. Devuelve la identidad que el test haya preparado, o falla como
/// falla Google cuando el código de autorización no sirve.
/// </summary>
public sealed class FakeGoogleOidcService : IGoogleOidcService
{
    private readonly Dictionary<string, GoogleIdentity> _identities = new(StringComparer.Ordinal);

    /// <summary>Asocia un código de autorización a la identidad que Google devolvería por él.</summary>
    public FakeGoogleOidcService WithIdentity(string code, string sub, string displayName, string email)
    {
        _identities[code] = new GoogleIdentity(sub, displayName, email);
        return this;
    }

    public Task<GoogleIdentity> ExchangeCodeAsync(
        string code,
        string redirectUri,
        string codeVerifier,
        CancellationToken ct = default)
        => _identities.TryGetValue(code, out var identity)
            ? Task.FromResult(identity)
            : throw new GoogleOidcException(
                "Código de autorización no válido.",
                Terrenario.Api.Common.Errors.ErrorCodes.AuthGoogleTokenInvalid);
}
