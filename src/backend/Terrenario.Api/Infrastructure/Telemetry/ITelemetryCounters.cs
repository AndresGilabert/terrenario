namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-601 — Acumulador de los contadores agregados del día. Sumar en memoria y volcar cada poco es
/// lo que permite medir sin una escritura en base de datos por evento: en el peor caso se pierde la
/// ventana sin volcar, que para una medida de producto es un precio aceptable y para nadie es un dato
/// que reclamar.
/// </summary>
public interface ITelemetryCounters
{
    /// <summary>Suma <paramref name="delta"/> al contador del día en curso (UTC).</summary>
    void Add(string metric, long delta = 1);
}

/// <summary>Contador de un día concreto, tal y como se vuelca y se lee.</summary>
public sealed record TelemetryCounter(DateOnly Date, string Metric, long Value);
