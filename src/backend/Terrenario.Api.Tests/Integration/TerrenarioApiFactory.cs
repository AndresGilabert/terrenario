using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
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
/// pipeline de middlewares y sus controladores— contra una base de datos SQLite propia de cada clase
/// de test. Es la diferencia con los tests de handler que ya existían: allí los repositorios van
/// mockeados y por eso pasaron 130 tests con <c>GET /workspaces</c> devolviendo 500 (`P-014`); aquí
/// la consulta se traduce a SQL de verdad y el 500 sale a la primera.
///
/// Dos cosas se sustituyen a propósito, y solo dos:
/// <list type="bullet">
/// <item><b>PostgreSQL por SQLite</b>. Mantiene el arnés sin dependencia de Docker (decisión del PO
/// en `MVP-501`). El precio está registrado: `P-031` sigue abierto —EF+SQLite no traduce
/// <c>ORDER BY</c> sobre <c>DateTimeOffset</c>— y la cobertura contra PostgreSQL real queda
/// pendiente.</item>
/// <item><b>Google</b> (<see cref="IGoogleOidcService"/>). Es un proveedor externo: no se puede
/// automatizar su consentimiento ni tiene sentido probarlo aquí. Todo lo demás del login —creación
/// de usuario, emisión del JWT, cookie de refresco, resolución de Workspace— sí se ejercita.</item>
/// </list>
///
/// Las claves RS256 se generan por proceso: el arnés no puede depender de secretos de la máquina.
/// </summary>
public sealed class TerrenarioApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"terrenario-test-{Guid.NewGuid():N}";
    private readonly SqliteConnection _keepAlive;

    /// <summary>Identidad que devolverá el doble de Google en el siguiente intercambio de código.</summary>
    public FakeGoogleOidcService Google { get; } = new();

    public TerrenarioApiFactory()
    {
        // La base en memoria de SQLite vive mientras haya una conexión abierta contra ella. Esta se
        // abre antes que nada y se cierra al final: es lo que hace que el esquema y los datos
        // sobrevivan entre peticiones dentro de un mismo test.
        _keepAlive = new SqliteConnection(ConnectionString);
        _keepAlive.Open();

        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    private static readonly Lazy<(string Private, string Public)> KeyPair = new(() =>
    {
        using var rsa = RSA.Create(2048);
        return (rsa.ExportPkcs8PrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());
    });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // «Testing» y no «Development»: en Development el arranque aplica migraciones de PostgreSQL
        // (`Program.cs`), que aquí no existen ni harían falta.
        builder.UseEnvironment("Testing");

        // `UseSetting` entra en la configuración del host, que es la que lee `Program.cs` al construir
        // la clave de validación del JWT antes de que exista el contenedor de servicios.
        builder.UseSetting("Auth:Jwt:PrivateKeyPem", KeyPair.Value.Private);
        builder.UseSetting("Auth:Jwt:PublicKeyPem", KeyPair.Value.Public);
        builder.UseSetting("Auth:Jwt:Issuer", "terrenario-api");
        builder.UseSetting("Auth:Jwt:Audience", "terrenario-web");
        builder.UseSetting("ConnectionStrings:DefaultConnection", string.Empty);
        builder.UseSetting("Invitations:BaseUrl", "https://terrenario.test");
        builder.UseSetting("Workspaces:BaseUrl", "https://terrenario.test");

        builder.ConfigureServices(services =>
        {
            // No basta con sustituir `DbContextOptions`: desde EF 9 cada `AddDbContext` deja además
            // registrada su acción de configuración (`IDbContextOptionsConfiguration<TContext>`), y
            // **todas** las registradas se aplican al mismo objeto de opciones. Sin retirar la de
            // Npgsql, el contexto acabaría con dos proveedores y EF se niega a arrancar.
            foreach (var descriptor in services
                         .Where(d => d.ServiceType.IsGenericType
                                     && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal)
                                     && d.ServiceType.GetGenericArguments()[0] == typeof(TerrenarioDbContext))
                         .ToList())
                services.Remove(descriptor);

            services.RemoveAll<DbContextOptions<TerrenarioDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<TerrenarioDbContext>(options => options.UseSqlite(ConnectionString));

            services.RemoveAll<IGoogleOidcService>();
            services.AddSingleton<IGoogleOidcService>(Google);
        });
    }

    private string ConnectionString => $"DataSource={_databaseName};Mode=Memory;Cache=Shared";

    /// <summary>Abre un contexto contra la misma base que usa la API, para sembrar o comprobar datos.</summary>
    public TerrenarioDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<TerrenarioDbContext>().UseSqlite(ConnectionString).Options);

    /// <summary>
    /// Cliente HTTP que <b>no sigue redirecciones</b>: un 302 inesperado debe fallar el test, no
    /// disimularse siguiéndolo.
    /// </summary>
    public HttpClient CreateApiClient()
        => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _keepAlive.Dispose();
    }
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
