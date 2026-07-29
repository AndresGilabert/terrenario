using Terrenario.Api.Domain.Activities;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Purchases;

namespace Terrenario.Api.Application.Diary;

/// <summary>Filtros del diario (<c>GET /api/v1/diary</c>).</summary>
public sealed record DiaryFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? PlotId = null,
    Guid? SeasonId = null,
    /// <summary>Tipos a incluir; vacío ⇒ todos (catálogo <c>diary_entry_type</c>).</summary>
    IReadOnlyCollection<string>? Types = null);

/// <summary>Resultado del diario: las entradas y el resumen que la cabecera necesita.</summary>
public sealed record DiaryResult(
    IReadOnlyList<DiaryEntry> Entries,
    int TotalActivities,
    int TotalPurchases,
    int TotalConsumptions,
    decimal TotalCost,
    /// <summary>Consumos sin compra previa: el impacto en la calidad del dato queda visible (CA-3 de la épica).</summary>
    int ConsumptionsWithoutPurchase);

/// <summary>
/// MVP-305 — Diario cronológico unificado del Workspace (RN-033, CA-1/CA-2). Mezcla las tres
/// entidades operativas del MVP en una sola secuencia ordenada por **fecha de negocio**, que es lo
/// que convierte la aplicación en «una experiencia tipo diario» y no en tres listados aislados.
///
/// <b>La mezcla se hace en memoria</b>, sobre lo que devuelven los tres puertos ya existentes, en vez
/// de con un <c>UNION</c> en SQL. Es una decisión consciente y acotada:
/// <list type="bullet">
/// <item>Reutiliza los repositorios tal cual, con su filtro de baja lógica y sus proyecciones ya
/// probadas; un <c>UNION</c> obligaría a una cuarta consulta que duplicaría esas reglas.</item>
/// <item>El diario **todavía no pagina** (`MVP-999`, `P-051`), así que en los dos casos se traen
/// todas las filas del rango: la diferencia es de forma, no de volumen.</item>
/// <item>Cuando se resuelva `P-051` habrá que mover la mezcla a SQL, porque paginar sobre tres
/// listas ya materializadas no es paginar. Queda anotado ahí.</item>
/// </list>
///
/// Añadir la cosecha en <c>MVP-401</c> es un cuarto puerto y un cuarto proyector: ni la forma de la
/// entrada ni el orden cambian (hallazgo <c>G-4</c>).
/// </summary>
public sealed class DiaryQueryService(
    IActivityRepository activityRepository,
    IPurchaseRepository purchaseRepository,
    IConsumptionRepository consumptionRepository)
{
    public async Task<DiaryResult> HandleAsync(
        Guid workspaceId,
        DiaryFilter filter,
        CancellationToken ct = default)
    {
        var wantsActivities = Includes(filter, DiaryEntryTypes.Activity);
        var wantsPurchases = Includes(filter, DiaryEntryTypes.Purchase);
        var wantsConsumptions = Includes(filter, DiaryEntryTypes.Consumption);

        // Solo se consulta lo que se va a mostrar: filtrar por tipo debe ahorrar trabajo, no solo
        // ocultarlo después.
        var activities = wantsActivities
            ? await activityRepository.ListAsync(
                workspaceId, new ActivityFilter(filter.From, filter.To, filter.PlotId, filter.SeasonId), ct)
            : [];

        // Una compra no se imputa a un terreno concreto (eso es el consumo, MVP-304), así que
        // filtrar el diario por terreno la deja fuera por definición, no por olvido.
        var purchases = wantsPurchases && filter.PlotId is null
            ? await purchaseRepository.ListAsync(
                workspaceId, new PurchaseFilter(null, filter.SeasonId, filter.From, filter.To), ct)
            : [];

        var consumptions = wantsConsumptions
            ? await consumptionRepository.ListAsync(
                workspaceId, new ConsumptionFilter(filter.From, filter.To, filter.PlotId, filter.SeasonId), ct)
            : [];

        var entries = activities.Select(ToEntry)
            .Concat(purchases.Select(ToEntry))
            .Concat(consumptions.Select(ToEntry))
            // Fecha de negocio descendente (RN-033) y, a igualdad, lo capturado más tarde primero:
            // es el orden en que la persona recuerda haberlo apuntado.
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.CreatedAt)
            .ToList();

        return new DiaryResult(
            entries,
            activities.Count,
            purchases.Count,
            consumptions.Count,
            entries.Sum(entry => entry.Cost),
            consumptions.Count(consumption => !consumption.HasPurchase));
    }

    private static bool Includes(DiaryFilter filter, string type)
        => filter.Types is null || filter.Types.Count == 0 || filter.Types.Contains(type);

    private static DiaryEntry ToEntry(ActivityView activity) => new(
        DiaryEntryTypes.Activity,
        activity.Id,
        activity.Date,
        activity.Task,
        activity.Description,
        activity.PlotId,
        activity.PlotName,
        activity.SeasonId,
        activity.SeasonName,
        activity.ManualCost,
        activity.Version,
        activity.IsOutOfSeasonRange,
        activity.CreatedAt,
        WorkerName: activity.WorkerName,
        Hours: activity.Hours,
        TaskId: activity.TaskId);

    private static DiaryEntry ToEntry(PurchaseView purchase) => new(
        DiaryEntryTypes.Purchase,
        purchase.Id,
        purchase.PurchaseDate,
        purchase.Product,
        null,
        // Una compra es del Workspace, no de un terreno: el reparto por terrenos es la imputación.
        null,
        null,
        purchase.SeasonId,
        purchase.SeasonName,
        purchase.TotalCost,
        purchase.Version,
        purchase.IsOutOfSeasonRange,
        purchase.CreatedAt,
        Quantity: purchase.TotalQuantity);

    private static DiaryEntry ToEntry(ConsumptionView consumption) => new(
        DiaryEntryTypes.Consumption,
        consumption.Id,
        consumption.Date,
        consumption.Product,
        null,
        consumption.PlotId,
        consumption.PlotName,
        consumption.SeasonId,
        consumption.SeasonName,
        consumption.ProportionalCost,
        consumption.Version,
        consumption.IsOutOfSeasonRange,
        consumption.CreatedAt,
        Quantity: consumption.ConsumedQuantity,
        HasPurchase: consumption.HasPurchase);
}
