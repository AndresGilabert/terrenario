using Terrenario.Api.Domain.Diary;

namespace Terrenario.Api.Application.Dashboard;

/// <summary>
/// MVP-707 — Lectura económica mínima de la campaña: cuánto ha salido y cuánto ha entrado.
/// </summary>
/// <param name="Expense">
/// Gasto del ámbito: labores + compras + consumos sin compra. Las imputaciones quedan fuera porque
/// reparten dinero que la compra ya aportó (<c>R-01</c> de MVP-399).
/// </param>
/// <param name="Income">
/// Ingreso del ámbito: la suma de <c>kilos × precio</c> de las cosechas que tienen precio.
/// <c>null</c> cuando **ninguna** lo tiene: la campaña no ha ingresado 0 €, es que no se sabe (CA-5).
/// </param>
/// <param name="Harvests">Partidas del ámbito.</param>
/// <param name="HarvestsWithPrice">Cuántas de ellas llevan precio, para poder decir sobre cuántas se suma.</param>
public sealed record DashboardEconomics(
    DashboardScope Scope,
    decimal Expense,
    decimal? Income,
    int Harvests,
    int HarvestsWithPrice);

/// <summary>
/// MVP-707 — Resuelve la lectura económica del panel.
///
/// <b>No calcula el gasto: se lo pregunta al diario.</b> El diario es donde el producto decidió qué
/// cuenta como gasto y qué no —las imputaciones fuera, los consumos sin compra dentro— y esa decisión
/// costó un hallazgo (<c>R-01</c> de MVP-399, doble contabilización). Reimplementarla aquí crearía dos
/// verdades sobre el mismo dinero, que es exactamente cómo nació <c>P-082</c> con el ámbito de
/// temporada. Preguntándoselo, las cifras del panel y las de la cabecera del diario **no pueden**
/// discrepar (CA-4).
///
/// El filtro de terrenos se pasa tal y como llegó, no el del ámbito ya resuelto: el ámbito rellena
/// «todos los activos» por defecto, y acotar por terrenos deja las compras fuera —una compra es del
/// Workspace, no de un terreno—. Distinguir «no he filtrado» de «he filtrado por todos» es lo que
/// hace que el gasto por defecto incluya las compras, igual que en el diario sin filtro de terreno.
/// </summary>
public sealed class DashboardEconomicsService(
    DashboardScopeResolver scopeResolver,
    IDiaryRepository diaryRepository)
{
    public async Task<DashboardEconomics> HandleAsync(
        Guid userId,
        Guid workspaceId,
        DashboardRequest request,
        CancellationToken ct = default)
    {
        var scope = await scopeResolver.ResolveAsync(userId, workspaceId, request, ct);

        if (!scope.IsResolvable)
            return new DashboardEconomics(scope, Expense: 0m, Income: null, Harvests: 0, HarvestsWithPrice: 0);

        var totals = await diaryRepository.GetTotalsAsync(
            workspaceId,
            new DiaryFilter(
                PlotIds: request.PlotIds is { Count: > 0 } requested ? requested : null,
                SeasonId: scope.Season!.Id),
            ct);

        return new DashboardEconomics(
            scope,
            totals.TotalCost,
            totals.TotalIncome,
            totals.Harvests,
            totals.HarvestsWithPrice);
    }
}
