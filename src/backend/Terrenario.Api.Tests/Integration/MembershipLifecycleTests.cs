using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-807 (<c>P-048</c>, <c>P-049</c>) — El ciclo de vida de la membresía por el lado que faltaba,
/// contra la API real.
///
/// Los dos puntos comparten superficie y decisión de producto:
///
/// <list type="bullet">
/// <item><b><c>P-048</c></b> — un miembro no propietario **no podía abandonar** un Workspace. Ni de
/// API ni de UI: <c>MVP-204</c> cubre retirar el acceso a otra persona y <c>MVP-206</c> la salida del
/// propietario. Con <c>RN-035</c> entrar en un Workspace ajeno es fácil; salir no existía.</item>
/// <item><b><c>P-049</c></b> — la interfaz nunca ofrecía revocar a un copropietario aunque la API lo
/// permite: <c>can_revoke</c> decía «activo y no propietario» mientras la guarda real solo protege al
/// propietario **único**.</item>
/// </list>
/// </summary>
public sealed class MembershipLifecycleTests : IAsyncLifetime
{
    private readonly TerrenarioApiFactory _factory = new();

    private ApiSession _propietaria = null!;
    private ApiSession _invitada = null!;
    private Guid _workspaceId;
    private Guid _invitadaUserId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();

        _factory.Google.WithIdentity("codigo-ana", "sub-ana", "Ana", "ana@ejemplo.test");
        _propietaria = await ApiSession.LoginAsync(_factory, "codigo-ana");
        _workspaceId = await _propietaria.CreateWorkspaceAsync("Finca El Olivar");

        // Se invita a una segunda persona y esta acepta: es el escenario de `RN-035`, donde entrar es
        // fácil, y el punto de partida de `P-048`.
        await _propietaria.PostJsonAsync("/api/v1/workspaces/invitations", new
        {
            channel = "email",
            email = "bruno@ejemplo.test"
        });

        _factory.Google.WithIdentity("codigo-bruno", "sub-bruno", "Bruno", "bruno@ejemplo.test");
        _invitada = await ApiSession.LoginAsync(_factory, "codigo-bruno");
        _invitadaUserId = _invitada.UserId;

        var recibidas = await _invitada.GetJsonAsync("/api/v1/invitations/received");
        var invitacionId = recibidas.GetProperty("data").EnumerateArray().First()
            .GetProperty("id").GetGuid();
        (await _invitada.PostAsync($"/api/v1/invitations/received/{invitacionId}/accept", null))
            .EnsureSuccessStatusCode();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private async Task<JsonElement> PersonAsync(ApiSession session, Guid userId)
        => (await session.GetJsonAsync("/api/v1/workspace-members"))
            .GetProperty("data").EnumerateArray()
            .First(p => p.TryGetProperty("user_id", out var id) && id.GetGuid() == userId);

