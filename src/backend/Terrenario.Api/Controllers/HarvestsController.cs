using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terrenario.Api.Application.Harvests;
using Terrenario.Api.Application.Harvests.Commands;
using Terrenario.Api.Application.Seasons;
using Terrenario.Api.Common;
using Terrenario.Api.Common.Auth;
using Terrenario.Api.Common.Errors;
using Terrenario.Api.Common.Http;
using Terrenario.Api.Common.Workspaces;
using Terrenario.Api.Domain.Harvests;
using Terrenario.Api.Domain.Operations;

namespace Terrenario.Api.Controllers;

/// <summary>
/// MVP-401 — Cosechas del Workspace activo (<c>contratos-api.md §6</c>). Como el resto de recursos con
/// ámbito de Workspace se apoya en <see cref="RequireWorkspaceScopeAttribute"/> (MVP-105): el
/// Workspace se resuelve en servidor y nunca viaja como parámetro (RN-034).
///
/// Es la cuarta <b>entidad operativa crítica</b> del producto y no reinventa nada: <c>PATCH</c> y
/// <c>DELETE</c> exigen <c>If-Match</c> con la versión vigente y responden
/// <c>409 CONFLICT_VERSION_MISMATCH</c> si no lo es (ADR-0005), y el <c>DELETE</c> es una <b>baja
/// lógica</b> (RN-037). Es el patrón que estrenó <c>ActivitiesController</c> en MVP-301.
/// </summary>
[ApiController]
[Authorize]
[RequireWorkspaceScope]
[Route("api/v1/harvests")]
public sealed class HarvestsController(
    CreateHarvestHandler createHarvestHandler,
    UpdateHarvestHandler updateHarvestHandler,
    DeleteHarvestHandler deleteHarvestHandler,
    ListHarvestsHandler listHarvestsHandler,
    GetHarvestHandler getHarvestHandler,
    FindHarvestDuplicatesHandler findHarvestDuplicatesHandler,
    SeasonScopeResolver seasonScopeResolver,
    IWorkspaceContext workspaceContext) : ControllerBase
{
    /// <summary>
    /// Cosechas del Workspace por fecha de negocio descendente (RN-033). Filtros opcionales:
    /// <c>from</c>, <c>to</c>, <c>plot_id</c>, <c>season_id</c>, <c>destination</c>.
    ///
    /// MVP-701 — <c>season_id</c> ausente aplica el defecto de RN-008 (la temporada de trabajo), no
    /// «todas». Era la causa de que esta pantalla y la Visión General dieran totales distintos de la
    /// misma campaña (<c>P-082</c>). El histórico completo se pide con <c>season_id=all</c>.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery(Name = "plot_id")] Guid? plotId,
        [FromQuery(Name = "season_id")] string? seasonId,
        [FromQuery] string? destination,
        CancellationToken ct)
    {
        if (!TryParseDate(from, out var fromDate) || !TryParseDate(to, out var toDate))
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationRequired, "Las fechas de filtro deben tener el formato YYYY-MM-DD.")));

        var seasonScope = await seasonScopeResolver.ResolveAsync(
            User.GetUserId()!.Value, workspaceContext.WorkspaceId, seasonId, ct);

        var harvests = await listHarvestsHandler.HandleAsync(
            workspaceContext.WorkspaceId,
            new HarvestFilter(fromDate, toDate, plotId, seasonScope.FilterId, destination),
            ct);

        return Ok(new
        {
            data = harvests.Select(ToResponse),
            meta = new
            {
                // MVP-701 — Ámbito de temporada realmente aplicado (RN-008).
                scope = seasonScope.ToResponse(),
                total = harvests.Count,
                // Lo que la cabecera del listado necesita sin recalcularlo en cliente ni pedir el
                // dashboard: kilos acumulados de lo filtrado. El mismo criterio que `total_cost` en el
                // libro de compras (MVP-303).
                total_kg = harvests.Sum(harvest => harvest.Kgs)
            }
        });
    }

    /// <summary>
    /// MVP-805 (RN-044, <c>RU-24</c>) — Partidas vivas que coinciden con la que se está escribiendo:
    /// <b>mismo terreno, misma fecha y mismo producto</b>.
    ///
    /// Es una lectura de apoyo del formulario, no un filtro más del listado: lo que devuelve alimenta
    /// un **aviso no bloqueante**, del mismo tipo que los de RN-023 y RN-043. Va en su propia ruta —y
    /// no como parámetros de <c>GET /harvests</c>— para que la regla de qué cuenta como duplicado
    /// tenga un nombre en el contrato en vez de vivir repartida en los dos formularios que la usan.
    ///
    /// <c>exclude_id</c> es la propia partida al corregir: cambiarle el destino a una cosecha no puede
    /// avisar de que esa cosecha ya existe (CA-3).
    /// </summary>
    [HttpGet("duplicates")]
    public async Task<IActionResult> Duplicates(
        [FromQuery(Name = "plot_id")] Guid? plotId,
        [FromQuery] string? date,
        [FromQuery] string? product,
        [FromQuery(Name = "exclude_id")] Guid? excludeId,
        CancellationToken ct)
    {
        if (!TryParseDate(date, out var parsedDate) || parsedDate is null || plotId is null
            || string.IsNullOrWhiteSpace(product))
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationRequired,
                "Para buscar partidas iguales hacen falta plot_id, date (YYYY-MM-DD) y product.")));
        }

        var duplicates = await findHarvestDuplicatesHandler.HandleAsync(
            workspaceContext.WorkspaceId, plotId.Value, parsedDate.Value, product, excludeId, ct);

        return Ok(new
        {
            // Solo lo que el aviso necesita nombrar: los kilos y el destino de la partida que ya está,
            // que es lo que permite distinguir de un vistazo si es la misma o una segunda de verdad.
            data = duplicates.Select(duplicate => new
            {
                id = duplicate.Id,
                kgs = duplicate.Kgs,
                destination = duplicate.Destination
            }),
            meta = new { total = duplicates.Count }
        });
    }

    /// <summary>
    /// Una cosecha concreta del Workspace activo. La usa el diario unificado: su entrada es una
    /// proyección común de los cuatro tipos y no lleva todos los campos del formulario de corrección.
    /// </summary>
    [HttpGet("{harvestId:guid}")]
    public async Task<IActionResult> GetById(Guid harvestId, CancellationToken ct)
    {
        var harvest = await getHarvestHandler.HandleAsync(workspaceContext.WorkspaceId, harvestId, ct);

        return harvest is null
            ? NotFound(new ApiErrorResponse(ApiError.HarvestNotFound()))
            : Ok(ToResponse(harvest));
    }

    /// <summary>Alta de cosecha (HU-1, CA-1).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHarvestRequest request, CancellationToken ct)
    {
        if (!TryParseDate(request.Date, out var date) || date is null)
            return BadRequest(new ApiErrorResponse(ApiError.Validation(
                ErrorCodes.ValidationHarvestRequiredFields, "La fecha es obligatoria (formato YYYY-MM-DD).")));

        try
        {
            var harvest = await createHarvestHandler.HandleAsync(
                new CreateHarvestCommand(
                    workspaceContext.WorkspaceId,
                    User.GetUserId()!.Value,
                    date.Value,
                    request.PlotId,
                    request.SeasonId,
                    request.Product,
                    request.Kgs,
                    request.Destination,
                    request.Yield,
                    request.Liters,
                    request.YieldUnit,
                    request.UnitPrice),
                ct);

            return CreatedAtAction(nameof(GetById), new { harvestId = harvest.Id }, ToResponse(harvest));
        }
        catch (HarvestValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
    }

    /// <summary>
    /// Corrección de una cosecha (HU-2, CA-2). Edición parcial: un campo ausente conserva su valor.
    /// Exige <c>If-Match</c> con la versión vigente (CA-5).
    /// </summary>
    [HttpPatch("{harvestId:guid}")]
    public async Task<IActionResult> Update(
        Guid harvestId,
        [FromBody] Dictionary<string, JsonElement>? body,
        CancellationToken ct)
    {
        if (!IfMatchHeader.TryRead(Request.Headers, out var expectedVersion))
            return BadRequest(new ApiErrorResponse(ApiError.IfMatchRequired()));

        body ??= new Dictionary<string, JsonElement>();

        UpdateHarvestCommand command;
        try
        {
            command = new UpdateHarvestCommand(
                workspaceContext.WorkspaceId,
                User.GetUserId()!.Value,
                harvestId,
                expectedVersion,
                ReadDate(body, "date"),
                ReadGuid(body, "plot_id"),
                ReadGuid(body, "season_id"),
                ReadString(body, "product"),
                ReadDecimal(body, "kgs"),
                ReadString(body, "destination"),
                ReadNullableDecimal(body, "yield"),
                ReadNullableDecimal(body, "liters"),
                ReadPlainString(body, "yield_unit"),
                // MVP-707 — anulable a propósito: `null` explícito **retira** el precio.
                ReadNullableDecimal(body, "unit_price"));
        }
        catch (HarvestValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }

        try
        {
            var harvest = await updateHarvestHandler.HandleAsync(command, ct);

            return harvest is null
                ? NotFound(new ApiErrorResponse(ApiError.HarvestNotFound()))
                : Ok(ToResponse(harvest));
        }
        catch (HarvestValidationException ex)
        {
            return BadRequest(new ApiErrorResponse(ApiError.Validation(ex.ErrorCode, ex.Message)));
        }
        catch (ConcurrencyConflictException ex)
        {
            return VersionConflict(ex);
        }
    }

    /// <summary>
    /// Eliminación <b>lógica</b> de una cosecha (RN-037, CA-5). Exige <c>If-Match</c>. La confirmación
    /// explícita del usuario la pone la UI.
    /// </summary>
    [HttpDelete("{harvestId:guid}")]
    public async Task<IActionResult> Delete(Guid harvestId, CancellationToken ct)
    {
        if (!IfMatchHeader.TryRead(Request.Headers, out var expectedVersion))
            return BadRequest(new ApiErrorResponse(ApiError.IfMatchRequired()));

        try
        {
            var deleted = await deleteHarvestHandler.HandleAsync(
                new DeleteHarvestCommand(
                    workspaceContext.WorkspaceId, User.GetUserId()!.Value, harvestId, expectedVersion),
                ct);

            return deleted
                ? NoContent()
                : NotFound(new ApiErrorResponse(ApiError.HarvestNotFound()));
        }
        catch (ConcurrencyConflictException ex)
        {
            return VersionConflict(ex);
        }
    }

    /// <summary>
    /// <c>409</c> del contrato con la versión vigente en el cuerpo, para que el cliente pueda resolver
    /// el conflicto refrescando en vez de dejar al usuario sin salida (CA-5).
    /// </summary>
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

    /// <summary>
    /// MVP-402 — Lectura de un valor que <b>no es un campo del recurso</b> sino un modificador de la
    /// petición (la unidad de <c>yield</c>, RN-014). Por eso no es un <see cref="FieldUpdate{T}"/>:
    /// ausente significa «la canónica», no «conserva la anterior».
    /// </summary>
    private static string? ReadPlainString(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return null;
        if (el.ValueKind == JsonValueKind.Null) return null;
        if (el.ValueKind == JsonValueKind.String) return JsonText.Read(el, key);

        throw new HarvestValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser texto.");
    }

    private static FieldUpdate<DateOnly> ReadDate(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<DateOnly>.Absent;

        if (el.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(JsonText.Read(el, key), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
            return FieldUpdate<DateOnly>.Set(parsed);

        throw new HarvestValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser una fecha YYYY-MM-DD.");
    }

    private static FieldUpdate<Guid> ReadGuid(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<Guid>.Absent;
        if (el.ValueKind == JsonValueKind.String && Guid.TryParse(JsonText.Read(el, key), out var parsed))
            return FieldUpdate<Guid>.Set(parsed);

        throw new HarvestValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser un identificador válido.");
    }

    private static FieldUpdate<decimal> ReadDecimal(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<decimal>.Absent;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var parsed))
            return FieldUpdate<decimal>.Set(parsed);

        throw new HarvestValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser numérico.");
    }

    /// <summary>
    /// Igual que <see cref="ReadDecimal"/> pero admite <c>null</c> explícito: es lo que permite
    /// <b>retirar</b> el rendimiento o los litros de una cosecha que ya los tenía (par excluyente de
    /// RN-004).
    /// </summary>
    private static FieldUpdate<decimal?> ReadNullableDecimal(Dictionary<string, JsonElement> body, string key)
    {
        if (!body.TryGetValue(key, out var el)) return FieldUpdate<decimal?>.Absent;
        if (el.ValueKind == JsonValueKind.Null) return FieldUpdate<decimal?>.Set(null);
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var parsed))
            return FieldUpdate<decimal?>.Set(parsed);

        throw new HarvestValidationException(
            ErrorCodes.ValidationRequired, $"El campo '{key}' debe ser numérico o null.");
    }

    private static object ToResponse(HarvestView harvest) => new
    {
        id = harvest.Id,
        workspace_id = harvest.WorkspaceId,
        date = harvest.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        plot_id = harvest.PlotId,
        plot_name = harvest.PlotName,
        season_id = harvest.SeasonId,
        season_name = harvest.SeasonName,
        product = harvest.Product,
        kgs = harvest.Kgs,
        // RN-004 — como mucho uno de los dos viene informado; el otro llega `null`.
        // RN-013 — `yield` va siempre en la unidad canónica L/100kg, sea cual sea la unidad en la que
        // se informó (RN-014): lo que se guarda y lo que compara el dashboard es lo mismo.
        yield = harvest.Yield,
        liters = harvest.Liters,
        // MVP-402 — Rendimiento venga de donde venga: el informado, o el derivado de los litros
        // (RN-014, tercer origen). Evita que cada consumidor rehaga la división.
        effective_yield = harvest.EffectiveYield,
        yield_source = harvest.YieldSource,
        destination = harvest.Destination,
        // MVP-707 — Precio por kilo e importe. `null` en los dos significa **no se sabe**, no cero: una
        // partida sin precio no ha ingresado 0 € (CA-2).
        unit_price = harvest.UnitPrice,
        // Derivado, nunca columna: guardarlo permitiría que divergiera de kilos × precio (CA-3).
        amount = harvest.Amount,
        // RN-023 — aviso no bloqueante de fecha fuera del rango de la temporada (CA-3).
        is_out_of_season_range = harvest.IsOutOfSeasonRange,
        version = harvest.Version,
        created_at = harvest.CreatedAt,
        updated_at = harvest.UpdatedAt,
        // MVP-804 (RU-21, CA-1) — Quién la apuntó y quién la corrigió por última vez. Solo el nombre:
        // ni correo ni identificador de cuenta, que no hacen falta para responder a la pregunta.
        created_by_name = harvest.CreatedByName(),
        updated_by_name = harvest.UpdatedByName()
    };
}

