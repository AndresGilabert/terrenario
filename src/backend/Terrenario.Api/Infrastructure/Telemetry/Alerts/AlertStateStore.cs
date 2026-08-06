using System.Collections.Concurrent;

namespace Terrenario.Api.Infrastructure.Telemetry.Alerts;

/// <summary>
/// MVP-603 — Estado vigente de cada alerta. Lo escribe la vigilancia y lo lee la revisión operativa.
///
/// Existe para que la alerta se avise **en la transición** y no en cada pasada: sin memoria de estado,
/// una degradación de dos horas mandaría ciento veinte correos idénticos y el canal de incidentes
/// dejaría de leerse justo cuando hace falta.
/// </summary>
public sealed class AlertStateStore
{
    private readonly ConcurrentDictionary<string, AlertState> _states = new(StringComparer.Ordinal);

    /// <summary>
    /// Aplica el veredicto y devuelve la transición ocurrida, o <c>null</c> si el estado no ha cambiado.
    /// </summary>
    public AlertTransition? Apply(AlertVerdict verdict, DateTimeOffset now)
    {
        var previous = _states.GetValueOrDefault(verdict.Name);
        var wasFiring = previous?.IsFiring ?? false;

        var firingSince = verdict.IsFiring
            ? (wasFiring ? previous!.FiringSince : now)
            : null;

        _states[verdict.Name] = new AlertState(
            verdict.Name, verdict.Severity, verdict.IsFiring, verdict.Detail, firingSince);

        if (verdict.IsFiring == wasFiring) return null;

        return verdict.IsFiring
            ? new AlertTransition(verdict, Started: true, Duration: TimeSpan.Zero)
            : new AlertTransition(verdict, Started: false,
                Duration: previous?.FiringSince is { } since ? now - since : TimeSpan.Zero);
    }

    /// <summary>Todas las alertas conocidas, disparadas primero y por severidad descendente.</summary>
    public IReadOnlyList<AlertState> Current() =>
        [.. _states.Values
            .OrderByDescending(a => a.IsFiring)
            .ThenByDescending(a => a.Severity)
            .ThenBy(a => a.Name, StringComparer.Ordinal)];

    public IReadOnlyList<AlertState> Firing() => [.. Current().Where(a => a.IsFiring)];
}

public sealed record AlertTransition(AlertVerdict Verdict, bool Started, TimeSpan Duration);
