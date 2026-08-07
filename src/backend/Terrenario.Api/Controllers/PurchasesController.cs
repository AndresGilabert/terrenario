using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Consumptions;
using Terrenario.Api.Application.Consumptions.Commands;
using Terrenario.Api.Application.Purchases;
using Terrenario.Api.Application.Purchases.Commands;
using Terrenario.Api.Application.Seasons;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Consumptions;
using Terrenario.Api.Domain.Operations;
using Terrenario.Api.Domain.Purchases;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-303 — Compras de material del Workspace activo (<c>contratos-api.md</c> §7). Segunda entidad
/// operativa crítica: hereda tal cual el patrón que estrenó la actividad en MVP-301 —<c>If-Match</c>
/// obligatorio en <c>PATCH</c>/<c>DELETE</c>, <c>409 CONFLICT_VERSION_MISMATCH</c> y <c>DELETE</c>
/// como baja lógica (RN-037)—.
///
/// La imputación por terrenos (<c>POST /purchases/{id}/consumptions</c>) es alcance de <c>MVP-304</c>.
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/purchases")]
public sealed class PurchasesController(
    CreatePurchaseHandler createPurchaseHandler,
    UpdatePurchaseHandler updatePurchaseHandler,
    DeletePurchaseHandler deletePurchaseHandler,
    ListPurchasesHandler listPurchasesHandler,
    ListPurchaseProductsHandler listPurchaseProductsHandler,
    ImputePurchaseHandler imputePurchaseHandler,
    IConsumptionRepository consumptionRepository,
    SeasonScopeResolver seasonScopeResolver,
    IWorkspaceContext workspaceContext) : ControllerBase
{
    /// <summary>
    /// Libro de compras del Workspace, por fecha de compra descendente.
    ///
    /// MVP-701 — <c>season_id</c> ausente aplica el defecto de RN-008 (la temporada de trabajo); el
    /// histórico completo se pide con <c>season_id=all</c> y el ámbito aplicado viaja en
    /// <c>meta.scope</c>.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? product,
        [FromQuery(Name = "season_id")] string? seasonId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken ct)
    {
        if (!TryParseDate(from, out var fromDate) || !TryParseDate(to, out var toDate))
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationRequired, "Las fechas de filtro deben tener el formato YYYY-MM-DD.")));

        var seasonScope = await seasonScopeResolver.ResolveAsync(
            User.GetUserId()!.Value, workspaceContext.WorkspaceId, seasonId, ct);

        var purchases = await listPurchasesHandler.HandleAsync(
            workspaceContext.WorkspaceId,
            new PurchaseFilter(product, seasonScope.FilterId, fromDate, toDate),
            ct);

        // MVP-304 — Cuánto se ha repartido ya de cada compra, en una sola consulta agrupada: el libro
        // muestra «imputado / total» por fila y hacerlo con una consulta por compra no escalaría.
        var imputed = await consumptionRepository.SumImputedQuantityByPurchaseAsync(
            workspaceContext.WorkspaceId, purchases.Select(p => p.Id).ToArray(), ct);

        return Ok(new
        {
            data = purchases.Select(p => ToResponse(p, imputed.GetValueOrDefault(p.Id))),
            meta = new
            {
                // MVP-701 — Ámbito de temporada realmente aplicado (RN-008).
                scope = seasonScope.ToResponse(),
                total = purchases.Count,
                // El gasto acumulado de lo filtrado: el libro de compras lo muestra en cabecera y
                // calcularlo en cliente obligaría a rehacerlo en cada consumidor.
                total_cost = purchases.Sum(p => p.TotalCost)
            }
        });
    }

    /// <summary>
    /// Vocabulario de materiales del histórico (RN-031, HU-2). <b>No es un catálogo</b>: no se
    /// administra y el usuario siempre puede escribir algo que no esté en la lista.
    /// </summary>
    [HttpGet("products")]
    public async Task<IActionResult> ListProducts([FromQuery] string? search, CancellationToken ct)
    {
        var products = await listPurchaseProductsHandler.HandleAsync(
            workspaceContext.WorkspaceId, search, ct);

        return Ok(new
        {
            data = products.Select(p => new { product = p.Product, times_used = p.TimesUsed }),
            meta = new { total = products.Count }
        });
    }

    /// <summary>Alta de compra (HU-1, CA-1).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseRequest request, CancellationToken ct)
    {
        if (!TryParseDate(request.PurchaseDate, out var purchaseDate) || purchaseDate is null)
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationPurchaseRequiredFields,
                "La fecha de compra es obligatoria (formato YYYY-MM-DD).")));

        try
        {
            var purchase = await createPurchaseHandler.HandleAsync(
                new CreatePurchaseCommand(
                    workspaceContext.WorkspaceId,
                    User.GetUserId()!.Value,
                    request.SeasonId,
                    purchaseDate.Value,
                    request.Product,
                    request.TotalQuantity,
                    request.TotalCost),
                ct);

            return CreatedAtAction(nameof(List), new { id = purchase.Id }, ToResponse(purchase));
        }
        catch (PurchaseValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    /// <summary>Edición parcial de una compra. Exige <c>If-Match</c> con la versión vigente.</summary>
    [HttpPatch("{purchaseId:guid}")]
    public async Task<IActionResult> Update(
        Guid purchaseId,
        [FromBody] Dictionary<string, JsonElement>? body,
        CancellationToken ct)
    {
        if (!IfMatchHeader.TryRead(Request.Headers, out var expectedVersion))
            return BadRequest(new ApiErrorResponse(ApiError.IfMatchRequired()));

        body ??= new Dictionary<string, JsonElement>();

        UpdatePurchaseCommand command;
        try
        {
            command = new UpdatePurchaseCommand(
                workspaceContext.WorkspaceId,
                User.GetUserId()!.Value,
                purchaseId,
                expectedVersion,
                ReadGuid(body, "season_id"),
                ReadDate(body, "purchase_date"),
                ReadString(body, "product"),
                ReadDecimal(body, "total_quantity"),
                ReadDecimal(body, "total_cost"));
        }
        catch (PurchaseValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }

        try
        {
            var purchase = await updatePurchaseHandler.HandleAsync(command, ct);

            if (purchase is null)
                return NotFound(new ApiErrorResponse(ApiError.PurchaseNotFound()));

            return Ok(ToResponse(purchase));
        }
        catch (PurchaseValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (ConcurrencyConflictException ex)
        {
            return VersionConflict(ex);
        }
    }

    /// <summary>
    /// MVP-304 (HU-1, CA-1) — Imputa la compra a un terreno con cantidad aproximada. El producto, la
    /// temporada y el precio unitario se heredan de la compra: el coste proporcional es
    /// <c>cantidad × unit_price</c>. No se puede repartir más de lo comprado
    /// (<c>VALIDATION_CONSUMPTION_OVERFLOW</c>).
    /// </summary>
    [HttpPost("{purchaseId:guid}/consumptions")]
    public async Task<IActionResult> Impute(
        Guid purchaseId,
        [FromBody] ImputePurchaseRequest request,
        CancellationToken ct)
    {
        if (!TryParseDate(request.Date, out var date) || date is null)
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationConsumptionRequiredFields,
                "La fecha de la imputación es obligatoria (formato YYYY-MM-DD).")));

        try
        {
            var consumption = await imputePurchaseHandler.HandleAsync(
                new ImputePurchaseCommand(
                    workspaceContext.WorkspaceId,
                    User.GetUserId()!.Value,
                    purchaseId,
                    request.PlotId,
                    date.Value,
                    request.Quantity),
                ct);

            if (consumption is null)
                return NotFound(new ApiErrorResponse(ApiError.PurchaseNotFound()));

            return CreatedAtAction(nameof(List), new { id = consumption.Id },
                ConsumptionsController.ToResponse(consumption));
        }
        catch (ConsumptionValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    /// <summary>
    /// Eliminación <b>lógica</b> de una compra (RN-037). Exige <c>If-Match</c>.
    ///
    /// MVP-304 — Se rechaza con <c>422</c> si la compra todavía tiene imputaciones vivas: son
    /// registros operativos propios que están en el diario, y llevárselos en cascada eliminaría datos
    /// que nadie pidió eliminar.
    /// </summary>
    [HttpDelete("{purchaseId:guid}")]
    public async Task<IActionResult> Delete(Guid purchaseId, CancellationToken ct)
    {
        if (!IfMatchHeader.TryRead(Request.Headers, out var expectedVersion))
            return BadRequest(new ApiErrorResponse(ApiError.IfMatchRequired()));

        try
        {
            var deleted = await deletePurchaseHandler.HandleAsync(
                new DeletePurchaseCommand(
                    workspaceContext.WorkspaceId, User.GetUserId()!.Value, purchaseId, expectedVersion),
                ct);

            return deleted
                ? NoContent()
                : NotFound(new ApiErrorResponse(ApiError.PurchaseNotFound()));
        }
        catch (PurchaseBusinessRuleException ex)
        {
            return UnprocessableEntity(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
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

    private static bool TryParseDate(string? raw, out DateOnly? value)
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

        throw new PurchaseValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser una fecha YYYY-MM-DD.");
    }

    private static FieldUpdate<Guid> ReadGuid(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<Guid>.Absent;
        if (el.ValueKind == JsonValueKind.String && Guid.TryParse(JsonText.Read(el, key), out var parsed))
            return FieldUpdate<Guid>.Set(parsed);

        throw new PurchaseValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser un identificador válido.");
    }

    private static FieldUpdate<decimal> ReadDecimal(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<decimal>.Absent;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var parsed))
            return FieldUpdate<decimal>.Set(parsed);

        throw new PurchaseValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser numérico.");
    }

    private static object ToResponse(PurchaseView purchase, decimal imputedQuantity = 0m) => new
    {
        id = purchase.Id,
        workspace_id = purchase.WorkspaceId,
        purchase_date = purchase.PurchaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        season_id = purchase.SeasonId,
        season_name = purchase.SeasonName,
        product = purchase.Product,
        total_quantity = purchase.TotalQuantity,
        total_cost = purchase.TotalCost,
        unit_price = purchase.UnitPrice,
        // RN-023 — mismo aviso no bloqueante que en la actividad.
        is_out_of_season_range = purchase.IsOutOfSeasonRange,
        // MVP-304 — cuánto se ha repartido ya por terrenos y cuánto queda por repartir.
        imputed_quantity = imputedQuantity,
        pending_quantity = purchase.TotalQuantity - imputedQuantity,
        version = purchase.Version,
        created_at = purchase.CreatedAt,
        updated_at = purchase.UpdatedAt
    };
}

