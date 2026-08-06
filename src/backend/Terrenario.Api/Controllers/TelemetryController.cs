using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Infrastructure.Telemetry;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-602 — Ingesta de las señales de uso del producto (entrada al área autenticada, uso del
/// dashboard, recarga manual y cobertura de widgets).
///
/// <b>Autenticado, pero sin ámbito de Workspace</b>: exigir <c>[Authorize]</c> evita que cualquiera
/// infle los contadores desde fuera, y no exigir <c>[RequireWorkspaceScope]</c> es deliberado: una
/// sesión sin Workspace (recién creada, o en onboarding) también es una sesión activa, y dejarla fuera
/// del divisor haría subir el KPI de uso del dashboard justo con los casos en los que el producto aún
/// no sirve de nada.
///
/// La señal <b>no lleva usuario ni Workspace</b> aunque el servidor los conozca: lo que se mide es
/// cuánto se usa el producto, no quién lo usa (RN-042).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/telemetry")]
public sealed class TelemetryController(IUsageTelemetry usage) : ControllerBase
{
    [HttpPost("usage")]
    public IActionResult Usage([FromBody] UsageTelemetryRequest request)
    {
        if (request.Event is null || !UsageEvents.ClientIngestable.Contains(request.Event))
            return BadRequest(new ApiErrorResponse(
                ApiError.Validation(ErrorCodes.ValidationRequired, "Evento de uso no reconocido.")));

        // Igual que en el embudo (MVP-601): las dimensiones secundarias degradan a `unknown` en vez de
        // tumbar el evento.
        var context = UsageEventContext.Create(request.SessionId, request.DeviceType);

        switch (request.Event)
        {
            case UsageEvents.AppSessionStarted:
                usage.AppSessionStarted(context);
                break;
            case UsageEvents.DashboardViewed:
                usage.DashboardViewed(context, request.FirstInSession ?? false);
                break;
            case UsageEvents.DashboardManualRefresh:
                usage.DashboardManualRefresh(context);
                break;
            case UsageEvents.DashboardWidgets:
                var outcomes = NormalizeWidgets(request.Widgets);
                if (outcomes.Count == 0)
                    return BadRequest(new ApiErrorResponse(ApiError.Validation(
                        ErrorCodes.ValidationRequired, "Ningún widget reconocido en la señal.")));
                usage.DashboardWidgets(context, outcomes);
                break;
        }

        // Fire-and-forget desde la perspectiva del cliente: medir no puede frenar la pantalla.
        return Accepted();
    }

    /// <summary>
    /// Se queda con los widgets y estados del catálogo cerrado y descarta el resto, en vez de rechazar
    /// el lote entero: un cliente más nuevo que envíe un widget que este servidor aún no conoce debe
    /// seguir aportando los que sí conoce. Se descartan también los repetidos, para que un cliente no
    /// pueda inflar la cobertura mandando el mismo widget veinte veces.
    /// </summary>
    private static List<DashboardWidgetOutcome> NormalizeWidgets(IReadOnlyList<WidgetOutcomeRequest>? widgets)
    {
        if (widgets is null) return [];

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var outcomes = new List<DashboardWidgetOutcome>(widgets.Count);

        foreach (var widget in widgets)
        {
            if (widget.Widget is null || widget.Status is null) continue;
            if (!DashboardWidgets.Keys.Contains(widget.Widget)) continue;
            if (!DashboardWidgets.Statuses.Contains(widget.Status)) continue;
            if (!seen.Add(widget.Widget)) continue;

            outcomes.Add(new DashboardWidgetOutcome(widget.Widget, widget.Status));
        }

        return outcomes;
    }
}

/// <summary>Señal de uso originada en el cliente (MVP-602).</summary>
public sealed record UsageTelemetryRequest(
    [property: JsonPropertyName("event")] string? Event,
    [property: JsonPropertyName("session_id")] string? SessionId = null,
    [property: JsonPropertyName("device_type")] string? DeviceType = null,
    // Solo en `dashboard_viewed`: si es la primera vez que esta sesión abre el dashboard.
    [property: JsonPropertyName("first_in_session")] bool? FirstInSession = null,
    // Solo en `dashboard_widgets`.
    [property: JsonPropertyName("widgets")] IReadOnlyList<WidgetOutcomeRequest>? Widgets = null);

public sealed record WidgetOutcomeRequest(
    [property: JsonPropertyName("widget")] string? Widget,
    [property: JsonPropertyName("status")] string? Status);
