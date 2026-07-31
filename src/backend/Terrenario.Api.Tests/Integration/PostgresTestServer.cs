using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terrenario.Api.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-501 (2ª pasada) — Servidor PostgreSQL de pruebas, compartido por todo el proyecto de tests.
///
/// Cierra <c>P-031</c>. El arnés anterior usaba SQLite, y eso obligaba a **degradar consultas de
/// producción** para que los tests pudieran ejecutarlas: ordenar en memoria lo que la base de datos
/// sabe ordenar, porque EF+SQLite no traduce <c>ORDER BY</c> sobre <c>DateTimeOffset</c>. Ese punto
/// lo describía como lo que era —el test moldeando el código de producción— y pedía un criterio
/// único. El criterio es este: **los tests que ejercitan SQL corren contra el motor real**.
///
/// Un solo contenedor por ejecución y una **base de datos por clase de test**: las clases siguen
/// corriendo en paralelo (xUnit paraleliza por colección) sin pisarse los datos, y levantar el
/// contenedor —que es lo caro— se hace una vez.
///
/// El esquema se crea aplicando las **migraciones reales**, no <c>EnsureCreated</c>. Cuesta un poco
/// más y compensa: valida de paso que las migraciones aplican limpias, que es algo que SQLite no
/// podía comprobar de ninguna manera.
///
/// <b>Requiere Docker</b>. Es la contrapartida aceptada al cerrar <c>P-031</c> y queda recogida en
/// <c>docs/04-ingenieria/estrategia-testing.md</c>.
/// </summary>
public static class PostgresTestServer
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static PostgreSqlContainer? _container;

    /// <summary>
    /// Crea una base de datos vacía con el esquema al día y devuelve su cadena de conexión.
    /// El contenedor se levanta la primera vez que alguien la pide.
    /// </summary>
    public static async Task<string> CreateDatabaseAsync()
    {
        var container = await EnsureContainerAsync();
        var databaseName = $"terrenario_test_{Guid.NewGuid():N}";

        await using (var admin = new NpgsqlConnection(container.GetConnectionString()))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\";", admin);
            await create.ExecuteNonQueryAsync();
        }

        // Pool acotado: cada clase de test necesita muy pocas conexiones, pero hay muchas clases a la
        // vez. Con el pool por defecto (100 por origen) las últimas en arrancar se topaban con
        // «sorry, too many clients already» y fallaban en `InitializeAsync`, que se lee como un fallo
        // del test cuando en realidad es del arnés.
        var connectionString = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = databaseName,
            MaxPoolSize = 4,
            MinPoolSize = 0
        }.ConnectionString;

        await using (var db = CreateDbContext(connectionString))
            await db.Database.MigrateAsync();

        return connectionString;
    }

    /// <summary>Contexto contra una base ya preparada. Lo usan tanto los tests como el arnés de API.</summary>
    public static TerrenarioDbContext CreateDbContext(string connectionString)
        => new(new DbContextOptionsBuilder<TerrenarioDbContext>().UseNpgsql(connectionString).Options);

    private static async Task<PostgreSqlContainer> EnsureContainerAsync()
    {
        if (_container is not null) return _container;

        await Gate.WaitAsync();
        try
        {
            if (_container is null)
            {
                // Misma familia que el entorno de desarrollo (`terrenario-pg`, postgres:15): probar
                // contra otra versión mayor haría el arnés menos representativo, no más.
                var container = new PostgreSqlBuilder("postgres:15-alpine")
                    .WithDatabase("terrenario_test")
                    .WithUsername("terrenario_test")
                    .WithPassword("terrenario_test")
                    // Muchas clases de test en paralelo, cada una con su base y su pool: el límite
                    // por defecto (100) se agota antes de que arranquen todas.
                    .WithCommand("-c", "max_connections=400")
                    .Build();

                await container.StartAsync();
                _container = container;
            }
        }
        finally
        {
            Gate.Release();
        }

        return _container;
    }
}
