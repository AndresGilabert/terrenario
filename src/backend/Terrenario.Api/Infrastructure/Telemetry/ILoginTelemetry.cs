namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// Emisor de los cinco eventos del embudo de login que exige la KB (RN-020). Desde MVP-601 cada
/// evento viaja con las dimensiones mínimas completas, no solo con el <c>flow_id</c>.
/// </summary>
public interface ILoginTelemetry
{
    void LoginScreenViewed(LoginEventContext context);
    void LoginGoogleClicked(LoginEventContext context);
    void LoginSuccess(LoginEventContext context);
    void LoginError(LoginEventContext context, string errorCode);
    void LoginAbandoned(LoginEventContext context);
}
