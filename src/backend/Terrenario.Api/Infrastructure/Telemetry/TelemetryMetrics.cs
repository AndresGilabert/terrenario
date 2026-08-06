namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-601 — Nombres canónicos de los contadores agregados. Centralizarlos evita que quien suma y
/// quien lee (la revisión semanal de KPIs, y las señales de <c>MVP-603</c>) se desincronicen, que es
/// exactamente lo que ya pasó con los nombres de los eventos y resolvió <see cref="LoginFunnelEvents"/>.
///
/// <b>Solo contadores</b>: ni una fila por evento ni un identificador conservado. Es lo que permite
/// calcular las ventanas de 7 y 30 días que piden los SLO sin retener nada de nadie (RN-042).
/// </summary>
public static class TelemetryMetrics
{
    public const string LoginScreenViewed = "login.screen_viewed";
    public const string LoginGoogleClicked = "login.google_clicked";
    public const string LoginSuccess = "login.success";
    public const string LoginError = "login.error";
    public const string LoginAbandonment = "login.abandonment";

    /// <summary>
    /// Suma de duraciones de los logins con éxito, en milisegundos. Con
    /// <see cref="LoginSuccess"/> como divisor da el «tiempo medio de login exitoso» del SLO
    /// (&lt;= 45 s). Se guarda como suma y no como media porque las medias no se pueden agregar entre
    /// días: la de la semana no es la media de las medias diarias.
    /// </summary>
    public const string LoginSuccessDurationMsSum = "login.success.duration_ms.sum";

    /// <summary>
    /// Logins con éxito de los que **sí** se conocía el instante de entrada a la pantalla, que es el
    /// divisor correcto de <see cref="LoginSuccessDurationMsSum"/>. No coincide con
    /// <see cref="LoginSuccess"/>: si la aplicación se reinicia a mitad de un intento, ese éxito se
    /// cuenta pero su duración no se conoce, y dividir por el total daría una media falsamente baja.
    /// </summary>
    public const string LoginSuccessTimedCount = "login.success.timed";

    private const int ErrorCodeMaxLength = 48;

    /// <summary>
    /// Contador por código de error (<c>login.error.auth_google_token_invalid</c>). El código viene de
    /// <c>ErrorCodes</c>, que es un conjunto cerrado, pero se sanea igualmente: un contador cuyo nombre
    /// dependa de texto no acotado deja de ser un contador y pasa a ser una fuga de cardinalidad.
    /// </summary>
    public static string LoginErrorFor(string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
            return $"{LoginError}.{TelemetryDimensions.Unknown}";

        var saneado = new string(errorCode
            .Where(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-')
            .Take(ErrorCodeMaxLength)
            .ToArray())
            .ToLowerInvariant();

        return saneado.Length == 0
            ? $"{LoginError}.{TelemetryDimensions.Unknown}"
            : $"{LoginError}.{saneado}";
    }
}
