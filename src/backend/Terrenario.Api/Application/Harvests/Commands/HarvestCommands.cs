using Terrenario.Api.Common;

namespace Terrenario.Api.Application.Harvests.Commands;

/// <summary>
/// Alta de cosecha (MVP-401, HU-1). El Workspace y el usuario nunca viajan como parámetros de
/// negocio: se resuelven en servidor desde el contexto de scope y el claim de la sesión (RN-034).
/// </summary>
public sealed record CreateHarvestCommand(
    Guid WorkspaceId,
    Guid UserId,
    DateOnly Date,
    Guid PlotId,
    Guid SeasonId,
    string Product,
    decimal Kgs,
    string Destination,
    /// <summary>Excluyente con <see cref="Liters"/> por RN-004.</summary>
    decimal? Yield,
    decimal? Liters,
    /// <summary>
    /// MVP-402 — Unidad en la que llega <see cref="Yield"/> (RN-014): <c>l_100kg</c> (canónica, por
    /// defecto) o <c>kg_100kg</c>. Se convierte antes de persistir; el agregado solo conoce la canónica.
    /// </summary>
    string? YieldUnit = null);

/// <summary>
/// Edición parcial de cosecha (MVP-401, HU-2, <c>PATCH</c>). Cada campo es un
/// <see cref="FieldUpdate{T}"/>: ausente conserva el valor actual.
///
/// <b><see cref="Yield"/> y <see cref="Liters"/> son un par excluyente</b> (RN-004): si viene
/// <b>cualquiera</b> de los dos se sustituye la pareja completa y el ausente pasa a nulo. Es el mismo
/// criterio que MVP-301 aplicó al par tarea de la actividad: enviar solo <c>liters</c> sobre una
/// cosecha que ya tenía <c>yield</c> dejaría los dos informados y el dominio lo rechazaría, sin que el
/// cliente pudiera hacer nada razonable.
///
/// <see cref="ExpectedVersion"/> llega de la cabecera <c>If-Match</c> (ADR-0005).
/// </summary>
public sealed record UpdateHarvestCommand(
    Guid WorkspaceId,
    Guid UserId,
    Guid HarvestId,
    long ExpectedVersion,
    FieldUpdate<DateOnly> Date,
    FieldUpdate<Guid> PlotId,
    FieldUpdate<Guid> SeasonId,
    FieldUpdate<string> Product,
    FieldUpdate<decimal> Kgs,
    FieldUpdate<string> Destination,
    FieldUpdate<decimal?> Yield,
    FieldUpdate<decimal?> Liters,
    /// <summary>
    /// MVP-402 — Unidad de <see cref="Yield"/> en esta petición (RN-014). No es un campo del recurso:
    /// lo persistido es siempre la unidad canónica, así que no tiene sentido «conservarlo» entre
    /// ediciones y por eso no es un <see cref="FieldUpdate{T}"/>.
    /// </summary>
    string? YieldUnit = null);

/// <summary>
/// Eliminación <b>lógica</b> de una cosecha (RN-037). La confirmación explícita es responsabilidad de
/// la UI; aquí llega ya confirmada, pero con la versión vigente (ADR-0005).
/// </summary>
public sealed record DeleteHarvestCommand(
    Guid WorkspaceId,
    Guid UserId,
    Guid HarvestId,
    long ExpectedVersion);
