namespace Terrenario.Api.Common.Http;

/// <summary>
/// Añade a todas las respuestas los headers de seguridad HTTP exigidos por
/// <c>docs/07-seguridad/autenticacion-autorizacion.md</c>. Se registra al principio del pipeline
/// para cubrir también las respuestas de error cortadas por filtros o middlewares posteriores
/// (MVP-105 / P-005). Los valores se fijan con el indexador (sobrescriben) para no duplicar headers.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Content-Security-Policy"] = "default-src 'self'";
        // HSTS solo tiene efecto sobre HTTPS; los navegadores lo ignoran sobre HTTP, así que fijarlo
        // siempre es seguro y evita depender del entorno.
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        return next(context);
    }
}
