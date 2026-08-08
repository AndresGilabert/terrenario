using Terrenario.Api.Domain.Diary;

namespace Terrenario.Api.Application.Diary;

/// <summary>Resultado del diario: la página pedida, sus totales y la posición dentro del conjunto.</summary>
public sealed record DiaryResult(
    IReadOnlyList<DiaryEntry> Entries,
    DiaryTotals Totals,
    int Page,
    int Limit);

/// <summary>
/// MVP-305 · MVP-506 — Diario cronológico unificado del Workspace (RN-033). Mezcla las cuatro
/// entidades operativas del MVP en una sola secuencia ordenada por <b>fecha de negocio</b>, que es lo
/// que convierte la aplicación en «una experiencia tipo diario» y no en cuatro listados aislados.
///
/// <b>MVP-506 mueve la mezcla a SQL.</b> Hasta entonces se hacía en memoria sobre lo que devolvían los
/// cuatro puertos operativos: era equivalente mientras no hubiera paginación —en los dos casos se
/// traían todas las filas del rango— pero deja de serlo en cuanto la hay. Paginar sobre cuatro listas
/// ya materializadas no es paginar (`P-051`), y buscar sobre una página no es buscar (`P-052`).
///
/// Lo que queda aquí es lo que no sabe hacer SQL: derivar el aviso de fecha fuera de temporada
/// (RN-023) sobre la página ya traída, que son como mucho <c>limit</c> filas.
/// </summary>
public sealed class DiaryQueryService(IDiaryRepository diaryRepository)
{
    public async Task<DiaryResult> HandleAsync(
        Guid workspaceId,
        DiaryFilter filter,
        DiaryPageRequest page,
        CancellationToken ct = default)
    {
        var rows = await diaryRepository.ListPageAsync(workspaceId, filter, page, ct);
        var totals = await diaryRepository.GetTotalsAsync(workspaceId, filter, ct);

        return new DiaryResult(
            rows.Select(ToEntry).ToList(),
            totals,
            page.Page,
            page.Limit);
    }

    private static DiaryEntry ToEntry(DiaryRow row) => new(
        row.Type,
        row.Id,
        row.Date,
        row.Title,
        row.Description,
        row.PlotId,
        row.PlotName,
        row.SeasonId,
        row.SeasonName,
        row.Cost,
        row.Version,
        IsOutOfSeasonRange(row),
        row.CreatedAt,
        WorkerName: row.WorkerName,
        Hours: row.Hours,
        TaskId: row.TaskId,
        Quantity: row.Quantity,
        HasPurchase: row.HasPurchase,
        Kgs: row.Kgs,
        Destination: row.Destination,
        Yield: row.Yield,
        Amount: row.Amount);

    /// <summary>
    /// RN-023 — la fecha cae fuera del rango de su temporada. Es un <b>aviso</b>, nunca un bloqueo, y
    /// se deriva en lectura para que la UI pueda marcarlo también en registros antiguos. Una temporada
    /// sin fecha de fin está abierta: nada cae después de ella.
    /// </summary>
    private static bool IsOutOfSeasonRange(DiaryRow row)
        => row.Date < row.SeasonStartDate
           || (row.SeasonEndDate is { } end && row.Date > end);
}
