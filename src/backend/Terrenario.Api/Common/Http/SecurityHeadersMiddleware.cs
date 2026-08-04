using System.IO;

namespace Terrenario.Api.Common.Http;

/// <summary>
/// Añade a todas las respuestas los headers de seguridad HTTP exigidos por
/// <c>docs/07-seguridad/autenticacion-autorizacion.md</c>. Se registra al principio del pipeline
/// para cubrir también las respuestas de error cortadas por filtros o middlewares posteriores
/// (MVP-105 / P-005). Los valores se fijan con el indexador (sobrescriben) para no duplicar headers.
///
/// <b>Publicación en un solo origen</b> — Desde que la propia API sirve también el cliente, esto
/// emite <b>dos políticas distintas</b>:
///
/// <list type="bullet">
///   <item>Para <c>/api/…</c>, la de siempre: <c>default-src 'self'</c>. Son respuestas JSON, así que
///   la más cerrada posible no cuesta nada.</item>
///   <item>Para el documento del SPA, la política del cliente, que necesita <c>'unsafe-inline'</c> en
///   estilos. Servirle la de la API la rompería: el navegador aplica la <b>intersección</b> de la
///   cabecera y el <c>meta</c>, así que la más estricta gana y los estilos calculados dejarían de
///   pintarse.</item>
/// </list>
///
/// La política del SPA <b>no se escribe aquí</b>: la genera el build del cliente y llega en
/// <c>wwwroot/csp.policy</c>. Duplicarla en C# es exactamente la divergencia silenciosa que este
/// proyecto ya se ha encontrado dos veces —una corrección aplicada en un sitio y no en su gemelo—, y
/// además dejaría fuera el origen de la API, que el build inyecta en <c>connect-src</c>.
///
/// Emitirla como <b>cabecera</b> y no solo como <c>meta</c> es lo que cierra <c>P-067</c>: hay
/// directivas que el navegador ignora en un <c>meta</c>, y <c>frame-ancestors</c> —la que frena el
/// clickjacking— es una de ellas.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, SpaContentSecurityPolicy spaPolicy)
{
    private const string ApiPolicy = "default-src 'self'";

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Content-Security-Policy"] = context.Request.Path.StartsWithSegments("/api")
            ? ApiPolicy
            : spaPolicy.Value ?? ApiPolicy;
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        // HSTS solo tiene efecto sobre HTTPS; los navegadores lo ignoran sobre HTTP, así que fijarlo
        // siempre es seguro y evita depender del entorno.
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        return next(context);
    }
}

/// <summary>
/// La CSP del cliente, leída una sola vez del fichero que emite su build.
///
/// Si no existe —API desplegada sin cliente, o ejecución local sin haber compilado el front— vale
/// <c>null</c> y el middleware usa la política de la API. Es el comportamiento seguro: ante la duda,
/// la más restrictiva.
/// </summary>
public sealed class SpaContentSecurityPolicy(string? value)
{
    public string? Value { get; } = value;

    /// <summary>Lee <c>csp.policy</c> del raíz web. Es lo que registra el contenedor.</summary>
    public static SpaContentSecurityPolicy FromWebRoot(IWebHostEnvironment environment)
    {
        var ruta = Path.Combine(environment.WebRootPath ?? string.Empty, "csp.policy");
        return new(File.Exists(ruta) ? File.ReadAllText(ruta).Trim() : null);
    }
}
