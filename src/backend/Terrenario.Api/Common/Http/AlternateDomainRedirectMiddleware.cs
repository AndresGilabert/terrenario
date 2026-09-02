using Microsoft.Extensions.Options;

namespace Terrenario.Api.Common.Http;

/// <summary>
/// PLT-101 — Los dominios comprados para no perderlos (<c>terrenario.com</c>, <c>terrenario.es</c> y
/// sus <c>www</c>) no tienen contenido propio: la aplicación entera vive en <c>app.terrenario.com</c>
/// (`publicacion-inicial-en-azure.md`). Antes de este middleware, quien llegaba por esos dominios no
/// encontraba nada.
///
/// Va **el primero de todo el pipeline**, antes de <c>RequestIdMiddleware</c>: un dominio que no es el
/// canónico no necesita ni traza ni métricas propias, solo la redirección. <c>301</c> y no
/// <c>302</c> porque el cambio es permanente y así lo aprenden los buscadores y los navegadores
/// (evita repetir el salto en cada visita).
/// </summary>
public sealed class AlternateDomainRedirectMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, IOptions<DomainRedirectOptions> options)
    {
        var configured = options.Value;

        if (configured.AlternateHosts.Count == 0 || string.IsNullOrEmpty(configured.CanonicalHost))
            return next(context);

        var host = StripPort(context.Request.Host.Value ?? string.Empty);

        if (!configured.AlternateHosts.Contains(host, StringComparer.OrdinalIgnoreCase))
            return next(context);

        var destino = $"https://{configured.CanonicalHost}{context.Request.Path}{context.Request.QueryString}";
        context.Response.Redirect(destino, permanent: true);
        return Task.CompletedTask;
    }

    private static string StripPort(string host)
    {
        var separador = host.IndexOf(':');
        return separador < 0 ? host : host[..separador];
    }
}
