using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Consumptions;
using Terrenario.Api.Application.Consumptions.Commands;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-304 — Consumos de material del Workspace activo (<c>contratos-api.md</c> §7).
///
/// Aquí vive la excepción operativa más importante de la épica: <c>POST /api/v1/consumptions</c>
/// registra un consumo <b>sin compra previa</b> (RN-032). La ausencia de compra nunca bloquea la
/// captura; el coste imputado es <c>0</c> y la respuesta lo señala con <c>has_purchase: false</c>
/// para que la UI avise del impacto en la calidad del dato (CA-2).
///
/// La imputación de una compra concreta cuelga de ella:
/// <c>POST /api/v1/purchases/{id}/consumptions</c> (ver <see cref="PurchasesController"/>).
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/consumptions")]
public sealed class ConsumptionsController(
    RegisterConsumptionHandler registerConsumptionHandler,
    UpdateConsumptionHandler updateConsumptionHandler,
    DeleteConsumptionHandler deleteConsumptionHandler,
    ListConsumptionsHandler listConsumptionsHandler,
    IWorkspaceContext workspaceContext) : ControllerBase
{
    /// <summary>
    /// Consumos e imputaciones del Workspace, por <b>fecha de negocio</b> descendente (CA-4): un
    /// consumo capturado hoy sobre trabajo de la semana pasada se lee donde ocurrió, no donde se
    /// apuntó.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery(Name = "plot_id")] Guid? plotId,
        [FromQuery(Name = "season_id")] Guid? seasonId,
        [FromQuery(Name = "purchase_id")] Guid? purchaseId,
        [FromQuery] string? product,
        CancellationToken ct)
    {
        if (!TryParseDate(from, out var fromDate) || !TryParseDate(to, out var toDate))
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationRequired, "Las fechas de filtro deben tener el formato YYYY-MM-DD.")));

        var consumptions = await listConsumptionsHandler.HandleAsync(
            workspaceContext.WorkspaceId,
            new ConsumptionFilter(fromDate, toDate, plotId, seasonId, purchaseId, product),
            ct);

        return Ok(new
        {
            data = consumptions.Select(ToResponse),
            meta = new
            {
                total = consumptions.Count,
                total_cost = consumptions.Sum(c => c.ProportionalCost),
                // Cuántos se registraron sin compra: es la medida del "impacto en la calidad del
                // dato" que pide el CA-3 de la épica, y la UI la usa para avisar en conjunto.
                without_purchase = consumptions.Count(c => !c.HasPurchase)
            }
        });
    }

    /// <summary>
    /// Consumo <b>sin compra previa</b> (HU-2, CA-2, RN-032). Coste <c>0</c> y aviso: registrar la
    /// compra más tarde <b>no</b> recalcula este registro (CA-3).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterConsumptionRequest request,
        CancellationToken ct)
    {
        if (!TryParseDate(request.Date, out var date) || date is null)
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationConsumptionRequiredFields,
                "La fecha del consumo es obligatoria (formato YYYY-MM-DD).")));

        try
        {
            var consumption = await registerConsumptionHandler.HandleAsync(
                new RegisterConsumptionCommand(
                    workspaceContext.WorkspaceId,
                    User.GetUserId()!.Value,
                    request.SeasonId,
                    request.PlotId,
                    date.Value,
                    request.Product,
                    request.Quantity),
                ct);

            return CreatedAtAction(nameof(List), new { id = consumption.Id }, ToResponse(consumption));
        }
        catch (ConsumptionValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    /// <summary>Edición parcial de un consumo. Exige <c>If-Match</c> con la versión vigente.</summary>
    [HttpPatch("{consumptionId:guid}")]
    public async Task<IActionResult> Update(
        Guid consumptionId,
        [FromBody] Dictionary<string, JsonElement>? body,
        CancellationToken ct)
    {
        if (!IfMatchHeader.TryRead(Request.Headers, out var expectedVersion))
            return BadRequest(new ApiErrorResponse(ApiError.IfMatchRequired()));

        body ??= new Dictionary<string, JsonElement>();

        UpdateConsumptionCommand command;
        try
        {
            command = new UpdateConsumptionCommand(
                workspaceContext.WorkspaceId,
                User.GetUserId()!.Value,
                consumptionId,
                expectedVersion,
                ReadGuid(body, "season_id"),
                ReadGuid(body, "plot_id"),
                ReadDate(body, "date"),
                ReadString(body, "product"),
                ReadDecimal(body, "quantity"));
        }
        catch (ConsumptionValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }

        try
        {
            var consumption = await updateConsumptionHandler.HandleAsync(command, ct);

            if (consumption is null)
                return NotFound(new ApiErrorResponse(ApiError.ConsumptionNotFound()));

            return Ok(ToResponse(consumption));
        }
        catch (ConsumptionValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (ConcurrencyConflictException ex)
        {
            return VersionConflict(ex);
        }
    }

    /// <summary>Eliminación <b>lógica</b> de un consumo (RN-037). Exige <c>If-Match</c>.</summary>
    [HttpDelete("{consumptionId:guid}")]
    public async Task<IActionResult> Delete(Guid consumptionId, CancellationToken ct)
    {
        if (!IfMatchHeader.TryRead(Request.Headers, out var expectedVersion))
            return BadRequest(new ApiErrorResponse(ApiError.IfMatchRequired()));

        try
        {
            var deleted = await deleteConsumptionHandler.HandleAsync(
                new DeleteConsumptionCommand(
                    workspaceContext.WorkspaceId, User.GetUserId()!.Value, consumptionId, expectedVersion),
                ct);

            return deleted
                ? NoContent()
                : NotFound(new ApiErrorResponse(ApiError.ConsumptionNotFound()));
        }
        catch (ConcurrencyConflictException ex)
        {
            return VersionConflict(ex);
        }
    }

    private IActionResult VersionConflict(ConcurrencyConflictException ex)
        => Conflict(new
        {
            error = new
            {
                code = ErrorCodes.ConflictVersionMismatch,
                message = ex.Message,
                current_version = ex.CurrentVersion
            }
        });

    internal static bool TryParseDate(string? raw, out DateOnly? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw)) return true;

        if (!DateOnly.TryParseExact(raw.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
            return false;

        value = parsed;
        return true;
    }

    private static FieldUpdate<string> ReadString(Dictionary<string, JsonElement> body, string key)
        => body.TryGetValue(key, out var el)
            ? FieldUpdate<string>.Set(JsonText.Read(el, key))
            : FieldUpdate<string>.Absent;

    private static FieldUpdate<DateOnly> ReadDate(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<DateOnly>.Absent;

        if (el.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(JsonText.Read(el, key), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
            return FieldUpdate<DateOnly>.Set(parsed);

        throw new ConsumptionValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser una fecha YYYY-MM-DD.");
    }

    private static FieldUpdate<Guid> ReadGuid(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<Guid>.Absent;
        if (el.ValueKind == JsonValueKind.String && Guid.TryParse(JsonText.Read(el, key), out var parsed))
            return FieldUpdate<Guid>.Set(parsed);

        throw new ConsumptionValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser un identificador válido.");
    }

    private static FieldUpdate<decimal> ReadDecimal(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<decimal>.Absent;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var parsed))
            return FieldUpdate<decimal>.Set(parsed);

        throw new ConsumptionValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser numérico.");
    }

    internal static object ToResponse(ConsumptionView consumption) => new
    {
        id = consumption.Id,
        workspace_id = consumption.WorkspaceId,
        purchase_id = consumption.PurchaseId,
        // RN-032 — `false` significa «coste desconocido», no «gratis»: es la señal del aviso (CA-2).
        has_purchase = consumption.HasPurchase,
        plot_id = consumption.PlotId,
        plot_name = consumption.PlotName,
        season_id = consumption.SeasonId,
        season_name = consumption.SeasonName,
        date = consumption.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        product = consumption.Product,
        quantity = consumption.ConsumedQuantity,
        unit_price = consumption.UnitPrice,
        proportional_cost = consumption.ProportionalCost,
        is_out_of_season_range = consumption.IsOutOfSeasonRange,
        version = consumption.Version,
        created_at = consumption.CreatedAt,
        updated_at = consumption.UpdatedAt
    };
}

/// <summary>
/// Consumo sin compra previa (<c>contratos-api.md</c> §7). Aquí el producto y la temporada son
/// obligatorios porque no hay compra de la que heredarlos (RN-031, RN-021).
/// </summary>
public sealed record RegisterConsumptionRequest(
    [RequiredField(ErrorCodes.ValidationConsumptionRequiredFields, "La fecha del consumo es obligatoria.")]
    string Date,
    [property: JsonPropertyName("plot_id")] Guid PlotId,
    [property: JsonPropertyName("season_id")] Guid SeasonId,
    [RequiredField(ErrorCodes.ValidationConsumptionRequiredProduct, "El producto consumido es obligatorio.")]
    [MaxTextLength(PurchaseConsumption.ProductMaxLength, ErrorCodes.ValidationConsumptionProductLength, "El producto es demasiado largo.")]
    string Product,
    decimal Quantity);
