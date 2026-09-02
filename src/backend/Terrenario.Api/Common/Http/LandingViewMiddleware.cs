using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Common.Http;

/// <summary>
/// MKT-106 (CA-1) — Cuenta las visitas a las landings públicas de <c>MKT-102</c>, que son HTML
/// estático sin ningún JavaScript ejecutable (<c>ADR-0012</c>) y por tanto no pueden emitir ningún
/// evento de cliente.
///
/// Va **antes** de <c>UseStaticFiles</c> y no sustituye nada de su comportamiento: solo suma un
/// contador y sigue la petición. El servidor ve el 100 % del tráfico real porque el cliente se sirve
/// desde este mismo origen, sin CDN por delante (ver el middleware de <c>home.html</c> en
/// <c>Program.cs</c>).
/// </summary>
public sealed class LandingViewMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITelemetryCounters counters, IWebHostEnvironment env)
    {
        if (HttpMethods.IsGet(context.Request.Method))
        {
            var landing = LandingCatalog.TryClassifyRequestPath(
                env.WebRootPath ?? string.Empty, context.Request.Path);

            if (landing is not null) counters.Add(TelemetryMetrics.LandingViewFor(landing));
        }

        await next(context);
    }
}
