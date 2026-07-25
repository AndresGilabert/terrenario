namespace Terrenario.Api.Common.Http;

/// <summary>
/// Garantiza un identificador de correlación por petición en todas las respuestas, exigido por las
/// convenciones de <c>docs/02-arquitectura/contratos-api.md</c> (MVP-105 / P-006). Reutiliza un
/// <c>X-Request-Id</c> entrante si es válido —para encadenar la traza con quien llama— o genera uno.
/// Lo publica en la respuesta y en el scope de logging para poder correlacionar errores (500) con
/// sus logs.
/// </summary>
public sealed class RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
{
    public const string HeaderName = "X-Request-Id";
    private const int MaxLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = ResolveRequestId(context);

        context.Response.Headers[HeaderName] = requestId;
        context.TraceIdentifier = requestId;

        using (logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = requestId }))
        {
            await next(context);
        }
    }

    private static string ResolveRequestId(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        return IsValid(incoming) ? incoming : Guid.NewGuid().ToString("N");
    }

    // Se acota el valor entrante para que no pueda inyectar contenido arbitrario en la traza ni en
    // el header de respuesta.
    private static bool IsValid(string? value) =>
        !string.IsNullOrEmpty(value) &&
        value.Length <= MaxLength &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
