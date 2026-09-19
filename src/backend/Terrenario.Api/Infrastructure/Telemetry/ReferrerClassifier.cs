namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MKT-106 (CA-2) — Clasifica de dónde viene una visita a partir del <c>Referer</c>/<c>document.referrer</c>
/// que manda el cliente, en un puñado de cubos agregados: nunca se conserva el valor en crudo.
///
/// <b>Primera parte y sin perfilado</b> (RN-042, ADR-0011): un dominio externo agregado
/// (<c>external.google.com</c>) no identifica a nadie, igual que hoy no lo hace <c>device_type</c>.
/// Un valor ausente o mal formado **no rechaza el evento**: se degrada a <see cref="Direct"/>, mismo
/// criterio que el resto de dimensiones secundarias del embudo.
/// </summary>
public static class ReferrerClassifier
{
    public const string Direct = "direct";
    public const string Internal = "internal";

    private const string ExternalPrefix = "external.";
    private const string LandingPrefix = "landing.";

    /// <summary>
    /// Longitud máxima del dominio externo saneado. Un dominio es texto que no controla el servidor,
    /// así que no puede convertirse en nombre de métrica sin acotar (mismo motivo que
    /// <c>TelemetryMetrics.LoginErrorFor</c> sanea el código de error).
    /// </summary>
    private const int ExternalHostMaxLength = 64;

    public static string Classify(string? referrer, string requestHost, string webRootPath)
    {
        if (string.IsNullOrWhiteSpace(referrer)) return Direct;
        if (!Uri.TryCreate(referrer, UriKind.Absolute, out var uri)) return Direct;
        if (uri.Scheme is not ("http" or "https")) return Direct;

        if (string.Equals(uri.Host, StripPort(requestHost), StringComparison.OrdinalIgnoreCase))
        {
            var landing = LandingCatalog.TryClassifyReferrerPath(webRootPath, uri.AbsolutePath);
            return landing is null ? Internal : $"{LandingPrefix}{landing}";
        }

        return $"{ExternalPrefix}{SanitizeHost(uri.Host)}";
    }

    private static string StripPort(string host)
    {
        var separator = host.IndexOf(':');
        return separator < 0 ? host : host[..separator];
    }

    private static string SanitizeHost(string host)
    {
        var withoutWww = host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;

        var sanitized = new string(withoutWww
            .Where(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-')
            .Take(ExternalHostMaxLength)
            .ToArray())
            .ToLowerInvariant();

        return sanitized.Length == 0 ? "unknown" : sanitized;
    }
}
