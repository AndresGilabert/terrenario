namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-601 — Cadencia del volcado de contadores y cuánto histórico se conserva. Las **métricas** no se
/// configuran aquí: qué se mide lo fija la KB, no un ajuste de despliegue.
/// </summary>
public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    /// <summary>
    /// Permite apagar el volcado. Se desactiva en los tests de integración: un servicio de fondo
    /// escribiendo por su cuenta mientras se ejercitan otros casos hace los fallos irreproducibles,
    /// que es la misma razón por la que se apaga el expurgo de <c>RN-041</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cada cuánto se vuelca lo acumulado. Un minuto es el equilibrio entre no escribir por evento y
    /// no perder mucho si el proceso se cae: el reinicio de un despliegue cuesta, como mucho, la
    /// última ventana.
    /// </summary>
    public int FlushIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Días de histórico que se conservan. Por defecto algo más de un año: cubre de sobra la ventana
    /// de 30 días del SLO de disponibilidad y deja comparar con la campaña anterior. No es un plazo de
    /// <c>RN-041</c> —aquí no hay datos personales— sino higiene de tabla.
    /// </summary>
    public int RetentionDays { get; set; } = 400;

    public TimeSpan FlushInterval => TimeSpan.FromSeconds(Math.Clamp(FlushIntervalSeconds, 5, 3600));
}
