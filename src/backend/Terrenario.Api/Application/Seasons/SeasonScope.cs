using System.Globalization;
using Terrenario.Api.Domain.Seasons;

namespace Terrenario.Api.Application.Seasons;

/// <summary>
/// MVP-701 — Ámbito de temporada ya resuelto de una lectura operativa (RN-008).
///
/// <b>Existe porque el defecto no puede vivir en el cliente.</b> Hasta MVP-701 el dashboard lo
/// resolvía en servidor y el diario, las cosechas y las compras arrancaban en «todas» por su cuenta:
/// dos pantallas del producto respondían con cifras distintas a «cuánto llevo esta campaña»
/// (<c>P-082</c>). Con el defecto en un único sitio, no hay dos verdades que puedan divergir.
/// </summary>
/// <param name="Season">
/// Temporada aplicada, o <c>null</c> cuando se está mirando el histórico completo o cuando el
/// Workspace todavía no tiene ninguna.
/// </param>
/// <param name="AllSeasons">
/// El usuario ha pedido el histórico completo de forma explícita (<c>season_id=all</c>), o no hay
/// temporada de trabajo que aplicar. Sin esto, «todas» y «ninguna» se confundirían en la respuesta.
/// </param>
public sealed record SeasonScope(Season? Season, bool AllSeasons)
{
    /// <summary>
    /// Lo que se pasa al filtro de persistencia: <c>null</c> significa «no acotes por temporada».
    /// </summary>
    public Guid? FilterId => AllSeasons ? null : Season?.Id;

    /// <summary>
    /// Forma en que el ámbito viaja en la respuesta, para que la pantalla posicione su control sin
    /// duplicar la regla del defecto (mismo criterio que el <c>scope</c> del dashboard, MVP-403).
    /// </summary>
    public object ToResponse() => new
    {
        season = Season is null
            ? null
            : new
            {
                id = Season.Id,
                name = Season.Name,
                status = Season.StatusOn(DateOnly.FromDateTime(DateTime.UtcNow))
                    .ToString().ToLowerInvariant(),
                start_date = Season.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                end_date = Season.EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            },
        all_seasons = AllSeasons
    };
}

/// <summary>
/// MVP-701 — Resuelve el ámbito de temporada de las lecturas operativas (diario, cosechas y compras)
/// aplicando el defecto de RN-008: sin petición explícita, la <b>temporada de trabajo del usuario</b>
/// (MVP-209).
///
/// El parámetro es texto y no un <c>Guid?</c> porque tiene tres significados y no dos:
/// <list type="bullet">
///   <item><b>ausente</b> — aplica el defecto;</item>
///   <item><see cref="AllToken"/> — histórico completo, elección explícita del usuario;</item>
///   <item><b>identificador</b> — esa temporada concreta.</item>
/// </list>
///
/// <b>Un identificador que no existe en el Workspace no es un error: se cae al defecto.</b> Es la
/// misma tolerancia que el dashboard aplica a los terrenos (MVP-403) y aquí importa más, porque desde
/// MVP-705 el filtro viaja en la URL: al cambiar de Workspace, la URL puede conservar la temporada del
/// anterior, y responder con un error —o peor, con el histórico entero— sería peor que mostrar el
/// ámbito por defecto y decir cuál es.
/// </summary>
public sealed class SeasonScopeResolver(ISeasonRepository seasonRepository)
{
    /// <summary>Valor reservado que pide el histórico completo.</summary>
    public const string AllToken = "all";

    public async Task<SeasonScope> ResolveAsync(
        Guid userId,
        Guid workspaceId,
        string? requested,
        CancellationToken ct = default)
    {
        var raw = requested?.Trim();

        if (string.Equals(raw, AllToken, StringComparison.OrdinalIgnoreCase))
            return new SeasonScope(null, AllSeasons: true);

        if (!string.IsNullOrEmpty(raw) && Guid.TryParse(raw, out var seasonId))
        {
            var requestedSeason = await seasonRepository.FindByIdAsync(workspaceId, seasonId, ct);
            if (requestedSeason is not null) return new SeasonScope(requestedSeason, AllSeasons: false);
        }

        var working = await seasonRepository.FindWorkingSeasonAsync(userId, workspaceId, ct);

        // Sin temporada de trabajo no hay nada por lo que acotar: se muestra todo y se dice que es
        // todo, en vez de devolver una lista vacía que parecería que no hay datos.
        return working is null
            ? new SeasonScope(null, AllSeasons: true)
            : new SeasonScope(working, AllSeasons: false);
    }
}
