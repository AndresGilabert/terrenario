namespace Terrenario.Api.Infrastructure.Telemetry;

public sealed class LoginTelemetryService(ILogger<LoginTelemetryService> logger) : ILoginTelemetry
{
    private const string Channel = "web";

    public void LoginScreenViewed(string flowId) =>
        LogEvent("login_screen_viewed", flowId);

    public void LoginGoogleClicked(string flowId) =>
        LogEvent("login_google_clicked", flowId);

    public void LoginSuccess(string flowId) =>
        LogEvent("login_google_success", flowId);

    public void LoginError(string flowId, string errorCode) =>
        LogEvent("login_google_error", flowId, errorCode);

    public void LoginAbandoned(string flowId) =>
        LogEvent("login_abandonment", flowId);

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
