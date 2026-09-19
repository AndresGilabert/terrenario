using Microsoft.AspNetCore.Http;
using System.IO;

namespace Terrenario.Api.Infrastructure.Telemetry;
/// <summary>
/// MKT-106 — Resuelve una ruta pública a la clave de landing que la identifica en la telemetría
/// (<c>home</c>, <c>funcionalidades.{slug}</c>, <c>para.{slug}</c>), o <c>null</c> si la ruta no es
/// una landing.
///
/// <b>Catálogo abierto a propósito</b> (crecerá con cada campaña nueva): no hay una lista de slugs
/// válidos declarada aquí. La validación es que el fichero físico exista en <c>wwwroot</c> — lo mismo
/// que ya decide si <c>UseStaticFiles</c> puede servir la página. Esto cierra dos cosas a la vez: no
/// hace falta desplegar el backend por cada landing nueva, y una clave de landing nunca sale de texto
/// de cliente sin contrastar contra contenido que de verdad existe.
/// </summary>
public static class LandingCatalog
{
    private const int SlugMaxLength = 64;

    /// <summary>Resuelve la ruta de una petición al servidor (conteo de vistas, MKT-106 CA-1).</summary>
    public static string? TryClassifyRequestPath(string webRootPath, PathString path)
    {
        if (string.IsNullOrEmpty(webRootPath)) return null;

        if (path == "/")
            return File.Exists(Path.Combine(webRootPath, "home.html")) ? "home" : null;

        if (TryMatchSlug(path.Value, "/funcionalidades/", out var funcionalidadSlug))
            return ExistsAsFolder(webRootPath, "funcionalidades", funcionalidadSlug)
                ? $"funcionalidades.{funcionalidadSlug}"
                : null;

        if (TryMatchSlug(path.Value, "/para/", out var paraSlug))
            return ExistsAsFolder(webRootPath, "para", paraSlug) ? $"para.{paraSlug}" : null;

        return null;
    }

    /// <summary>
    /// Resuelve una ruta que llega como texto (la de un <c>Referer</c>), mismas reglas que
    /// <see cref="TryClassifyRequestPath"/> pero sin depender de <see cref="PathString"/>.
    /// </summary>
    public static string? TryClassifyReferrerPath(string webRootPath, string absolutePath)
        => TryClassifyRequestPath(webRootPath, new PathString(absolutePath));

    private static bool TryMatchSlug(string? path, string prefix, out string slug)
    {
        slug = string.Empty;
        if (string.IsNullOrEmpty(path) || !path.StartsWith(prefix, StringComparison.Ordinal)) return false;

        var rest = path[prefix.Length..].TrimEnd('/');
        if (rest.Length == 0 || rest.Length > SlugMaxLength || rest.Contains('/')) return false;
        if (!rest.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')) return false;

        slug = rest;
        return true;
    }

    private static bool ExistsAsFolder(string webRootPath, string folder, string slug)
        => File.Exists(Path.Combine(webRootPath, folder, slug, "index.html"));
}
