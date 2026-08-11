using Terrenario.Api.Application.Workers;
using Terrenario.Api.Application.Workspaces;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Domain.Workspaces;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Application.Account;

/// <summary>Lo que la persona verá antes de confirmar la baja: qué le bloquea y qué va a pasar.</summary>
public sealed record AccountClosurePreview(
    /// <summary>Workspaces de propiedad única sin resolver (RN-038). Vacío ⇒ la baja puede completarse.</summary>
    IReadOnlyList<SoleOwnedWorkspace> Obligations,
    int ActiveMemberships,
    /// <summary>
    /// MVP-811 (<c>P-118</c>) — De esas membresías, en cuántas hay **más gente**. La pantalla de baja
    /// afirmaba que se salía de <i>n</i> Workspaces «compartidos» con el adjetivo fijo, y quien era la
    /// única persona de su Workspace leía que lo compartía mientras la misma pantalla le decía más
    /// arriba «eres la única persona en este Workspace». En un flujo irreversible, un texto que
    /// describe mal la situación resta confianza justo donde hace falta.
    /// </summary>
    int SharedMemberships,
    int ActiveSessions)
{
    public bool IsClear => Obligations.Count == 0;
}

/// <summary>Resultado de una baja completada, como evidencia de lo que se hizo.</summary>
public sealed record AccountClosureResult(
    int RevokedSessions,
    int RevokedMemberships,
    int CancelledInvitations,
    DateTimeOffset PurgeAfter);

/// <summary>
/// MVP-505 (HU-3, CA-3/CA-4) — <b>Baja de cuenta</b>: el derecho de supresión del RGPD, ejercido por
/// la propia persona desde la aplicación y sin tener que escribir a nadie.
///
/// El modelo es <b>anonimización inmediata + purga diferida</b> (decisión del PO, 2026-07-30):
///
/// <list type="number">
/// <item>Al confirmar, los datos personales desaparecen <b>ya</b> —cuenta, nombre en los maestros de
/// sus Workspaces e invitaciones dirigidas a su correo— y la persona deja de poder entrar.</item>
/// <item>La <b>fila</b> sobrevive anonimizada porque cada actividad, cosecha y compra guarda quién la
/// registró: borrarla dejaría el histórico operativo del Workspace sin autoría, o lo arrastraría en
/// cascada. Lo que el derecho de supresión exige son los datos personales, y esos ya no están.</item>
/// <item>Al vencer el plazo de retención (RN-041) la fila se purga físicamente.</item>
/// </list>
///
/// <b>La regla de no-orfandad no se reimplementa</b>: se llama a
/// <see cref="WorkspaceOwnershipGuard"/>, que <c>MVP-206</c> dejó implementada y probada
/// explícitamente como punto de enganche de esta historia (RN-038, CA-4). Era la condición con la que
/// se registró <c>P-024</c>.
/// </summary>
public sealed class CloseAccountHandler(
    IUserRepository userRepository,
    IWorkspaceRepository workspaceRepository,
    IWorkspaceInvitationRepository invitationRepository,
    WorkspaceOwnershipGuard ownershipGuard,
    MemberRosterService memberRoster,
    IRefreshTokenStore refreshTokenStore,
    AccountRetentionPolicy retentionPolicy)
{
    /// <summary>Qué bloquea la baja y qué alcance tendrá, para que la confirmación sea informada.</summary>
    public async Task<AccountClosurePreview> PreviewAsync(Guid userId, CancellationToken ct = default)
    {
        var obligations = await ownershipGuard.ListObligationsAsync(userId, ct);
        var memberships = await workspaceRepository.ListActiveMembershipsAsync(userId, ct);

        // MVP-811 (`P-118`) — Cuántas de esas membresías son de verdad compartidas. Se cuenta aquí y no
        // en el cliente porque el cliente no tiene el dato: la lista de membresías no dice cuánta gente
        // hay en cada Workspace, y la pantalla acababa afirmándolo sin saberlo.
        var shared = 0;
        foreach (var membership in memberships)
        {
            if (await workspaceRepository.CountActiveMembersAsync(membership.WorkspaceId, ct) > 1) shared++;
        }

        return new AccountClosurePreview(
            obligations.Workspaces,
            memberships.Count,
            shared,
            await refreshTokenStore.CountActiveForUserAsync(userId, ct));
    }

    public async Task<AccountClosureResult> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.FindByIdAsync(userId, ct)
            ?? throw new AccountClosureException(ErrorCodes.AuthUnauthenticated, "La cuenta ya no existe.");

        if (user.IsDeleted)
            throw new AccountClosureException(ErrorCodes.AuthUnauthenticated, "La cuenta ya está dada de baja.");

        // CA-4 — la guarda de MVP-206, llamada tal cual. Si queda algún Workspace de propiedad única
        // sin resolver, la baja no puede completarse y el error dice qué falta.
        await ownershipGuard.EnsureAccountClosureAllowedAsync(userId, ct);

        var now = DateTimeOffset.UtcNow;

        // 1. Salir de los Workspaces en los que era miembro. No se borra el vínculo —los registros
        //    que ya lo referencian siguen siendo válidos (CA-7 de MVP-204)— pero deja de tener
        //    acceso, y su fila de responsable se inactiva.
        var memberships = await workspaceRepository.ListActiveMembershipsAsync(userId, ct);
        foreach (var membership in memberships)
        {
            var member = await workspaceRepository.FindActiveMemberAsync(membership.WorkspaceId, userId, ct);
            if (member is null) continue;

            member.Revoke();
            await memberRoster.SuspendMemberAsync(membership.WorkspaceId, userId, ct);
        }

        // 2. El nombre de la persona vive también en el maestro de responsables de cada Workspace
        //    (RN-036/MVP-208): sin esto, el dato personal sobreviviría a la baja donde más se ve.
        await memberRoster.SyncIdentityAsync(userId, User.AnonymizedDisplayName, ct);

        // 3. Las invitaciones pendientes dirigidas a su correo lo llevan escrito: se anulan, porque
        //    si no el email seguiría vivo en base de datos después de la baja.
        var cancelledInvitations = await invitationRepository.CancelPendingForEmailAsync(user.Email, userId, now, ct);

        // 4. Anonimizar la cuenta. A partir de aquí el `google_sub` deja de coincidir con el de
        //    Google, así que volver a entrar crea una cuenta nueva y limpia.
        user.Anonymize(now);

        // 5. Revocar las sesiones vivas: sin esto, un token de refresco emitido antes seguiría
        //    sirviendo para entrar con una cuenta que ya no existe.
        var revokedSessions = await refreshTokenStore.RevokeAllForUserAsync(userId, ct);

        await userRepository.SaveChangesAsync(ct);

        return new AccountClosureResult(
            revokedSessions,
            memberships.Count,
            cancelledInvitations,
            retentionPolicy.PurgeDateFor(now));
    }
}

/// <summary>La baja no se puede completar. El código dice por qué y la UI puede reaccionar.</summary>
public sealed class AccountClosureException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
