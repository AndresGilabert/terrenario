using System.IO;

namespace Terrenario.Api.Common.Http;

/// <summary>
/// MKT-102 (riesgo pendiente, detectado en producción en <c>v0.9.0</c>) — <c>UseDefaultFiles</c> no
/// resolvió el <c>index.html</c> de las carpetas de landings en App Service, ni con barra final ni
/// sin ella. Ambas formas caían en <c>MapFallback</c> y devolvían el shell vacío de la SPA.
///
/// Sirve el fichero directamente (no redirige) para no desalinear la URL servida del
/// <c>canonical</c> ya declarado en el propio HTML.
/// </summary>
public sealed class PrettyUrlIndexMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IWebHostEnvironment env)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var webRoot = env.WebRootPath;

        if (!HttpMethods.IsGet(context.Request.Method)
            || path == "/"
            || path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(webRoot))
        {
            await next(context);
            return;
        }

        var candidato = Path.Combine(webRoot, path.Trim('/'), "index.html");
        var raizAbsoluta = Path.GetFullPath(webRoot) + Path.DirectorySeparatorChar;

        // El path de la petición no puede sacar la resolución fuera de `wwwroot` (p. ej. `..`).
        if (!Path.GetFullPath(candidato).StartsWith(raizAbsoluta, StringComparison.Ordinal)
            || !File.Exists(candidato))
        {
            await next(context);
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(candidato);
    }
}
