namespace Terrenario.Api.Domain.Seasons;

/// <summary>
/// Regla de defecto de la <b>temporada de trabajo</b> de un usuario (MVP-209) cuando no tiene ninguna
/// fijada en su membresía (miembro nuevo, o la que tenía se borró). Es lógica de dominio pura —una
/// política de selección sobre un conjunto de temporadas— aislada para poder probarla de forma
/// determinista pasándole «hoy».
///
/// Orden de preferencia:
/// <list type="number">
///   <item>La campaña <b>abierta que contiene hoy</b> (iniciada, no cerrada, y sin fin o con fin
///   posterior): es «la de ahora».</item>
///   <item>Si no hay, la <b>abierta más reciente</b> (iniciada y no cerrada), que cubre el caso de
///   entre campañas o de una pasada aún sin cerrar.</item>
///   <item>Si tampoco, la <b>más reciente</b> por fecha de inicio (todas planificadas o cerradas): algo
///   que ofrecer es mejor que nada.</item>
///   <item><c>null</c> si el Workspace no tiene temporadas.</item>
/// </list>
/// </summary>
public static class WorkingSeasonPolicy
{
    public static Season? ResolveDefault(IEnumerable<Season> seasons, DateOnly today)
    {
        var all = seasons.ToList();
        if (all.Count == 0) return null;

        Season? containsToday = all
            .Where(s => s.StatusOn(today) == SeasonStatus.Abierta
                        && (s.EndDate is null || today <= s.EndDate))
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();
        if (containsToday is not null) return containsToday;

        Season? mostRecentOpen = all
            .Where(s => s.StatusOn(today) == SeasonStatus.Abierta)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();
        if (mostRecentOpen is not null) return mostRecentOpen;

        return all.OrderByDescending(s => s.StartDate).First();
    }
}