/// <summary>
/// Alta de cosecha (<c>contratos-api.md §6</c>). Las reglas de negocio —kilos positivos, exclusión de
/// rendimiento y litros, producto y destino— viven en el dominio, no en anotaciones: son reglas de
/// negocio, no de forma.
/// </summary>
public sealed record CreateHarvestRequest(
    [RequiredField(ErrorCodes.ValidationHarvestRequiredFields, "La fecha de la cosecha es obligatoria.")]
    string Date,
    [property: JsonPropertyName("plot_id")] Guid PlotId,
    [property: JsonPropertyName("season_id")] Guid SeasonId,
    string Product,
    decimal Kgs,
    string Destination,
    /// <summary>Rendimiento en la unidad de <see cref="YieldUnit"/>. Excluyente con <see cref="Liters"/> (RN-004).</summary>
    decimal? Yield,
    decimal? Liters,
    /// <summary>
    /// MVP-402 — Unidad de <see cref="Yield"/> (RN-014): <c>l_100kg</c> (canónica, por defecto) o
    /// <c>kg_100kg</c>, que es como dan el rendimiento graso muchas almazaras. Se convierte con la
    /// densidad de RN-016 antes de persistir.
    /// </summary>
    [property: JsonPropertyName("yield_unit")] string? YieldUnit,
    /// <summary>
    /// MVP-707 — Precio de venta por kilo, <b>opcional</b> (RN-029 matizada). Ausente o <c>null</c>
    /// significa que no se sabe; el importe se deriva y no se envía.
    /// </summary>
    [property: JsonPropertyName("unit_price")] decimal? UnitPrice = null);
