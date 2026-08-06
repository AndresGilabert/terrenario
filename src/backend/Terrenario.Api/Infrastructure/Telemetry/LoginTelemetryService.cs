namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-601 — Emite los cinco eventos del embudo con las dimensiones mínimas de la KB y, en el mismo
/// paso, los suma a los contadores agregados que sostienen los KPI y las señales de <c>MVP-603</c>.
///
/// <b>Dos salidas y no una</b>: el log deja la traza individual para diagnosticar un caso concreto
/// mientras esté a mano; el contador deja la serie con la que se miran siete o treinta días. Ninguna
/// de las dos sustituye a la otra y ninguna conserva PII (RN-020, RN-042).
/// </summary>
public sealed class LoginTelemetryService(
    ILogger<LoginTelemetryService> logger,
    ITelemetryCounters counters,
    LoginFlowTimings timings,
    TimeProvider clock) : ILoginTelemetry
{
    public void LoginScreenViewed(LoginEventContext context)
    {
        timings.Start(context.FlowId);
        Emit(LoginFunnelEvents.ScreenViewed, context, TelemetryMetrics.LoginScreenViewed);
    }

    public void LoginGoogleClicked(LoginEventContext context)
        => Emit(LoginFunnelEvents.GoogleClicked, context, TelemetryMetrics.LoginGoogleClicked);

    public void LoginSuccess(LoginEventContext context)
    {
        var elapsed = timings.Complete(context.FlowId);

        Emit(LoginFunnelEvents.Success, context, TelemetryMetrics.LoginSuccess, elapsed: elapsed);

        // Solo los intentos de los que se conoce el inicio entran en la media (ver `LoginSuccessTimedCount`).
        if (elapsed is { } duration)
        {
            counters.Add(TelemetryMetrics.LoginSuccessTimedCount);
            counters.Add(TelemetryMetrics.LoginSuccessDurationMsSum, (long)duration.TotalMilliseconds);
        }
    }

    public void LoginError(LoginEventContext context, string errorCode)
    {
        timings.Discard(context.FlowId);

        Emit(LoginFunnelEvents.Error, context, TelemetryMetrics.LoginError, errorCode);
        counters.Add(TelemetryMetrics.LoginErrorFor(errorCode));
    }

    public void LoginAbandoned(LoginEventContext context)
    {
        timings.Discard(context.FlowId);
        Emit(LoginFunnelEvents.Abandonment, context, TelemetryMetrics.LoginAbandonment);
    }

    /// <summary>
    /// El <c>timestamp</c> viaja como propiedad del evento y no solo como marca del renglón de log: la
    /// dimensión la exige la KB, y depender del formateador del proveedor de logs para tenerla la haría
    /// desaparecer en cuanto ese formateador cambie.
    /// </summary>
    private void Emit(
        string eventName,
        LoginEventContext context,
        string metric,
        string? errorCode = null,
        TimeSpan? elapsed = null)
    {
        counters.Add(metric);

        var timestamp = clock.GetUtcNow().ToString("O");

        if (errorCode is not null)
        {
            logger.LogInformation(
                "auth.funnel event={Event} timestamp={Timestamp} session_id={SessionId} flow_id={FlowId} "
                + "channel={Channel} device_type={DeviceType} error_code={ErrorCode}",
                eventName, timestamp, context.SessionId, context.FlowId,
                context.Channel, context.DeviceType, errorCode);
        }
        else if (elapsed is { } duration)
        {
            logger.LogInformation(
                "auth.funnel event={Event} timestamp={Timestamp} session_id={SessionId} flow_id={FlowId} "
                + "channel={Channel} device_type={DeviceType} elapsed_ms={ElapsedMs}",
                eventName, timestamp, context.SessionId, context.FlowId,
                context.Channel, context.DeviceType, (long)duration.TotalMilliseconds);
        }
        else
        {
            logger.LogInformation(
                "auth.funnel event={Event} timestamp={Timestamp} session_id={SessionId} flow_id={FlowId} "
                + "channel={Channel} device_type={DeviceType}",
                eventName, timestamp, context.SessionId, context.FlowId,
                context.Channel, context.DeviceType);
        }
    }
}
