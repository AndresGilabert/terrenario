using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Workers;

namespace Terrenario.Api.Application.Workers;

/// <summary>
/// MVP-208 (CA-1/CA-4) — Mantiene el maestro de responsables alineado con la membresía del Workspace,
/// sin ninguna acción manual. Es la pieza que cierra `P-034`: cada miembro tiene su fila en
/// <c>workers</c>, así que un responsable —miembro o cuadrilla— siempre se identifica con un
/// <c>workers.id</c> y puede guardarse en <c>ACTIVITY.worker_id</c> (MVP-301).
///
/// Se invoca desde los tres momentos en los que la membresía cambia —crear el Workspace, aceptar una
/// invitación y revocar el acceso— y desde el login, cuando Google devuelve un nombre de display
/// distinto al que se materializó (RN-036).
///
/// <b>No persiste</b>: participa en la unidad de trabajo de quien la llama, que comparte el
/// <see cref="Microsoft.EntityFrameworkCore.DbContext"/> de la petición. Así la membresía y su fila de
/// responsable se escriben en la misma transacción implícita de EF Core y no puede quedar una sin la
/// otra.
/// </summary>
public sealed class MemberRosterService(IWorkerRepository workerRepository)
{
    /// <summary>
    /// Tope de intentos del desempate de nombre. El bucle converge en una o dos vueltas —igual que el
    /// de la migración de MVP-207—; el contador solo existe para que un dato inesperado no lo
    /// convierta en un bucle infinito.
    /// </summary>
    private const int MaxSuffixAttempts = 100;

    /// <summary>
    /// Deja a la persona como responsable seleccionable del Workspace: crea su fila si no la tenía,
    /// la reactiva si volvió a entrar y resincroniza su nombre con el de la cuenta. Idempotente.
    /// </summary>
    public async Task EnsureMemberAsync(
        Guid workspaceId,
        Guid userId,
        string displayName,
        CancellationToken ct = default)
    {
        var existing = await workerRepository.FindByUserAccountAsync(workspaceId, userId, ct);

        if (existing is not null)
        {
            existing.SyncMembership(true);
            await SyncNameAsync(existing, displayName, ct);
            return;
        }

        var name = await ClaimNameAsync(workspaceId, Worker.NormalizeName(displayName), null, ct);
        await workerRepository.AddAsync(Worker.CreateForMember(workspaceId, userId, name), ct);
    }

    /// <summary>
    /// Retira a la persona de los responsables seleccionables al revocarse su acceso (MVP-204, CA-7),
    /// <b>sin</b> invalidar los registros que ya la referencian: la fila se inactiva, nunca se borra.
    /// Es un no-op si la persona no tenía fila (miembros revocados antes de MVP-208).
    /// </summary>
    public async Task SuspendMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default)
    {
        var worker = await workerRepository.FindByUserAccountAsync(workspaceId, userId, ct);
        worker?.SyncMembership(false);
    }

    /// <summary>
    /// Resincroniza el nombre del responsable en <b>todos</b> los Workspaces de la cuenta cuando Google
    /// devuelve otro nombre de display (RN-036). Si el nombre nuevo choca con otro responsable, se
    /// aplica la misma regla que en el alta: la cuadrilla cede el nombre y el miembro lo conserva.
    /// </summary>
    public async Task SyncIdentityAsync(Guid userId, string displayName, CancellationToken ct = default)
    {
        foreach (var worker in await workerRepository.ListByUserAccountAsync(userId, ct))
            await SyncNameAsync(worker, displayName, ct);
    }

    private async Task SyncNameAsync(Worker worker, string displayName, CancellationToken ct)
    {
        var desired = Worker.NormalizeName(displayName);
        // Un cambio de mayúsculas del mismo nombre no dispara el desempate: la fila ya ocupa ese hueco
        // del índice y compararía consigo misma.
        if (string.Equals(worker.Name, desired, StringComparison.OrdinalIgnoreCase))
        {
            worker.SyncIdentityName(desired);
            return;
        }

        worker.SyncIdentityName(await ClaimNameAsync(worker.WorkspaceId, desired, worker.Id, ct));
    }

    /// <summary>
    /// Reserva <paramref name="desiredName"/> para el responsable con cuenta que va a ocuparlo,
    /// resolviendo la colisión con quien ya lo tenga:
    /// <list type="bullet">
    ///   <item>Si el ocupante es <b>cuadrilla</b>, se le renombra con sufijo y el miembro conserva el
    ///   nombre: no es renombrable y su nombre es el de su cuenta (RN-036). Es la política de datos
    ///   preexistentes que el PO ya aprobó en MVP-207: conservar y renombrar, nunca borrar.</item>
    ///   <item>Si el ocupante es <b>otro miembro</b> —dos cuentas de Google con el mismo nombre de
    ///   display en el mismo Workspace—, ninguno de los dos es renombrable, así que el sufijo lo toma
    ///   quien llega. Sin él, la materialización fallaría contra el índice único y la persona no
    ///   podría entrar en el Workspace.</item>
    /// </list>
    /// </summary>
    private async Task<string> ClaimNameAsync(
        Guid workspaceId,
        string desiredName,
        Guid? claimantWorkerId,
        CancellationToken ct)
    {
        var candidate = desiredName;

        for (var attempt = 2; attempt < MaxSuffixAttempts; attempt++)
        {
            var occupant = await workerRepository.FindByNameAsync(workspaceId, candidate, claimantWorkerId, ct);
            if (occupant is null) return candidate;

            if (!occupant.HasAccount)
            {
                await RenameAwayAsync(occupant, ct);
                return candidate;
            }

            candidate = Worker.WithSuffix(desiredName, attempt);
        }

        throw new WorkerConflictException(
            ErrorCodes.ConflictWorkerNameDuplicate,
            $"No se pudo asignar un nombre libre a «{desiredName}» en este Workspace.");
    }

    /// <summary>
    /// Aparta a un trabajador de cuadrilla del nombre que reclama un miembro, con el primer sufijo
    /// libre. El bucle repite porque el nombre generado puede chocar con uno que ya existía
    /// («Andrés Gilabert» duplicado junto a un «Andrés Gilabert (2)» previo).
    /// </summary>
    private async Task RenameAwayAsync(Worker occupant, CancellationToken ct)
    {
        for (var ordinal = 2; ordinal < MaxSuffixAttempts; ordinal++)
        {
            var candidate = Worker.WithSuffix(occupant.Name, ordinal);
            var taken = await workerRepository.ExistsWithNameAsync(
                occupant.WorkspaceId, candidate, occupant.Id, ct);

            if (taken) continue;

            occupant.RenameWithSuffix(ordinal);
            return;
        }

        throw new WorkerConflictException(
            ErrorCodes.ConflictWorkerNameDuplicate,
            $"No se pudo liberar el nombre «{occupant.Name}» en este Workspace.");
    }
}
