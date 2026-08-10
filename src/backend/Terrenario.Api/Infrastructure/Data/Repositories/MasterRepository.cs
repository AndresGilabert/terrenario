using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Domain.Masters;
using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Depuración de los cuatro maestros (MVP-806). Implementa el puerto único
/// <see cref="IMasterRepository"/> apoyándose en <see cref="MasterReferenceMap"/>, que es donde está
/// declarado <b>una sola vez</b> quién puede referenciar a cada maestro.
///
/// El recuento es genérico: recorre el mapa. La reasignación no lo es —necesita el método del
/// agregado que mueve la versión (ADR-0005)— pero no puede quedarse corta sin que se note, porque
/// antes de borrar el absorbido se vuelve a contar su uso dentro de la misma transacción, y por
/// debajo están las claves ajenas <c>RESTRICT</c> del modelo.
/// </summary>
public sealed class MasterRepository(TerrenarioDbContext db) : IMasterRepository
{
    public async Task<MasterRecord?> FindAsync(
        MasterKind kind, Guid workspaceId, Guid masterId, CancellationToken ct = default)
        => kind switch
        {
            MasterKind.Plot => await db.Plots
                .Where(p => p.WorkspaceId == workspaceId && p.Id == masterId)
                .Select(p => new MasterRecord(p.Id, p.Name, false))
                .FirstOrDefaultAsync(ct),
            MasterKind.Season => await db.Seasons
                .Where(s => s.WorkspaceId == workspaceId && s.Id == masterId)
                .Select(s => new MasterRecord(s.Id, s.Name, false))
                .FirstOrDefaultAsync(ct),
            MasterKind.Worker => await db.Workers
                .Where(w => w.WorkspaceId == workspaceId && w.Id == masterId)
                .Select(w => new MasterRecord(w.Id, w.Name, w.UserAccountId != null))
                .FirstOrDefaultAsync(ct),
            MasterKind.Task => await db.Tasks
                .Where(t => t.WorkspaceId == workspaceId && t.Id == masterId)
                .Select(t => new MasterRecord(t.Id, t.Name, false))
                .FirstOrDefaultAsync(ct),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Maestro no contemplado.")
        };

