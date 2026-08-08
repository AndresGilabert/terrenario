using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Application.Account;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Data;

namespace Terrenario.Api.Application.Retention;

/// <summary>
/// MVP-504 (B-3) — <b>Ejecuta</b> la política de retención de <c>RN-041</c>.
///
/// Hasta aquí el plazo existía en tres sitios —la regla, la tabla de retención y
/// <see cref="AccountRetentionPolicy"/>— y no lo aplicaba nadie: la baja de cuenta devolvía una
/// fecha de purga que nunca llegaba. Una política declarada que no se ejecuta es peor que no tenerla,
/// porque se documenta un compromiso que se incumple desde el primer día y el desfase crece solo.
///
/// <b>Lo que NO hace</b>: borrar datos personales. Eso ya ocurre en el acto al darse de baja
/// (<c>MVP-505</c>, <see cref="Terrenario.Api.Domain.Users.User.Anonymize"/>). Lo que queda a los 24
/// meses son filas anonimizadas y registros operativos sin PII, así que esto no es el derecho de
/// supresión: es el principio de limitación del plazo de conservación.
///
/// <b>Orden</b>: de hijo a padre. Las FK hacia <c>users</c> son <c>Restrict</c> —a propósito, para
/// que nada borre por accidente el rastro de quién hizo qué—, así que una cuenta solo puede
/// purgarse cuando ya no la referencia nadie. Por eso va la última y por eso puede quedarse.
///
/// <b>MVP-714 (P-071)</b> añade una sexta categoría con <b>plazo propio</b>: los tokens de refresco
/// muertos, a 30 días. Es la única línea que no usa el corte de 24 meses, y por eso se calculan dos.
/// </summary>
public sealed class RetentionPurgeService(TerrenarioDbContext db, AccountRetentionPolicy policy)
{
    /// <summary>
    /// Purga todo lo que cumplió su plazo antes de <paramref name="now"/>.
    ///
    /// El instante se recibe en vez de leerlo del reloj para que la rutina sea comprobable: sin esto
    /// probar una retención de 24 meses exigiría esperarlos.
    /// </summary>
    public async Task<RetentionPurgeReport> PurgeAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        var cutoff = policy.CutoffFrom(now);
        var tokenCutoff = policy.RefreshTokenCutoffFrom(now);

        // 1. Invitaciones terminales. La caducada no es un estado persistido (se deriva de
        //    `expires_at`), así que se comprueba la fecha en vez del estado.
        var invitations = await db.WorkspaceInvitations
            .Where(i =>
                (i.AcceptedAt != null && i.AcceptedAt < cutoff) ||
                (i.RejectedAt != null && i.RejectedAt < cutoff) ||
                (i.CancelledAt != null && i.CancelledAt < cutoff) ||
                (i.Status == InvitationStatuses.Pending && i.ExpiresAt < cutoff))
            .ExecuteDeleteAsync(ct);

        // 2. Solicitudes de reactivación resueltas o caducadas (MVP-206).
        var reactivations = await db.WorkspaceReactivationRequests
            .Where(r =>
                (r.ResolvedAt != null && r.ResolvedAt < cutoff) ||
                ((r.Status == ReactivationRequestStatuses.Pending ||
                  r.Status == ReactivationRequestStatuses.Requested) && r.ExpiresAt < cutoff))
            .ExecuteDeleteAsync(ct);

        // 3. Registros operativos eliminados lógicamente (RN-037). El consumo va antes que la compra
        //    y la cosecha: es el que cuelga de ellas.
        var operational =
            await db.PurchaseConsumptions.Where(c => c.DeletedAt != null && c.DeletedAt < cutoff).ExecuteDeleteAsync(ct) +
            await db.Harvests.Where(h => h.DeletedAt != null && h.DeletedAt < cutoff).ExecuteDeleteAsync(ct) +
            await db.Activities.Where(a => a.DeletedAt != null && a.DeletedAt < cutoff).ExecuteDeleteAsync(ct) +
            await db.Purchases.Where(p => p.DeletedAt != null && p.DeletedAt < cutoff).ExecuteDeleteAsync(ct);

        // 4. Workspaces dados de baja (RN-039). La cascada de la base de datos se lleva por delante
        //    miembros, temporadas, terrenos, cuadrilla, tareas y todo el histórico operativo: es
        //    justo lo que RN-041 promete, y por eso la baja tiene 24 meses de margen para volver.
        var workspaces = await db.Workspaces
            .Where(w => w.DeletedAt != null && w.DeletedAt < cutoff)
            .ExecuteDeleteAsync(ct);

