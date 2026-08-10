using Terrenario.Api.Domain.Masters;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Quién puede referenciar a cada maestro. Es el <b>único</b> sitio donde eso está escrito, y de él
/// sale tanto el recuento de uso como la comprobación de cobertura.
///
/// El spec de MVP-806 lo dice sin rodeos: «la comprobación del sin uso es la parte delicada, no el
/// borrado […] comprobarlo contra una sola tabla es exactamente el fallo que dejaría un registro
/// huérfano». Repartir esa lista por cuatro repositorios garantizaría que la próxima entidad
/// operativa se añadiera en tres de ellos. Aquí está una vez, y
/// <c>MasterReferenceCoverageTests</c> falla si el modelo de EF declara una clave ajena hacia un
/// maestro que esta tabla no recoge.
///
/// Dos decisiones que conviene leer antes de tocar nada:
///
/// <list type="bullet">
///   <item>
///     <b>No se filtran los eliminados lógicamente.</b> Una actividad con <c>deleted_at</c> sigue
///     teniendo su <c>plot_id</c> apuntando aquí: la FK <c>RESTRICT</c> impediría el borrado físico
///     del terreno, así que contarla no es una precaución, es la verdad. Filtrar por «vivos» daría
///     un «sin uso» que la base de datos desmentiría con un 500.
///   </item>
///   <item>
///     <b><see cref="MasterReference.IsOperational"/> distingue histórico de preferencia.</b> La
///     temporada de trabajo de un miembro (<c>workspace_members.active_season_id</c>) referencia a
///     una temporada, pero no es un registro operativo: su FK es <c>ON DELETE SET NULL</c> y su
///     desaparición se resuelve sola cayendo al defecto de <c>WorkingSeasonPolicy</c>. Se declara —
///     para que la comprobación de cobertura la vea— pero no bloquea el borrado ni cuenta como uso.
///   </item>
/// </list>
/// </summary>
public static class MasterReferenceMap
{
    private static readonly IReadOnlyList<MasterReference> PlotReferences =
    [
        new("actividad", "actividades", typeof(Domain.Activities.Activity), "PlotId", true,
            (db, ws) => db.Activities.Where(a => a.WorkspaceId == ws).Select(a => a.PlotId)),
        new("cosecha", "cosechas", typeof(Domain.Harvests.Harvest), "PlotId", true,
            (db, ws) => db.Harvests.Where(h => h.WorkspaceId == ws).Select(h => h.PlotId)),
        // La que el spec señala como fácil de olvidar: el terreno también se referencia desde los
        // consumos, no solo desde el diario y la producción.
        new("consumo", "consumos", typeof(Domain.Consumptions.PurchaseConsumption), "PlotId", true,
            (db, ws) => db.PurchaseConsumptions.Where(c => c.WorkspaceId == ws).Select(c => c.PlotId))
    ];

    private static readonly IReadOnlyList<MasterReference> SeasonReferences =
    [
        new("actividad", "actividades", typeof(Domain.Activities.Activity), "SeasonId", true,
            (db, ws) => db.Activities.Where(a => a.WorkspaceId == ws).Select(a => a.SeasonId)),
        new("cosecha", "cosechas", typeof(Domain.Harvests.Harvest), "SeasonId", true,
            (db, ws) => db.Harvests.Where(h => h.WorkspaceId == ws).Select(h => h.SeasonId)),
        new("compra", "compras", typeof(Domain.Purchases.Purchase), "SeasonId", true,
            (db, ws) => db.Purchases.Where(p => p.WorkspaceId == ws).Select(p => p.SeasonId)),
        new("consumo", "consumos", typeof(Domain.Consumptions.PurchaseConsumption), "SeasonId", true,
            (db, ws) => db.PurchaseConsumptions.Where(c => c.WorkspaceId == ws).Select(c => c.SeasonId)),
        // Preferencia por usuario (MVP-209), no histórico: ver la nota de la clase.
        new("preferencia de temporada de trabajo", "preferencias de temporada de trabajo",
            typeof(Domain.Workspaces.WorkspaceMember), "ActiveSeasonId", false,
            (db, ws) => db.WorkspaceMembers
                .Where(m => m.WorkspaceId == ws && m.ActiveSeasonId != null)
                .Select(m => m.ActiveSeasonId!.Value))
    ];

    private static readonly IReadOnlyList<MasterReference> WorkerReferences =
    [
        new("actividad", "actividades", typeof(Domain.Activities.Activity), "WorkerId", true,
            (db, ws) => db.Activities.Where(a => a.WorkspaceId == ws).Select(a => a.WorkerId))
    ];

    private static readonly IReadOnlyList<MasterReference> TaskReferences =
    [
        // RN-025 — la tarea del catálogo es opcional en la actividad (la alternativa es texto libre),
        // así que solo cuentan las filas que traen `task_id`.
        new("actividad", "actividades", typeof(Domain.Activities.Activity), "TaskId", true,
            (db, ws) => db.Activities
                .Where(a => a.WorkspaceId == ws && a.TaskId != null)
                .Select(a => a.TaskId!.Value))
    ];

    /// <summary>Todas las referencias declaradas de un maestro, operativas y no operativas.</summary>
    public static IReadOnlyList<MasterReference> For(MasterKind kind) => kind switch
    {
        MasterKind.Plot => PlotReferences,
        MasterKind.Season => SeasonReferences,
        MasterKind.Worker => WorkerReferences,
        MasterKind.Task => TaskReferences,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Maestro no contemplado.")
    };

    /// <summary>El tipo de entidad de cada maestro, para casar el mapa con el modelo de EF.</summary>
    public static Type EntityTypeOf(MasterKind kind) => kind switch
    {
        MasterKind.Plot => typeof(Domain.Plots.Plot),
        MasterKind.Season => typeof(Domain.Seasons.Season),
        MasterKind.Worker => typeof(Domain.Workers.Worker),
        MasterKind.Task => typeof(Domain.Tasks.TaskItem),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Maestro no contemplado.")
    };
}

/// <summary>
/// Una forma de referenciar a un maestro desde otra tabla.
/// </summary>
/// <param name="SingularLabel">Cómo se nombra en el mensaje de error cuando hay una: «actividad».</param>
/// <param name="PluralLabel">Cómo se nombra cuando hay varias: «actividades».</param>
/// <param name="EntityType">Entidad que referencia. Solo lo usa la comprobación de cobertura.</param>
/// <param name="ForeignKey">Propiedad de la clave ajena. Solo lo usa la comprobación de cobertura.</param>
/// <param name="IsOperational">
/// ¿Es histórico? Si lo es, su existencia impide el borrado físico del maestro y cuenta en el
/// recuento que ve el usuario. Si no, se reapunta en la fusión pero no bloquea nada.
/// </param>
/// <param name="ReferencedIds">
/// Identificadores de maestro referenciados por cada fila del Workspace. Devolver los <b>ids</b> y no
/// las filas es lo que permite servir con la misma declaración el recuento de una fila
/// (<c>Where(id == x).Count()</c>) y el de todo el maestro (<c>GroupBy(id)</c>).
/// </param>
public sealed record MasterReference(
    string SingularLabel,
    string PluralLabel,
    Type EntityType,
    string ForeignKey,
    bool IsOperational,
    Func<TerrenarioDbContext, Guid, IQueryable<Guid>> ReferencedIds);
