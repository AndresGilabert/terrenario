using System.Text.Json.Serialization;
using Terrenario.Api.Application.Masters;

namespace Terrenario.Api.Controllers;

/// <summary>
/// Cuerpo de <c>POST /api/v1/{maestro}/{id}/merge</c> (MVP-806). El identificador de la ruta es el de
/// la ficha que <b>sobrevive</b>; el del cuerpo, el de la que se absorbe y desaparece.
///
/// No lleva <c>If-Match</c>: los maestros no tienen versión. El control de concurrencia de la fusión
/// actúa sobre los registros operativos que se reapuntan, no sobre las dos fichas (ADR-0005).
/// </summary>
public sealed record MergeMasterRequest(
    [property: JsonPropertyName("absorbed_id")] Guid AbsorbedId);

/// <summary>
/// Respuesta de una fusión. Devuelve las dos fichas por su nombre —no solo sus identificadores— para
/// que el aviso de la interfaz pueda decir qué pasó sin volver a consultar el maestro, y el número de
/// registros reapuntados, que es lo que hace comprobable el CA-3.
/// </summary>
public sealed record MasterMergeResponse(
    [property: JsonPropertyName("survivor_id")] Guid SurvivorId,
    [property: JsonPropertyName("survivor_name")] string SurvivorName,
    [property: JsonPropertyName("absorbed_id")] Guid AbsorbedId,
    [property: JsonPropertyName("absorbed_name")] string AbsorbedName,
    [property: JsonPropertyName("reassigned_count")] int ReassignedCount)
{
    public static MasterMergeResponse From(MasterMergeResult result) => new(
        result.Survivor.Id,
        result.Survivor.Name,
        result.Absorbed.Id,
        result.Absorbed.Name,
        result.ReassignedCount);
}
