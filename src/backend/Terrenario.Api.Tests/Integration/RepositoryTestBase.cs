using Terrenario.Api.Infrastructure.Data;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-501 (2ª pasada) — Base de los tests de repositorio, que ahora corren contra <b>PostgreSQL
/// real</b> en vez de SQLite.
///
/// El cambio cierra <c>P-031</c>. Con SQLite había consultas de producción escritas «hacia atrás»
/// —ordenando en memoria lo que la base sabe ordenar— solo porque EF+SQLite no traduce <c>ORDER BY</c>
/// sobre <c>DateTimeOffset</c>. Un test que obliga a empeorar el código que prueba deja de ser una
/// red de seguridad. Contra el motor real esa presión desaparece y, de paso, entra en cobertura todo
/// lo que SQLite nunca pudo representar: <c>timestamptz</c>, índices funcionales sobre
/// <c>lower(name)</c>, <c>jsonb</c> y las propias migraciones.
///
/// Cada clase de test recibe su <b>base de datos propia</b> sobre un contenedor compartido, así que
/// las clases siguen ejecutándose en paralelo sin pisarse los datos.
/// </summary>
public abstract class RepositoryTestBase : IAsyncLifetime
{
    private string _connectionString = string.Empty;

    /// <summary>Contexto de larga vida de la clase, para sembrar y comprobar.</summary>
    protected TerrenarioDbContext Db { get; private set; } = null!;

    /// <summary>
    /// Contexto nuevo contra la misma base. Lo usan los tests que quieren reproducir el ámbito por
    /// petición de producción y evitar artefactos del <i>identity map</i>.
    /// </summary>
    protected TerrenarioDbContext NewDb() => PostgresTestServer.CreateDbContext(_connectionString);

    public async Task InitializeAsync()
    {
        _connectionString = await PostgresTestServer.CreateDatabaseAsync();
        Db = NewDb();
    }

    public async Task DisposeAsync() => await Db.DisposeAsync();
}
