using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-804 (<c>RU-21</c>, <c>P-113</c>) — <b>Autoría de los registros operativos</b>, contra la API y
/// la base de datos reales.
///
/// Los repositorios van mockeados en los tests de handler, así que un <c>JOIN</c> a <c>users</c> que EF
/// no supiera traducir pasaría inadvertido allí y reventaría en producción: es exactamente
/// <c>P-014</c>. Estos tests ejercitan el SQL de verdad.
///
/// El test con filo es <see cref="Deberia_MostrarCuentaEliminada_YNoFiltrarNombreNiCorreo_Cuando_LaCuentaSeDioDeBaja"/>:
/// una funcionalidad de <b>lectura</b> nueva sobre un dato que ya estaba escrito es justo por donde se
/// escapa un dato personal que la baja de cuenta había borrado (<c>CA-3</c>). Por eso no comprueba el
/// código de respuesta ni el rótulo: comprueba que el <b>cuerpo entero</b> no contiene ni el nombre ni
/// el correo que la cuenta tuvo.
/// </summary>
public sealed class RecordAuthorshipTests : IAsyncLifetime
{
    private const string NombreDeAndres = "Andrés Gilabert";
    private const string NombreDeLucia = "Lucía Pérez";
    private const string CorreoDeLucia = "lucia@ejemplo.test";
    private const string CuentaEliminada = "Cuenta eliminada";

    private readonly TerrenarioApiFactory _factory = new();

    private ApiSession _andres = null!;
    private Guid _seasonId;
    private Guid _plotId;
    private Guid _workerId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _factory.Google
            .WithIdentity("codigo-andres", "sub-andres", NombreDeAndres, "andres@ejemplo.test")
            .WithIdentity("codigo-lucia", "sub-lucia", NombreDeLucia, CorreoDeLucia);

        _andres = await ApiSession.LoginAsync(_factory, "codigo-andres");
        await _andres.CreateWorkspaceAsync("Finca El Olivar");

        _seasonId = (await _andres.PostJsonAsync("/api/v1/seasons", new
        {
            name = "Campaña 2025/26",
            start_date = "2025-10-01",
            end_date = "2026-03-31"
        })).GetProperty("id").GetGuid();

        _plotId = (await _andres.PostJsonAsync("/api/v1/plots", new
        {
            name = "Matorral",
            ownership_type = "propia"
        })).GetProperty("id").GetGuid();

        _workerId = (await _andres.PostJsonAsync("/api/v1/workers", new
        {
            name = "Antonio Ruiz",
            hourly_rate = 12.5m
        })).GetProperty("id").GetGuid();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private Task<JsonElement> CrearActividadAsync() => _andres.PostJsonAsync("/api/v1/activities", new
    {
        date = "2025-11-12",
        plot_id = _plotId,
        season_id = _seasonId,
        worker_id = _workerId,
        task_text = "Poda de formación",
        hours = 6m,
        manual_cost = 75m
    });

    private Task<JsonElement> CrearCosechaAsync() => _andres.PostJsonAsync("/api/v1/harvests", new
    {
        date = "2025-12-05",
        plot_id = _plotId,
        season_id = _seasonId,
        product = "aceituna_olivar",
        kgs = 1_000m,
        destination = "aceite_para_venta"
    });

    private Task<JsonElement> CrearCompraAsync() => _andres.PostJsonAsync("/api/v1/purchases", new
    {
        purchase_date = "2025-11-02",
        product = "Abono foliar",
        season_id = _seasonId,
        total_quantity = 200m,
        total_cost = 400m
    });

    private async Task<JsonElement> CrearConsumoAsync()
    {
        var compra = await CrearCompraAsync();
        return await _andres.PostJsonAsync(
            $"/api/v1/purchases/{compra.GetProperty("id").GetGuid()}/consumptions",
            new { date = "2025-11-08", plot_id = _plotId, quantity = 50m });
    }

    /// <summary>Mete a Lucía en el Workspace de Andrés por el flujo real de invitación (MVP-204).</summary>
    private async Task<ApiSession> InvitarALuciaAsync()
    {
        await _andres.PostJsonAsync("/api/v1/workspaces/invitations", new
        {
            channel = "email",
            email = CorreoDeLucia
        });

        var lucia = await ApiSession.LoginAsync(_factory, "codigo-lucia");
        var recibidas = await lucia.GetJsonAsync("/api/v1/invitations/received");
        var invitacionId = recibidas.GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();
        (await lucia.PostAsync($"/api/v1/invitations/received/{invitacionId}/accept", null))
            .EnsureSuccessStatusCode();

        return lucia;
    }