        // 5. Cuentas dadas de baja que ya no referencia nadie.
        var (accounts, retained) = await PurgeAccountsAsync(cutoff, ct);

        // 6. Tokens de refresco muertos (MVP-714, P-071). Va al final aunque cuelgue de una cuenta
        //    porque `refresh_tokens` **no tiene FK** hacia `users`: no retiene a nadie ni la retiene
        //    nadie, así que el orden es indiferente.
        //
        //    Esa ausencia de FK es justo lo que hacía falta arreglar. La suposición al registrar
        //    P-071 era que la purga de la cuenta se llevaba los tokens por cascada y que el problema
        //    era solo el plazo; en realidad no hay cascada que valga y las filas quedaban huérfanas
        //    para siempre, sin cuenta a la que volver. Con esta línea desaparecen 30 días después de
        //    morir, mucho antes de que la cuenta llegue a purgarse.
        //
        //    Un token muere en cuanto se revoca **o** caduca, lo que ocurra primero, y desde ahí
        //    corre su plazo: de ahí el `OR`. Lo vivo —sin revocar y con `expires_at` en el futuro—
        //    no puede cumplir ninguna de las dos condiciones, porque `expires_at > now > tokenCutoff`.
        var refreshTokens = await db.RefreshTokens
            .Where(rt =>
                (rt.RevokedAt != null && rt.RevokedAt < tokenCutoff) ||
                rt.ExpiresAt < tokenCutoff)
            .ExecuteDeleteAsync(ct);

        return new RetentionPurgeReport(
            invitations, reactivations, operational, workspaces, accounts, refreshTokens, retained);
    }

    /// <summary>
    /// Purga las cuentas anonimizadas cuyo plazo venció, saltando las que todavía sostienen una FK.
    ///
    /// Se comprueba antes de borrar en lugar de intentarlo y capturar el fallo: así una cuenta
    /// retenida no aborta la purga de las demás, y el motivo queda contado en el informe en vez de
    /// perdido en una excepción.
    ///
    /// Si alguien añade otra FK <c>Restrict</c> hacia <c>users</c>, hay que añadirla aquí. El test
    /// de integración lo detecta: la cuenta dejaría de purgarse y saltaría la aserción.
    /// </summary>
    private async Task<(int Purged, int Retained)> PurgeAccountsAsync(DateTimeOffset cutoff, CancellationToken ct)
    {
        var expired = db.Users.Where(u => u.DeletedAt != null && u.DeletedAt < cutoff);

        var total = await expired.CountAsync(ct);
        if (total == 0) return (0, 0);

        var purged = await expired
            .Where(u =>
                !db.Workspaces.Any(w => w.OwnerId == u.Id || w.DeletedByUserId == u.Id) &&
                !db.WorkspaceInvitations.Any(i =>
                    i.InvitedByUserId == u.Id ||
                    i.AcceptedByUserId == u.Id ||
                    i.RejectedByUserId == u.Id ||
                    i.CancelledByUserId == u.Id) &&
                !db.WorkspaceReactivationRequests.Any(r =>
                    r.RecipientUserId == u.Id || r.AuthorizerUserId == u.Id))
            .ExecuteDeleteAsync(ct);

        // Que una cuenta se quede no es un incidente de privacidad: la fila ya no identifica a nadie
        // desde el momento de la baja. Se cuenta para que se vea, no para alarmar.
        return (purged, total - purged);
    }
}

/// <summary>Qué se llevó una ejecución. Se registra en el log para que la rutina sea auditable.</summary>
public sealed record RetentionPurgeReport(
    int Invitations,
    int ReactivationRequests,
    int OperationalRecords,
    int Workspaces,
    int Accounts,
    int RefreshTokens,
    int AccountsRetained)
{
    public int Total =>
        Invitations + ReactivationRequests + OperationalRecords + Workspaces + Accounts + RefreshTokens;

    public override string ToString() =>
        $"invitaciones={Invitations}, reactivaciones={ReactivationRequests}, " +
        $"registros operativos={OperationalRecords}, workspaces={Workspaces}, " +
        $"cuentas={Accounts} (retenidas por referencias={AccountsRetained}), " +
        $"tokens de refresco={RefreshTokens}";
}
