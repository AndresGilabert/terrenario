namespace Terrenario.Api.Application.Feedback;

/// <summary>
/// MVP-711 (CA-6) — Límite anti-abuso del canal de feedback.
///
/// <b>Por cuenta y en servidor.</b> Deshabilitar el botón en el cliente ordena la pantalla, pero no
/// es un límite: el endpoint está autenticado y cualquiera con una sesión puede llamarlo en bucle.
/// Lo que protege el buzón es esto.
///
/// <b>En memoria, y aquí basta.</b> La API corre en <b>una sola instancia</b> —la misma premisa por
/// la que las migraciones se aplican al arrancar (<c>Program.cs</c>) y por la que el estado de las
/// alertas vive en un singleton (<c>AlertStateStore</c>)—, así que un contador en proceso ve todas
/// las peticiones. Si algún día escala a varias réplicas, el límite pasaría a ser «N por réplica» y
/// habría que llevarlo a la base de datos; queda dicho aquí para que se vea al releerlo y no el día
/// del incidente.
///
/// Reiniciar el proceso borra el contador. Es aceptable: el peor caso es que quien acabe de agotar
/// su cupo pueda mandar tres más, no que el buzón se inunde.
/// </summary>
public sealed class FeedbackRateLimiter(TimeProvider clock)
{
    /// <summary>
    /// Tres reportes por hora y cuenta. El número sale de lo que hace una persona de verdad: contar
    /// un problema, acordarse de un detalle y mandarlo aparte, y poco más. Un cuarto en la misma hora
    /// ya no es uso, es repetición.
    /// </summary>
    public const int MaxPerWindow = 3;

    /// <summary>Ventana deslizante, no cubo por hora natural: cambiar de hora no debe regalar cupo.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromHours(1);

    private readonly Dictionary<Guid, List<DateTimeOffset>> _sentByUser = [];
    private readonly object _gate = new();

    /// <summary>
    /// ¿Le queda cupo a esta cuenta? Devuelve además cuánto falta para que se libere uno, que es lo
    /// que la respuesta necesita para decir «vuelve a intentarlo en X» en vez de solo negarse.
    /// </summary>
    public bool IsAllowed(Guid userId, out TimeSpan retryAfter)
    {
        var now = clock.GetUtcNow();
        retryAfter = TimeSpan.Zero;

        lock (_gate)
        {
            if (!_sentByUser.TryGetValue(userId, out var sent)) return true;

            sent.RemoveAll(instant => now - instant >= Window);
            if (sent.Count < MaxPerWindow) return true;

            // El cupo se libera cuando el más antiguo de los que cuentan sale de la ventana.
            retryAfter = sent[0] + Window - now;
            return false;
        }
    }

    /// <summary>
    /// Anota un envío <b>que ha salido</b>.
    ///
    /// Se llama después de entregar y no antes, a propósito: si el servidor de correo está caído el
    /// reporte no ha llegado a ninguna parte, y gastarle el cupo a quien lo intentó sería castigarle
    /// por un fallo nuestro. El riesgo teórico —vaciar el canal a base de envíos fallidos— no existe:
    /// justamente lo que no está pasando es que salga correo.
    /// </summary>
    public void Register(Guid userId)
    {
        var now = clock.GetUtcNow();

        lock (_gate)
        {
            if (!_sentByUser.TryGetValue(userId, out var sent))
            {
                sent = [];
                _sentByUser[userId] = sent;
            }

            sent.RemoveAll(instant => now - instant >= Window);
            sent.Add(now);

            // Sin esta limpieza el diccionario conservaría una entrada por cada cuenta que reportó
            // alguna vez desde que arrancó el proceso. Se hace aquí y no en la comprobación porque
            // registrar es lo raro (como mucho tres veces por hora y cuenta) y comprobar es lo común.
            foreach (var expired in _sentByUser.Where(entry => entry.Value.Count == 0).ToArray())
                _sentByUser.Remove(expired.Key);
        }
    }
}
