namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-601 — Fila de la tabla <c>telemetry_daily_counters</c>: un contador, un día, un valor.
///
/// <b>Por qué agregados y no una traza de eventos</b>: los SLO piden ventanas de 7 y 30 días, que en
/// App Service no se pueden calcular sobre logs (no se retienen de forma fiable), y una tabla con una
/// fila por evento habría añadido una categoría de dato conservado a <c>RN-041</c> y al inventario de
/// <c>RN-042</c>. Un contador diario responde a todos los KPI de la KB y no conserva ningún
/// identificador: no hay nada que reidentificar ni nada que expurgar por derecho de supresión.
/// </summary>
public sealed class TelemetryDailyCounter
{
    public DateOnly Date { get; set; }
    public string Metric { get; set; } = string.Empty;
    public long Value { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