    private static string NombreDeAlta(JsonElement recurso) => recurso.GetProperty("created_by_name").GetString()!;

    private static string NombreDeEdicion(JsonElement recurso) => recurso.GetProperty("updated_by_name").GetString()!;

    [Fact]
    public async Task Deberia_DecirQuienLoApunto_EnLosCuatroTiposDeRegistro()
    {
        // CA-1 — Las cuatro tablas operativas guardan `created_by` desde que se crearon; lo que no
        // había era forma de leerlo. Se comprueban las cuatro porque el `JOIN` es de cada repositorio.
        var actividad = await CrearActividadAsync();
        var cosecha = await CrearCosechaAsync();
        var consumo = await CrearConsumoAsync();

        // La compra se relee del listado: no tiene lectura por id, así que su autoría solo puede venir
        // de la proyección que comparte con el listado.
        var compra = (await _andres.GetJsonAsync("/api/v1/purchases?season_id=all"))
            .GetProperty("data").EnumerateArray().Single();

        foreach (var recurso in new[] { actividad, cosecha, compra, consumo })
        {
            NombreDeAlta(recurso).Should().Be(NombreDeAndres);
            // Sin edición posterior, la última edición es la propia alta.
            NombreDeEdicion(recurso).Should().Be(NombreDeAndres);
            recurso.GetProperty("created_at").GetDateTimeOffset()
                .Should().Be(recurso.GetProperty("updated_at").GetDateTimeOffset());
        }
    }

    [Fact]
    public async Task Deberia_LlegarLaAutoria_TambienPorLaLecturaDeUnSoloRegistro()
    {
        // El modal de corrección del diario no usa la fila del muro: pide el registro completo por id.
        // Si la autoría no viajara por ese camino, el diario sería la única vista sin ella.
        var actividadId = (await CrearActividadAsync()).GetProperty("id").GetGuid();
        var cosechaId = (await CrearCosechaAsync()).GetProperty("id").GetGuid();

        NombreDeAlta(await _andres.GetJsonAsync($"/api/v1/activities/{actividadId}"))
            .Should().Be(NombreDeAndres);
        NombreDeAlta(await _andres.GetJsonAsync($"/api/v1/harvests/{cosechaId}"))
            .Should().Be(NombreDeAndres);
    }

    [Fact]
    public async Task Deberia_SepararQuienApuntoDeQuienCorrigio_Cuando_LaEditaOtroMiembro()
    {
        // RN-034 — los permisos son planos: Lucía puede corregir lo que apuntó Andrés. Es justo el caso
        // que motiva la historia, y el que exige que los dos nombres sean campos distintos.
        var cosecha = await CrearCosechaAsync();
        var cosechaId = cosecha.GetProperty("id").GetGuid();
        var version = cosecha.GetProperty("version").GetInt32();

        var lucia = await InvitarALuciaAsync();
        var corregida = await lucia.PatchAsync($"/api/v1/harvests/{cosechaId}", new { kgs = 1_250m }, version);
        corregida.EnsureSuccessStatusCode();

        var releida = await _andres.GetJsonAsync($"/api/v1/harvests/{cosechaId}");

        NombreDeAlta(releida).Should().Be(NombreDeAndres);
        NombreDeEdicion(releida).Should().Be(NombreDeLucia);
        // Y la fecha de edición avanza: es lo que permite a la interfaz saber que hubo una corrección
        // posterior al alta y no repetir el mismo nombre dos veces (CA-2).
        releida.GetProperty("updated_at").GetDateTimeOffset()
            .Should().BeAfter(releida.GetProperty("created_at").GetDateTimeOffset());
    }

