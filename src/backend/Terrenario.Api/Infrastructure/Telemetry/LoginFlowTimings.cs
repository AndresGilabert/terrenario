using System.Collections.Concurrent;

namespace Terrenario.Api.Infrastructure.Telemetry;

/// <summary>
/// MVP-601 — Recuerda cuándo empezó cada intento de login para poder medir el «tiempo medio de login
/// exitoso» (SLO &lt;= 45 s). Es lo único que necesita estado entre eventos del embudo.
///
/// <b>Vive en memoria y solo mientras dura el intento</b>: guarda un <c>flow_id</c> aleatorio y un
/// instante, nada más, y se borra al cerrarse el intento. Persistirlo habría convertido una medida de
/// producto en un dato conservado, que es justo lo que <c>RN-042</c> pide no hacer sin motivo.
///
/// Se acota por tamaño y por edad: un cliente que emitiera «pantalla vista» en bucle no puede hacer
/// crecer esto sin límite.
/// </summary>
public sealed class LoginFlowTimings(TimeProvider clock)
{
    /// <summary>
    /// Un intento que lleva más de esto abierto ya no sirve para medir: o se abandonó, o el usuario
    /// dejó la pestaña olvidada. Contarlo como duración de login falsearía la media hacia arriba.
    /// </summary>
    public static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(30);

    /// <summary>Tope de intentos vivos a la vez. Por encima, se deja de admitir hasta que la poda libere.</summary>
    public const int MaxTrackedFlows = 10_000;

    private readonly ConcurrentDictionary<string, DateTimeOffset> _startedAt = new(StringComparer.Ordinal);

    /// <summary>Marca el inicio del intento. Repetir «pantalla vista» no reinicia el reloj.</summary>
    public void Start(string flowId)
    {
        // La poda solo hace falta cuando el diccionario empieza a crecer: la caducidad real la aplica
        // `Complete`, así que barrer en cada evento sería trabajo por nada.
        if (_startedAt.Count >= MaxTrackedFlows / 2) Prune();
        if (_startedAt.Count >= MaxTrackedFlows) return;

        _startedAt.TryAdd(flowId, clock.GetUtcNow());
    }

    /// <summary>
    /// Cierra el intento y devuelve lo que duró, o <c>null</c> si no se conocía su inicio (por ejemplo
    /// tras un reinicio). Devolver <c>null</c> y no cero es deliberado: quien mide debe poder
    /// distinguir «duró 0 ms» de «no lo sé».
    /// </summary>
    public TimeSpan? Complete(string flowId)
    {
        if (!_startedAt.TryRemove(flowId, out var startedAt)) return null;

        var elapsed = clock.GetUtcNow() - startedAt;
        return elapsed < TimeSpan.Zero || elapsed > MaxAge ? null : elapsed;
    }

    /// <summary>Descarta el intento sin medirlo (error o abandono).</summary>
    public void Discard(string flowId) => _startedAt.TryRemove(flowId, out _);

    private void Prune()
    {
        var limit = clock.GetUtcNow() - MaxAge;

        foreach (var (flowId, startedAt) in _startedAt)
        {
            if (startedAt < limit) _startedAt.TryRemove(flowId, out _);
        }
    }
}
