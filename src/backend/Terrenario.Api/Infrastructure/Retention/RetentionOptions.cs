namespace Terrenario.Api.Infrastructure.Retention;

/// <summary>
/// MVP-504 (B-3) — Cadencia de la rutina de expurgo. Los <b>plazos</b> no se configuran aquí a
/// propósito: los 24 meses y los 30 días de los tokens de refresco son <c>RN-041</c> y viven en
/// <c>AccountRetentionPolicy</c>. Lo que se configura es cada cuánto se mira, que es una decisión de
/// operación, no de negocio.
/// </summary>
public sealed class RetentionOptions
{
    public const string SectionName = "Retention";

    /// <summary>
    /// Permite apagarla. Se desactiva en los tests de integración de la API: una rutina que borra
    /// filas por su cuenta mientras se ejercitan otros casos haría los fallos irreproducibles.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cada cuánto se ejecuta. Diaria: el plazo más corto son los 30 días de los tokens de refresco
    /// (MVP-714), así que un día de holgura es un 3 % del plazo. No hace falta más fino.
    /// </summary>
    public int IntervalHours { get; set; } = 24;

    /// <summary>
    /// Margen antes de la primera pasada. Arrancar y ponerse a borrar compite con el tráfico del
    /// despliegue justo cuando más importa que la aplicación responda.
    /// </summary>
    public int InitialDelayMinutes { get; set; } = 5;

    public TimeSpan Interval => TimeSpan.FromHours(Math.Max(1, IntervalHours));

    public TimeSpan InitialDelay => TimeSpan.FromMinutes(Math.Max(0, InitialDelayMinutes));
}
