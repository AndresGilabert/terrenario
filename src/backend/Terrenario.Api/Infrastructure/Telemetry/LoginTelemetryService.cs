namespace Terrenario.Api.Infrastructure.Telemetry;

public sealed class LoginTelemetryService(ILogger<LoginTelemetryService> logger) : ILoginTelemetry
{
    private const string Channel = "web";

    public void LoginScreenViewed(string flowId) =>
        LogEvent(LoginFunnelEvents.ScreenViewed, flowId);

    public void LoginGoogleClicked(string flowId) =>
        LogEvent(LoginFunnelEvents.GoogleClicked, flowId);

    public void LoginSuccess(string flowId) =>
        LogEvent(LoginFunnelEvents.Success, flowId);

    public void LoginError(string flowId, string errorCode) =>
        LogEvent(LoginFunnelEvents.Error, flowId, errorCode);

    public void LoginAbandoned(string flowId) =>
        LogEvent(LoginFunnelEvents.Abandonment, flowId);

    private void LogEvent(string eventName, string flowId, string? errorCode = null)
    {
        if (errorCode is not null)
        {
            logger.LogInformation(
                "auth.funnel event={Event} flow_id={FlowId} channel={Channel} error_code={ErrorCode}",
                eventName, flowId, Channel, errorCode);
        }
        else
        {
            logger.LogInformation(
                "auth.funnel event={Event} flow_id={FlowId} channel={Channel}",
                eventName, flowId, Channel);
        }
    }
}