/// <summary>
/// Imputación de una compra a un terreno (MVP-304). Solo terreno, fecha y cantidad: el producto, la
/// temporada y el precio unitario los pone la compra.
/// </summary>
public sealed record ImputePurchaseRequest(
    [RequiredField(ErrorCodes.ValidationConsumptionRequiredFields, "La fecha de la imputación es obligatoria.")]
    string Date,
    [property: JsonPropertyName("plot_id")] Guid PlotId,
    decimal Quantity);

/// <summary>
/// Alta de compra (<c>contratos-api.md</c> §7). El producto es texto libre (RN-031) y la temporada es
/// obligatoria (RN-021). El precio unitario no se envía: lo deriva el servidor.
/// </summary>
public sealed record CreatePurchaseRequest(
    [RequiredField(ErrorCodes.ValidationPurchaseRequiredFields, "La fecha de compra es obligatoria.")]
    [property: JsonPropertyName("purchase_date")]
    string PurchaseDate,
    [RequiredField(ErrorCodes.ValidationPurchaseRequiredProduct, "El producto o material es obligatorio.")]
    [MaxTextLength(Purchase.ProductMaxLength, ErrorCodes.ValidationPurchaseProductLength, "El producto es demasiado largo.")]
    string Product,
    [property: JsonPropertyName("season_id")] Guid SeasonId,
    [property: JsonPropertyName("total_quantity")] decimal TotalQuantity,
    [property: JsonPropertyName("total_cost")] decimal TotalCost);
