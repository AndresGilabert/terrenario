namespace Terrenario.Api.Infrastructure.Telemetry;

public interface ILoginTelemetry
{
    void LoginScreenViewed(string flowId);
    void LoginGoogleClicked(string flowId);
    void LoginSuccess(string flowId);
    void LoginError(string flowId, string errorCode);
    void LoginAbandoned(string flowId);
}
