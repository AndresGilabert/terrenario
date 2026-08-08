using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Application.Account;
using Terrenario.Api.Application.Retention;
using Terrenario.Api.Domain.Activities;
using Terrenario.Api.Domain.Plots;
using Terrenario.Api.Domain.Seasons;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workers;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Auth;
using Terrenario.Api.Infrastructure.Retention;

namespace Terrenario.Api.Tests.Integration;

/// <summary>
/// MVP-504 (B-3) — La rutina de expurgo de <c>RN-041</c>, contra PostgreSQL real.
///
/// Aquí el motor real no es un lujo: media prueba consiste en que la <b>cascada de la base de datos</b>
/// se lleve el contenido de un Workspace purgado, y en que las FK <c>Restrict</c> hacia <c>users</c>
/// retengan de verdad una cuenta referenciada. Ninguna de las dos cosas existe fuera del motor.
///
/// El instante se pasa como parámetro, así que probar una retención de 24 meses no exige esperarlos:
/// se pregunta «¿qué habría que purgar dentro de 25 meses?».
///
/// <b>MVP-714 (CA-2)</b> añade los tokens de refresco. Su plazo son 30 días, no 24 meses, así que
/// esos casos siembran las fechas hacia atrás y preguntan por <i>hoy</i>.
/// </summary>
public sealed class RetentionPurgeTests : RepositoryTestBase
{
    private static readonly DateTimeOffset Ahora = DateTimeOffset.UtcNow;

    /// <summary>Momento en el que todo lo sembrado ya ha cumplido los 24 meses de RN-041.</summary>
    private static readonly DateTimeOffset PlazoCumplido = Ahora.AddMonths(25);

    private RetentionPurgeService NewService() => new(Db, new AccountRetentionPolicy());

    private async Task<(User Owner, Workspace Workspace)> SeedWorkspaceAsync(string suffix)
    {
        var owner = User.Create($"sub-{suffix}", $"Andrés {suffix}", $"andres-{suffix}@ejemplo.test");
        Db.Users.Add(owner);
        var workspace = Workspace.Create(owner.Id, $"Finca {suffix}");
        Db.Workspaces.Add(workspace);
        await Db.SaveChangesAsync();
        return (owner, workspace);
    }

