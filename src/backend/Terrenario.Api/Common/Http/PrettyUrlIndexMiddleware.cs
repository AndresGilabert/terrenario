using System.IO;

namespace Terrenario.Api.Common.Http;

/// <summary>
/// MKT-102 (riesgo pendiente, detectado en uso real) — <c>UseDefaultFiles</c> solo resuelve el
/// <c>index.html</c> de una carpeta cuando la URL **termina en <c>/</c></c>. Las landings públicas se
/// enlazan y se declaran en su propio <c>canonical</c> (`MKT-103`) **sin** barra final
/// (<c>/funcionalidades/gestion-terrenos</c>), así que sin este middleware esa URL exacta da 404 y
/// solo funciona con la barra añadida a mano.
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
            || path.EndsWith('/')
            || path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(webRoot))
        {
            await next(context);
            return;
        }

        var candidato = Path.Combine(webRoot, path.TrimStart('/'), "index.html");
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
