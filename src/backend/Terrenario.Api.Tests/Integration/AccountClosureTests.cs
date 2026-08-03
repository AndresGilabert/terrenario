using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-505 (HU-3, CA-3/CA-4) — <b>Baja de cuenta</b>: el derecho de supresión, de punta a punta.
///
/// Es la operación más irreversible del producto, así que se prueba contra la API y la base de datos
/// reales: no basta con que el endpoint conteste `200`, hay que comprobar qué queda escrito.
/// </summary>
public sealed class AccountClosureTests : IAsyncLifetime
{
    private readonly TerrenarioApiFactory _factory = new();

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _factory.Google
            .WithIdentity("codigo-andres", "sub-andres", "Andrés Gilabert", "andres@ejemplo.test")
            .WithIdentity("codigo-lucia", "sub-lucia", "Lucía Pérez", "lucia@ejemplo.test")
            .WithIdentity("codigo-andres-vuelve", "sub-andres", "Andrés Gilabert", "andres@ejemplo.test");
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static async Task<JsonElement> BodyOf(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>();

    private static Task<HttpResponseMessage> CloseAsync(ApiSession session, string frase = "ELIMINAR MI CUENTA")
        => session.PostAsync("/api/v1/account/closure", new { confirmation = frase });

    [Fact]
    public async Task Deberia_PermitirLaBaja_Cuando_NoHayWorkspacesDePropiedadUnica()
    {
        // Cuenta sin Workspace: nada que resolver.
        var session = await ApiSession.LoginAsync(_factory, "codigo-andres");

        var preview = await BodyOf(await session.GetAsync("/api/v1/account/closure"));

        preview.GetProperty("is_clear").GetBoolean().Should().BeTrue();
        preview.GetProperty("obligations").EnumerateArray().Should().BeEmpty();
        preview.GetProperty("confirmation_phrase").GetString().Should().Be("ELIMINAR MI CUENTA");
        // RN-041 — la respuesta dice cuánto se conserva lo que queda anonimizado, no solo que se borra.
        preview.GetProperty("retention_months").GetInt32().Should().Be(24);
    }

    [Fact]
    public async Task Deberia_BloquearLaBaja_Cuando_QuedaUnWorkspaceDePropiedadUnica()
    {
        // CA-4 / RN-038 — la guarda de MVP-206, llamada desde la baja de cuenta sin reimplementarla.
        var session = await ApiSession.LoginAsync(_factory, "codigo-andres");
        await session.CreateWorkspaceAsync("Finca El Olivar");

        var preview = await BodyOf(await session.GetAsync("/api/v1/account/closure"));
        preview.GetProperty("is_clear").GetBoolean().Should().BeFalse();
        preview.GetProperty("obligations").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("name").GetString().Should().Be("Finca El Olivar");

        var response = await CloseAsync(session);

        // 422: la petición es correcta; es el estado del sistema el que no permite completarla.
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await BodyOf(response)).GetProperty("error").GetProperty("code").GetString()
            .Should().Be("BUSINESS_RULE_WORKSPACE_OWNERSHIP_UNRESOLVED");

        // Y la cuenta sigue viva: un rechazo no puede dejar la baja a medias.
        await using var db = _factory.CreateDbContext();
        (await db.Users.SingleAsync(u => u.GoogleSub == "sub-andres")).IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("eliminar mi cuenta")]
    [InlineData("BORRAR MI CUENTA")]
    public async Task Deberia_RechazarLaBaja_Cuando_LaConfirmacionNoEsExacta(string frase)
    {
        var session = await ApiSession.LoginAsync(_factory, "codigo-andres");

        var response = await CloseAsync(session, frase);

        // CA-3 — la confirmación se comprueba **en servidor**: una operación irreversible no puede
        // depender de que el cliente se porte bien.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await using var db = _factory.CreateDbContext();
        (await db.Users.SingleAsync(u => u.GoogleSub == "sub-andres")).IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Deberia_BorrarLosDatosPersonales_Cuando_SeCompletaLaBaja()
    {
        var session = await ApiSession.LoginAsync(_factory, "codigo-andres");
        var userId = session.UserId;

        var response = await CloseAsync(session);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var db = _factory.CreateDbContext();
        var user = await db.Users.SingleAsync(u => u.Id == userId);

        user.IsDeleted.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        // Ni el nombre, ni el correo, ni el identificador de Google sobreviven a la baja.
        user.DisplayName.Should().Be("Cuenta eliminada");
        user.Email.Should().NotContain("andres@ejemplo.test");
        user.GoogleSub.Should().NotBe("sub-andres");
        // El dominio `.invalid` está reservado (RFC 2606): ese correo no puede existir.
        user.Email.Should().EndWith("@terrenario.invalid");
    }

    [Fact]
    public async Task Deberia_ImpedirVolverAEntrar_Cuando_LaCuentaEstaDadaDeBaja()
    {
        var session = await ApiSession.LoginAsync(_factory, "codigo-andres");
        var userIdOriginal = session.UserId;
        await CloseAsync(session);

        // La persona vuelve con **la misma** cuenta de Google.
        var vuelta = await ApiSession.LoginAsync(_factory, "codigo-andres-vuelve");

        // No se la reconoce: entra como cuenta nueva y vacía. Es lo que hace que la supresión sea de
        // verdad y no una desactivación.
        vuelta.UserId.Should().NotBe(userIdOriginal);
        vuelta.WorkspaceId.Should().BeNull();
    }

    [Fact]
    public async Task Deberia_RevocarLasSesiones_Cuando_SeCompletaLaBaja()
    {
        var session = await ApiSession.LoginAsync(_factory, "codigo-andres");
        var userId = session.UserId;

        var preview = await BodyOf(await session.GetAsync("/api/v1/account/closure"));
        preview.GetProperty("active_sessions").GetInt32().Should().BeGreaterThan(0);

        var result = await BodyOf(await CloseAsync(session));
        result.GetProperty("revoked_sessions").GetInt32().Should().BeGreaterThan(0);

        await using var db = _factory.CreateDbContext();
        // Sin esto, un token de refresco emitido antes seguiría sirviendo para volver a entrar.
        (await db.RefreshTokens.CountAsync(rt => rt.UserId == userId && rt.RevokedAt == null))
            .Should().Be(0);
    }

    [Fact]
    public async Task Deberia_SalirDeLosWorkspacesAjenos_Y_AnonimizarSuNombreEnElMaestro()
    {
        // Lucía tiene el Workspace; Andrés es miembro, así que no tiene obligaciones de propiedad.
        var lucia = await ApiSession.LoginAsync(_factory, "codigo-lucia");
        var workspaceId = await lucia.CreateWorkspaceAsync("Cortijo del Río");

        var invitacion = await lucia.PostJsonAsync("/api/v1/workspaces/invitations", new
        {
            channel = "email",
            email = "andres@ejemplo.test"
        });

        var andres = await ApiSession.LoginAsync(_factory, "codigo-andres");
        var recibidas = await andres.GetJsonAsync("/api/v1/invitations/received");
        var invitacionId = recibidas.GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();
        var aceptada = await andres.PostAsync($"/api/v1/invitations/received/{invitacionId}/accept", null);
        aceptada.EnsureSuccessStatusCode();

        var resultado = await BodyOf(await CloseAsync(andres));
        resultado.GetProperty("revoked_memberships").GetInt32().Should().BeGreaterThan(0);

        await using var db = _factory.CreateDbContext();

        // Deja de tener acceso, pero el vínculo no se borra: los registros que ya lo referencian
        // siguen siendo válidos (CA-7 de MVP-204).
        var membresia = await db.WorkspaceMembers.SingleAsync(m => m.UserId == andres.UserId);
        membresia.Status.Should().Be("revocado");

        // Y su nombre desaparece del maestro de responsables, que es donde más se ve (RN-036).
        var responsables = await db.Workers
            .Where(w => w.WorkspaceId == workspaceId && w.UserAccountId == andres.UserId)
            .ToListAsync();
        responsables.Should().OnlyContain(w => w.Name == "Cuenta eliminada");

        _ = invitacion;
    }

    [Fact]
    public async Task Deberia_AnularLasInvitacionesDirigidasASuCorreo_Cuando_SeDaDeBaja()
    {
        var lucia = await ApiSession.LoginAsync(_factory, "codigo-lucia");
        await lucia.CreateWorkspaceAsync("Cortijo del Río");
        await lucia.PostJsonAsync("/api/v1/workspaces/invitations", new
        {
            channel = "email",
            email = "andres@ejemplo.test"
        });

        var andres = await ApiSession.LoginAsync(_factory, "codigo-andres");
        var resultado = await BodyOf(await CloseAsync(andres));

        resultado.GetProperty("cancelled_invitations").GetInt32().Should().Be(1);

        await using var db = _factory.CreateDbContext();
        // Una invitación pendiente lleva el email escrito: sin anularla, el dato personal
        // sobreviviría a la supresión.
        (await db.WorkspaceInvitations.CountAsync(i => i.Status == "pendiente")).Should().Be(0);
    }

    [Fact]
    public async Task Deberia_ExigirSesion_Cuando_SePideLaBajaSinToken()
    {
        var anonymous = _factory.CreateApiClient();

        var response = await anonymous.PostAsJsonAsync(
            "/api/v1/account/closure",
            new { confirmation = "ELIMINAR MI CUENTA" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deberia_PoderDarseDeBaja_Cuando_NoTieneNingunWorkspace()
    {
        // La baja es de la **cuenta**, no de un Workspace: quien no tenga ninguno también tiene
        // derecho a irse. Por eso el endpoint no exige contexto de Workspace.
        var session = await ApiSession.LoginAsync(_factory, "codigo-andres");

        var response = await CloseAsync(session);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