    [Fact]
    public async Task Deberia_PoderAbandonar_Cuando_EsMiembroNoPropietario()
    {
        // CA-1 — y el Workspace desaparece de su selector, que es el efecto que el punto describía:
        // «arrastra ese Workspace en su selector indefinidamente».
        var response = await _invitada.PostAsync("/api/v1/workspaces/active/leave", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var suyos = await _invitada.GetJsonAsync("/api/v1/workspaces");
        suyos.GetProperty("data").EnumerateArray()
            .Should().NotContain(w => w.GetProperty("id").GetGuid() == _workspaceId);
    }

    [Fact]
    public async Task Deberia_DejarDeSerResponsableSeleccionable_SinPerderSuHistorico()
    {
        // CA-4 (MVP-208) — la fila de responsable se inactiva: deja de ofrecerse para registros nuevos
        // y las labores que ya tenía siguen mostrando su nombre.
        var antes = await _propietaria.GetJsonAsync("/api/v1/workers?is_active=true");
        antes.GetProperty("data").EnumerateArray()
            .Should().Contain(w => w.GetProperty("name").GetString() == "Bruno");

        await _invitada.PostAsync("/api/v1/workspaces/active/leave", null);

        var despues = await _propietaria.GetJsonAsync("/api/v1/workers?is_active=true");
        despues.GetProperty("data").EnumerateArray()
            .Should().NotContain(w => w.GetProperty("name").GetString() == "Bruno");

        // El histórico no se toca: la ficha sigue existiendo, inactiva.
        var todas = await _propietaria.GetJsonAsync("/api/v1/workers");
        todas.GetProperty("data").EnumerateArray()
            .Should().Contain(w => w.GetProperty("name").GetString() == "Bruno");
    }

    [Fact]
    public async Task Deberia_ExigirInvitacionNueva_Para_Volver()
    {
        // CA-5 — el reingreso es por la vía normal, igual que para quien fue revocado. No hay
        // readmisión automática ni el enlace anterior sirve.
        await _invitada.PostAsync("/api/v1/workspaces/active/leave", null);

        var persona = await PersonAsync(_propietaria, _invitadaUserId);

        persona.GetProperty("status").GetString().Should().Be("revocado");
    }

    [Fact]
    public async Task NoDeberia_DejarSalir_AlPropietarioUnico()
    {
        // CA-2 — la misma obligación que ya impone la baja de cuenta: resolver la propiedad antes.
        var response = await _propietaria.PostAsync("/api/v1/workspaces/active/leave", null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var cuerpo = await response.Content.ReadFromJsonAsync<JsonElement>();
        cuerpo.GetProperty("error").GetProperty("code").GetString()
            .Should().Be("BUSINESS_RULE_WORKSPACE_OWNERSHIP_UNRESOLVED");
    }

    [Fact]
    public async Task NoDeberia_DejarSalir_AlUltimoMiembroActivo()
    {
        // CA-3 — con la invitada fuera, la propietaria es la única que queda. La guarda que salta es
        // la de propiedad, que es más específica; lo que se comprueba es que **no se queda vacío**.
        await _invitada.PostAsync("/api/v1/workspaces/active/leave", null);

        var response = await _propietaria.PostAsync("/api/v1/workspaces/active/leave", null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── `P-049` — `can_revoke` y la guarda real describen la misma regla ──────────────────────

    [Fact]
    public async Task NoDeberia_OfrecerRevocar_AlPropietarioUnico()
    {
        // CA-6, primera mitad: con **un** propietario, ni se ofrece ni la API lo permite.
        var propietaria = await PersonAsync(_propietaria, _propietaria.UserId);

        propietaria.GetProperty("can_revoke").GetBoolean().Should().BeFalse();

        var response = await _invitada.PostAsync(
            $"/api/v1/workspace-members/{_propietaria.UserId}/revoke", null);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>
    /// CA-6, segunda mitad. **Este estado no lo produce hoy ningún flujo de la API**: el traspaso, la
    /// baja con copropietario, la reapertura y la reactivación promueven a uno y degradan al otro, así
    /// que el producto nunca llega a tener dos propietarios activos. Se comprobó uno por uno.
    ///
    /// Es decir: la incoherencia de <c>P-049</c> es **latente**, no viva —la interfaz era más
    /// restrictiva que la API, pero en la práctica coincidían—. Aun así la alineación importa, porque
    /// el día que exista un segundo propietario la pantalla escondería una acción que la API acepta, y
    /// nadie volvería a mirarlo.
    ///
    /// Por eso el estado se **siembra en base de datos**: es la única forma de comprobar la regla que
    /// la guarda dice tener, en vez de comprobar la que hoy resulta indistinguible. El hallazgo queda
    /// registrado como punto nuevo en <c>MVP-999</c>.
    /// </summary>
    [Fact]
    public async Task Deberia_OfrecerRevocar_AUnCopropietario_Cuando_QuedaOtro()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var segundo = await db.WorkspaceMembers.SingleAsync(
                m => m.WorkspaceId == _workspaceId && m.UserId == _invitadaUserId);
            segundo.PromoteToOwner();
            await db.SaveChangesAsync();
        }

        var propietaria = await PersonAsync(_propietaria, _propietaria.UserId);
        var copropietaria = await PersonAsync(_propietaria, _invitadaUserId);

        // Con dos propietarios activos, los dos se pueden revocar: la guarda solo protege al único.
        propietaria.GetProperty("can_revoke").GetBoolean().Should().BeTrue();
        copropietaria.GetProperty("can_revoke").GetBoolean().Should().BeTrue();

        // Y la operación se completa, que es lo que `can_revoke` estaba prometiendo que no.
        var response = await _propietaria.PostAsync(
            $"/api/v1/workspace-members/{_invitadaUserId}/revoke", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task NoDeberia_OfrecerRevocar_AlUltimoMiembroActivo()
    {
        // La otra mitad del `CA-8` de `MVP-204` que `can_revoke` tampoco decía: al último miembro
        // activo no se le puede retirar el acceso aunque no sea propietario.
        await _invitada.PostAsync("/api/v1/workspaces/active/leave", null);

        var propietaria = await PersonAsync(_propietaria, _propietaria.UserId);

        propietaria.GetProperty("can_revoke").GetBoolean().Should().BeFalse();
    }
}
