namespace Terrenario.Api.Infrastructure.Telemetry.Alerts;

/// <summary>
/// MVP-603 — Cómo sale una alerta del proceso. La KB declara «canal privado interno de incidentes»;
/// aquí se materializa como traza estructurada siempre y, si hay cuenta de envío y destinatario
/// configurados, además como correo.
/// </summary>
public interface IAlertNotifier
{
    /// <summary>La alerta acaba de pasar a disparada.</summary>
    Task NotifyFiringAsync(AlertVerdict verdict, CancellationToken ct);

    /// <summary>La alerta se ha resuelto. Se avisa también: una alerta que nadie cierra no informa de nada.</summary>
    Task NotifyResolvedAsync(AlertVerdict verdict, TimeSpan duration, CancellationToken ct);
}

/// <summary>Estado vivo de una alerta, tal y como lo expone la revisión operativa.</summary>
public sealed record AlertState(
    string Name,
    AlertSeverity Severity,
    bool IsFiring,
    string Detail,
    DateTimeOffset? FiringSince);