    public async Task<MasterUsage> CountUsageAsync(
        MasterKind kind, Guid workspaceId, Guid masterId, CancellationToken ct = default)
    {
        var counted = new List<MasterUsageReference>();

        foreach (var reference in MasterReferenceMap.For(kind).Where(r => r.IsOperational))
        {
            var count = await reference.ReferencedIds(db, workspaceId).CountAsync(id => id == masterId, ct);
            if (count > 0)
                counted.Add(new MasterUsageReference(reference.SingularLabel, reference.PluralLabel, count));
        }

        return counted.Count == 0 ? MasterUsage.None : new MasterUsage(counted);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountUsageByWorkspaceAsync(
        MasterKind kind, Guid workspaceId, CancellationToken ct = default)
    {
        var totals = new Dictionary<Guid, int>();

        // Una consulta agrupada por tipo de referencia (tres en el peor caso), no una por fila del
        // maestro: el listado de terrenos de un Workspace con cincuenta fichas haría cincuenta idas.
        foreach (var reference in MasterReferenceMap.For(kind).Where(r => r.IsOperational))
        {
            var grouped = await reference.ReferencedIds(db, workspaceId)
                .GroupBy(id => id)
                .Select(g => new { MasterId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            foreach (var row in grouped)
                totals[row.MasterId] = totals.GetValueOrDefault(row.MasterId) + row.Count;
        }

        return totals;
    }

    public Task DeleteAsync(
        MasterKind kind, Guid workspaceId, Guid masterId, CancellationToken ct = default)
        => RemoveAsync(kind, workspaceId, masterId, ct);

    public async Task<int> MergeAsync(
        MasterKind kind,
        Guid workspaceId,
        Guid survivorId,
        Guid absorbedId,
        Guid userId,
        CancellationToken ct = default)
    {
        // Reapuntar y borrar tienen que ser atómicos: una caída en medio dejaría media operativa
        // apuntando al superviviente y el absorbido todavía en la lista.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var reassigned = kind switch
        {
            MasterKind.Plot => await ReassignPlotAsync(workspaceId, survivorId, absorbedId, userId, ct),
            MasterKind.Season => await ReassignSeasonAsync(workspaceId, survivorId, absorbedId, userId, ct),
            MasterKind.Worker => await ReassignWorkerAsync(workspaceId, survivorId, absorbedId, userId, ct),
            MasterKind.Task => await ReassignTaskAsync(workspaceId, survivorId, absorbedId, userId, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Maestro no contemplado.")
        };

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // ADR-0005 — alguien editó uno de los registros que se estaban reapuntando. La fusión no
            // se completa: es preferible repetirla a pisar en silencio la corrección de otra persona.
            throw new ConcurrencyConflictException(
                "Otra persona ha modificado uno de los registros mientras se fusionaban las fichas. " +
                "Vuelve a intentarlo.");
        }

        // CA-5, comprobado y no supuesto: si algún tipo de referencia se hubiera quedado sin reapuntar,
        // aquí se ve con un mensaje que lo dice, en lugar de en un 23503 de PostgreSQL al borrar.
        var pending = await CountUsageAsync(kind, workspaceId, absorbedId, ct);
        if (pending.IsUsed)
            throw new InvalidOperationException(
                $"La fusión de {MasterKinds.Singular(kind)} dejó sin reapuntar {pending.Describe()}.");

        await RemoveAsync(kind, workspaceId, absorbedId, ct);

        await transaction.CommitAsync(ct);

        return reassigned;
    }

    private async Task<int> ReassignPlotAsync(
        Guid workspaceId, Guid survivorId, Guid absorbedId, Guid userId, CancellationToken ct)
    {
        var activities = await db.Activities
            .Where(a => a.WorkspaceId == workspaceId && a.PlotId == absorbedId).ToListAsync(ct);
        foreach (var activity in activities) activity.ReassignPlot(survivorId, userId);

        var harvests = await db.Harvests
            .Where(h => h.WorkspaceId == workspaceId && h.PlotId == absorbedId).ToListAsync(ct);
        foreach (var harvest in harvests) harvest.ReassignPlot(survivorId, userId);

        var consumptions = await db.PurchaseConsumptions
            .Where(c => c.WorkspaceId == workspaceId && c.PlotId == absorbedId).ToListAsync(ct);
        foreach (var consumption in consumptions) consumption.ReassignPlot(survivorId, userId);

        return activities.Count + harvests.Count + consumptions.Count;
    }

    private async Task<int> ReassignSeasonAsync(
        Guid workspaceId, Guid survivorId, Guid absorbedId, Guid userId, CancellationToken ct)
    {
        var activities = await db.Activities
            .Where(a => a.WorkspaceId == workspaceId && a.SeasonId == absorbedId).ToListAsync(ct);
        foreach (var activity in activities) activity.ReassignSeason(survivorId, userId);

        var harvests = await db.Harvests
            .Where(h => h.WorkspaceId == workspaceId && h.SeasonId == absorbedId).ToListAsync(ct);
        foreach (var harvest in harvests) harvest.ReassignSeason(survivorId, userId);

        var purchases = await db.Purchases
            .Where(p => p.WorkspaceId == workspaceId && p.SeasonId == absorbedId).ToListAsync(ct);
        foreach (var purchase in purchases) purchase.ReassignSeason(survivorId, userId);

        var consumptions = await db.PurchaseConsumptions
            .Where(c => c.WorkspaceId == workspaceId && c.SeasonId == absorbedId).ToListAsync(ct);
        foreach (var consumption in consumptions) consumption.ReassignSeason(survivorId, userId);

        // La temporada de trabajo de quien tuviera fijada la absorbida (MVP-209). No cuenta como
        // registro reapuntado —no es histórico— pero se traslada igualmente: la FK es `SET NULL`, así
        // que no reapuntarla le devolvería al defecto sin que nadie se lo haya pedido.
        await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.ActiveSeasonId == absorbedId)
            .ExecuteUpdateAsync(set => set.SetProperty(m => m.ActiveSeasonId, survivorId), ct);

        return activities.Count + harvests.Count + purchases.Count + consumptions.Count;
    }

    private async Task<int> ReassignWorkerAsync(
        Guid workspaceId, Guid survivorId, Guid absorbedId, Guid userId, CancellationToken ct)
    {
        var activities = await db.Activities
            .Where(a => a.WorkspaceId == workspaceId && a.WorkerId == absorbedId).ToListAsync(ct);
        foreach (var activity in activities) activity.ReassignWorker(survivorId, userId);

        return activities.Count;
    }

    private async Task<int> ReassignTaskAsync(
        Guid workspaceId, Guid survivorId, Guid absorbedId, Guid userId, CancellationToken ct)
    {
        var activities = await db.Activities
            .Where(a => a.WorkspaceId == workspaceId && a.TaskId == absorbedId).ToListAsync(ct);
        foreach (var activity in activities) activity.ReassignTask(survivorId, userId);

        return activities.Count;
    }

    /// <summary>
    /// Borra la fila con un <c>DELETE</c> directo, acotando por Workspace igual que el resto de
    /// lecturas del maestro: el aislamiento multi-tenant no se relaja porque la operación sea interna.
    ///
    /// Va por <c>ExecuteDeleteAsync</c> y no por <c>Remove</c> + <c>SaveChanges</c> a propósito. Con el
    /// agregado en el rastreador, EF ve que un dependiente cargado se quedaría con una clave ajena
    /// obligatoria apuntando a nada y lanza «the association has been severed» <b>antes</b> de hablar
    /// con la base de datos: un fallo de infraestructura donde lo correcto es la respuesta de negocio
    /// que da la propia FK <c>RESTRICT</c>. Yendo directo al SQL, quien decide es la base de datos, que
    /// es la única que ve <b>todas</b> las referencias y no solo las que este contexto cargó.
    /// </summary>
    private async Task RemoveAsync(MasterKind kind, Guid workspaceId, Guid masterId, CancellationToken ct)
    {
        try
        {
            _ = kind switch
            {
                MasterKind.Plot => await db.Plots
                    .Where(p => p.WorkspaceId == workspaceId && p.Id == masterId)
                    .ExecuteDeleteAsync(ct),
                MasterKind.Season => await db.Seasons
                    .Where(s => s.WorkspaceId == workspaceId && s.Id == masterId)
                    .ExecuteDeleteAsync(ct),
                MasterKind.Worker => await db.Workers
                    .Where(w => w.WorkspaceId == workspaceId && w.Id == masterId)
                    .ExecuteDeleteAsync(ct),
                MasterKind.Task => await db.Tasks
                    .Where(t => t.WorkspaceId == workspaceId && t.Id == masterId)
                    .ExecuteDeleteAsync(ct),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Maestro no contemplado.")
            };
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            // Alguien registró algo entre la comprobación de uso y el borrado. La FK `RESTRICT` es la
            // red por debajo, igual que el índice único lo es de la guarda de nombres duplicados
            // (MVP-207): se traduce al mismo 422 en vez de a un 500.
            throw new MasterOperationException(
                ErrorCodes.BusinessRuleMasterInUse,
                $"Ya no se puede eliminar {MasterKinds.Article(kind)} {MasterKinds.Singular(kind)}: " +
                "acaba de registrarse algo que lo referencia.");
        }
    }
}