    [Fact]
    public async Task Deberia_NoBorrarNada_Cuando_NadaHaCumplidoElPlazo()
    {
        var (owner, workspace) = await SeedWorkspaceAsync("intacta");
        workspace.SoftDelete(owner.Id, Ahora);
        await Db.SaveChangesAsync();

        // Hoy: la baja es de hace un instante, le quedan 24 meses por delante.
        var report = await NewService().PurgeAsync(Ahora);

        report.Total.Should().Be(0);
        (await Db.Workspaces.CountAsync(w => w.Id == workspace.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Deberia_PurgarElWorkspaceDadoDeBaja_Y_TodoSuContenido()
    {
        var (owner, workspace) = await SeedWorkspaceAsync("purgada");

        var plot = Plot.Create(workspace.Id, "Olivar Alto", "propia");
        var season = Season.Create(
            workspace.Id, "2026/2027", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28));
        var worker = Worker.Create(workspace.Id, "Antonio");
        Db.AddRange(plot, season, worker);
        await Db.SaveChangesAsync();

        var activity = Activity.Create(
            workspace.Id, plot.Id, season.Id, worker.Id,
            new DateOnly(2026, 10, 1), 4m, null, "Poda", 70m, null, owner.Id);
        Db.Activities.Add(activity);

        workspace.SoftDelete(owner.Id, Ahora);
        await Db.SaveChangesAsync();

        var report = await NewService().PurgeAsync(PlazoCumplido);

        report.Workspaces.Should().Be(1);

        // Lo que prueba que la cascada existe de verdad: el contenido no se borra explícitamente en
        // ningún sitio del servicio, se lo lleva la FK del propio motor.
        await using var db = NewDb();
        (await db.Workspaces.AnyAsync(w => w.Id == workspace.Id)).Should().BeFalse();
        (await db.Activities.AnyAsync(a => a.WorkspaceId == workspace.Id)).Should().BeFalse();
        (await db.Plots.AnyAsync(p => p.WorkspaceId == workspace.Id)).Should().BeFalse();
        (await db.Workers.AnyAsync(w => w.WorkspaceId == workspace.Id)).Should().BeFalse();
        (await db.Seasons.AnyAsync(s => s.WorkspaceId == workspace.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Deberia_PurgarSoloLosRegistrosOperativosEliminados()
    {
        var (owner, workspace) = await SeedWorkspaceAsync("operativa");

        var plot = Plot.Create(workspace.Id, "Olivar Bajo", "propia");
        var season = Season.Create(
            workspace.Id, "2026/2027", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28));
        var worker = Worker.Create(workspace.Id, "Lucía");
        Db.AddRange(plot, season, worker);
        await Db.SaveChangesAsync();

        Activity NuevaActividad(int dia) => Activity.Create(
            workspace.Id, plot.Id, season.Id, worker.Id,
            new DateOnly(2026, 10, dia), 4m, null, "Poda", 70m, null, owner.Id);

        var borrada = NuevaActividad(1);
        var viva = NuevaActividad(2);
        Db.AddRange(borrada, viva);
        borrada.Delete(owner.Id);
        await Db.SaveChangesAsync();

        var report = await NewService().PurgeAsync(PlazoCumplido);

        report.OperationalRecords.Should().Be(1);

        await using var db = NewDb();
        (await db.Activities.AnyAsync(a => a.Id == borrada.Id)).Should().BeFalse();
        // El Workspace sigue vivo, así que lo que no está eliminado no se toca por antiguo que sea.
        (await db.Activities.AnyAsync(a => a.Id == viva.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task Deberia_PurgarLaCuentaAnonimizada_Cuando_YaNoLaReferenciaNadie()
    {
        var user = User.Create("sub-baja", "Andrés", "andres-baja@ejemplo.test");
        Db.Users.Add(user);
        await Db.SaveChangesAsync();

        user.Anonymize(Ahora);
        await Db.SaveChangesAsync();

        var report = await NewService().PurgeAsync(PlazoCumplido);

        report.Accounts.Should().Be(1);
        report.AccountsRetained.Should().Be(0);

        await using var db = NewDb();
        (await db.Users.AnyAsync(u => u.Id == user.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Deberia_RetenerLaCuenta_Cuando_TodaviaLaReferenciaUnWorkspaceVivo()
    {
        // Es un caso real, no artificial: `workspaces.owner_id` no se traspasa al cerrar la cuenta, así
        // que un Workspace que sigue vivo porque tiene otro propietario activo (RN-038) mantiene la
        // referencia. La FK es `Restrict` a propósito —para no borrar por accidente el rastro de quién
        // hizo qué— y el expurgo tiene que respetarla en vez de reventar contra ella.
        var (owner, _) = await SeedWorkspaceAsync("referenciada");

        owner.Anonymize(Ahora);
        await Db.SaveChangesAsync();

        var report = await NewService().PurgeAsync(PlazoCumplido);

        report.Accounts.Should().Be(0);
        report.AccountsRetained.Should().Be(1);

        await using var db = NewDb();
        var retenido = await db.Users.SingleAsync(u => u.Id == owner.Id);
        // Quedarse no es una fuga: la fila dejó de identificar a nadie en el momento de la baja.
        retenido.DisplayName.Should().Be(User.AnonymizedDisplayName);
    }

    [Fact]
    public async Task Deberia_PurgarLaCuenta_Cuando_LaInvitacionQueLaReferenciaTambienVence()
    {
        // El orden importa: la invitación se purga en el paso 1 y eso libera la FK que retenía a la
        // cuenta en el paso 5. Si el servicio purgara al revés, la cuenta se quedaría para siempre.
        var (owner, workspace) = await SeedWorkspaceAsync("orden");

        var invitation = WorkspaceInvitation.Create(
            workspace.Id, owner.Id, InvitationChannels.Email,
            "invitada@ejemplo.test", "hash-de-token", TimeSpan.FromDays(7));
        Db.WorkspaceInvitations.Add(invitation);
        await Db.SaveChangesAsync();

        // El Workspace también se da de baja: si no, retendría a la cuenta por `owner_id`.
        workspace.SoftDelete(owner.Id, Ahora);
        owner.Anonymize(Ahora);
        await Db.SaveChangesAsync();

        var report = await NewService().PurgeAsync(PlazoCumplido);

        report.Workspaces.Should().Be(1);
        report.Accounts.Should().Be(1);

        await using var db = NewDb();
        (await db.WorkspaceInvitations.AnyAsync(i => i.Id == invitation.Id)).Should().BeFalse();
        (await db.Users.AnyAsync(u => u.Id == owner.Id)).Should().BeFalse();
    }

    /// <summary>
    /// MVP-714 (CA-2) — Los tokens de refresco muertos, con su plazo propio de 30 días.
    ///
    /// Se siembran las cuatro situaciones que se dan en la tabla y se comprueba <b>una sola pasada</b>
    /// contra las cuatro: si se probaran por separado, un predicado que borrase de más pasaría igual
    /// mientras el token vivo no estuviera delante.
    /// </summary>
    [Fact]
    public async Task Deberia_PurgarLosTokensDeRefrescoMuertos_Y_NoTocarLosVivos()
    {
        var (owner, _) = await SeedWorkspaceAsync("tokens");
        var ahora = DateTimeOffset.UtcNow;

        // Caducado hace 31 días y nunca revocado: el usuario simplemente dejó de volver.
        var caducado = NuevoToken(owner.Id, "caducado", expiresAt: ahora.AddDays(-31));

        // Revocado hace 31 días por rotación, con la caducidad todavía por delante: es el caso
        // masivo, uno por cada refresco de cada sesión activa.
        var revocado = NuevoToken(
            owner.Id, "revocado", expiresAt: ahora.AddDays(-1), revokedAt: ahora.AddDays(-31));

        // Muerto, pero hace solo una semana: dentro del plazo, se queda.
        var recienMuerto = NuevoToken(owner.Id, "reciente", expiresAt: ahora.AddDays(-7));

        // Sesión viva. Que siga aquí es la mitad del criterio: la rutina no puede echar a nadie.
        var vivo = NuevoToken(owner.Id, "vivo", expiresAt: ahora.AddDays(23));

        Db.RefreshTokens.AddRange(caducado, revocado, recienMuerto, vivo);
        await Db.SaveChangesAsync();

        // `ahora`, no `PlazoCumplido`: el plazo de esta categoría no son 24 meses, así que se
        // comprueba con el reloj de hoy. Con `PlazoCumplido` no se distinguiría de los demás.
        var report = await NewService().PurgeAsync(ahora);

        report.RefreshTokens.Should().Be(2);

        await using var db = NewDb();
        (await db.RefreshTokens.AnyAsync(rt => rt.Id == caducado.Id)).Should().BeFalse();
        (await db.RefreshTokens.AnyAsync(rt => rt.Id == revocado.Id)).Should().BeFalse();
        (await db.RefreshTokens.AnyAsync(rt => rt.Id == recienMuerto.Id)).Should().BeTrue(
            "un token muerto hace una semana no ha cumplido los 30 días de RN-041");
        (await db.RefreshTokens.AnyAsync(rt => rt.Id == vivo.Id)).Should().BeTrue(
            "purgar una sesión abierta echaría al usuario, que es lo contrario de lo que se pide");
    }

    /// <summary>
    /// MVP-714 — El día justo. Sin esto, un error de signo en <c>RefreshTokenCutoffFrom</c> daría
    /// igual: el token de 31 días se borraría de todos modos.
    /// </summary>
    [Fact]
    public async Task Deberia_RespetarElPlazoExactoDeTreintaDias()
    {
        var (owner, _) = await SeedWorkspaceAsync("frontera");
        var ahora = DateTimeOffset.UtcNow;

        var justoAntes = NuevoToken(
            owner.Id, "justo-antes",
            expiresAt: ahora.AddDays(-AccountRetentionPolicy.RefreshTokenRetentionDays).AddHours(-1));
        var justoDespues = NuevoToken(
            owner.Id, "justo-despues",
            expiresAt: ahora.AddDays(-AccountRetentionPolicy.RefreshTokenRetentionDays).AddHours(1));

        Db.RefreshTokens.AddRange(justoAntes, justoDespues);
        await Db.SaveChangesAsync();

        (await NewService().PurgeAsync(ahora)).RefreshTokens.Should().Be(1);

        await using var db = NewDb();
        (await db.RefreshTokens.AnyAsync(rt => rt.Id == justoAntes.Id)).Should().BeFalse();
        (await db.RefreshTokens.AnyAsync(rt => rt.Id == justoDespues.Id)).Should().BeTrue();
    }

    /// <summary>
    /// MVP-714 — El motivo real de <c>P-071</c>: <c>refresh_tokens</c> <b>no tiene FK</b> hacia
    /// <c>users</c>, así que purgar la cuenta no arrastra sus tokens y las filas quedaban huérfanas
    /// para siempre. Este test fija la conducta contra la que se escribió la línea nueva.
    /// </summary>
    [Fact]
    public async Task Deberia_DejarHuerfanoElToken_Cuando_SePurgaLaCuentaYElTokenSigueEnPlazo()
    {
        var user = User.Create("sub-huerfano", "Andrés", "andres-huerfano@ejemplo.test");
        Db.Users.Add(user);

        // Revocado al cerrar la cuenta, como hace `CloseAccountHandler`.
        var token = NuevoToken(user.Id, "huerfano", expiresAt: Ahora.AddDays(30), revokedAt: Ahora);
        Db.RefreshTokens.Add(token);
        user.Anonymize(Ahora);
        await Db.SaveChangesAsync();

        // A los 25 meses la cuenta cumple su plazo; el token cumplió el suyo mucho antes, así que
        // esta pasada se lleva las dos cosas y no queda huérfano nada.
        var report = await NewService().PurgeAsync(PlazoCumplido);

        report.Accounts.Should().Be(1);
        report.RefreshTokens.Should().Be(1);

        await using var db = NewDb();
        (await db.RefreshTokens.AnyAsync(rt => rt.UserId == user.Id)).Should().BeFalse(
            "sin FK no hay cascada: si la rutina no los borra, no los borra nadie");
    }

    /// <summary>Token con las fechas puestas a mano: sembrar por el store obligaría a esperar 30 días.</summary>
    private static RefreshTokenEntity NuevoToken(
        Guid userId, string sufijo, DateTimeOffset expiresAt, DateTimeOffset? revokedAt = null) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            // El valor no se parece a un token real a propósito: no hace falta y el índice único solo
            // exige que no se repita.
            TokenHash = $"hash-{sufijo}-{Guid.NewGuid():N}",
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt,
            CreatedAt = expiresAt.AddDays(-30)
        };

    [Fact]
    public async Task Deberia_SerIdempotente_Cuando_SeEjecutaDosVeces()
    {
        var (owner, workspace) = await SeedWorkspaceAsync("idempotente");
        workspace.SoftDelete(owner.Id, Ahora);
        owner.Anonymize(Ahora);
        await Db.SaveChangesAsync();

        var primera = await NewService().PurgeAsync(PlazoCumplido);
        primera.Total.Should().BeGreaterThan(0);

        // La rutina corre a diario sin supervisión: si la segunda pasada no fuera inocua, cada día
        // sería una oportunidad de romper algo.
        var segunda = await NewService().PurgeAsync(PlazoCumplido);
        segunda.Total.Should().Be(0);
        segunda.AccountsRetained.Should().Be(0);
    }
}

/// <summary>
/// MVP-504 (B-3) — El cerrojo que impide que dos instancias purguen a la vez. Se prueba aparte porque
/// es lo único de la rutina que depende del motor: la forma de la consulta escalar y la semántica del
/// <i>advisory lock</i> son de PostgreSQL, y en un mock siempre saldría bien.
/// </summary>
public sealed class RetentionAdvisoryLockTests : RepositoryTestBase
{
    [Fact]
    public async Task Deberia_ConcederseSoloAlPrimero_Cuando_DosTransaccionesCompiten()
    {
        await using var primera = NewDb();
        await using var segunda = NewDb();

        await using var t1 = await primera.Database.BeginTransactionAsync();
        (await RetentionAdvisoryLock.TryAcquireAsync(primera)).Should().BeTrue();

        // Otra conexión, otra transacción: es la situación de dos réplicas de la API a la misma hora.
        await using var t2 = await segunda.Database.BeginTransactionAsync();
        (await RetentionAdvisoryLock.TryAcquireAsync(segunda)).Should().BeFalse(
            "el cerrojo no debe concederse dos veces, o las dos instancias purgarían a la vez");

        // Y no bloquea: si esperase, este test no habría llegado hasta aquí.
        await t1.RollbackAsync();

        // Al ser de ámbito de transacción, cerrar la primera lo libera sin tener que soltarlo a mano.
        (await RetentionAdvisoryLock.TryAcquireAsync(segunda)).Should().BeTrue();
        await t2.RollbackAsync();
    }
}