    [Fact]
    public async Task Deberia_MostrarCuentaEliminada_YNoFiltrarNombreNiCorreo_Cuando_LaCuentaSeDioDeBaja()
    {
        // CA-3 — El caso con filo. Lucía corrige una cosecha y **después** se da de baja: la fila de su
        // cuenta sobrevive anonimizada porque el histórico operativo de terceros guarda quién lo
        // registró (MVP-505), y esta lectura nueva no puede resucitar lo que la baja borró.
        var cosecha = await CrearCosechaAsync();
        var cosechaId = cosecha.GetProperty("id").GetGuid();

        var lucia = await InvitarALuciaAsync();
        (await lucia.PatchAsync(
            $"/api/v1/harvests/{cosechaId}", new { kgs = 1_250m }, cosecha.GetProperty("version").GetInt32()))
            .EnsureSuccessStatusCode();

        (await lucia.PostAsync("/api/v1/account/closure", new { confirmation = "ELIMINAR MI CUENTA" }))
            .EnsureSuccessStatusCode();

        var respuesta = await _andres.GetAsync($"/api/v1/harvests/{cosechaId}");
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        var releida = JsonDocument.Parse(cuerpo).RootElement;

        NombreDeEdicion(releida).Should().Be(CuentaEliminada);
        // Quien la apuntó sigue vivo: la baja de una cuenta no puede borrar la autoría de otra.
        NombreDeAlta(releida).Should().Be(NombreDeAndres);

        // Lo que de verdad se comprueba: **nada** del cuerpo la nombra. No basta con mirar los dos
        // campos de autoría, porque una fuga entraría por cualquier otro.
        cuerpo.Should().NotContain(NombreDeLucia);
        cuerpo.Should().NotContain(CorreoDeLucia);
        cuerpo.Should().NotContain("lucia");
    }

    [Fact]
    public async Task Deberia_MostrarCuentaEliminada_TambienEnElListado()
    {
        // La misma proyección alimenta la lectura por id y el listado, pero el listado es el que se pide
        // sin querer: si la anonimización se aplicara solo en una de las dos, la fuga saldría por aquí.
        var cosecha = await CrearCosechaAsync();

        var lucia = await InvitarALuciaAsync();
        (await lucia.PatchAsync(
            $"/api/v1/harvests/{cosecha.GetProperty("id").GetGuid()}",
            new { kgs = 1_250m },
            cosecha.GetProperty("version").GetInt32()))
            .EnsureSuccessStatusCode();
        (await lucia.PostAsync("/api/v1/account/closure", new { confirmation = "ELIMINAR MI CUENTA" }))
            .EnsureSuccessStatusCode();

        var listado = await (await _andres.GetAsync("/api/v1/harvests?season_id=all")).Content.ReadAsStringAsync();

        listado.Should().Contain(CuentaEliminada);
        listado.Should().NotContain(NombreDeLucia);
        listado.Should().NotContain(CorreoDeLucia);
    }

    [Fact]
    public async Task NoDeberia_LlevarLaAutoria_AlMuroDelDiario()
    {
        // CA-4 — El diario es la lista más densa del producto y su modal ya pide el registro por id, así
        // que la autoría no tiene nada que hacer en el muro. Su proyección es propia (`DiaryRow`), y
        // este test es lo que impide que alguien la añada «ya que estamos».
        await CrearActividadAsync();
        await CrearCosechaAsync();

        var diario = await (await _andres.GetAsync("/api/v1/diary")).Content.ReadAsStringAsync();

        diario.Should().NotContain("created_by_name");
        diario.Should().NotContain("updated_by_name");
    }

    [Fact]
    public async Task Deberia_SeguirDevolviendoElRegistro_Cuando_LaCuentaQueLoApuntoYaNoExiste()
    {
        // Las tablas operativas **no tienen FK** hacia `users` y la purga de RN-041 no las mira, así que
        // `created_by` puede acabar apuntando a una fila que ya no está. Con un `INNER JOIN` la cosecha
        // desaparecería del listado: perder el dato de apoyo es aceptable, perder la partida no.
        var cosechaId = (await CrearCosechaAsync()).GetProperty("id").GetGuid();

        // Se simula la purga dejando la referencia colgando, que es como queda la fila cuando la cuenta
        // ya no está. Escribirlo por SQL es deliberado: el agregado no permite falsear su autoría.
        await using (var db = _factory.CreateDbContext())
        {
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE harvests SET created_by = '11111111-1111-1111-1111-111111111111', " +
                "updated_by = '11111111-1111-1111-1111-111111111111';");
        }

        var releida = await _andres.GetJsonAsync($"/api/v1/harvests/{cosechaId}");

        NombreDeAlta(releida).Should().Be(CuentaEliminada);
        NombreDeEdicion(releida).Should().Be(CuentaEliminada);
        releida.GetProperty("kgs").GetDecimal().Should().Be(1_000m);
    }
}
