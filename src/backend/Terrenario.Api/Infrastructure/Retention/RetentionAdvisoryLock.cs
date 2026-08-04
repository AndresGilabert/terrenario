using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Infrastructure.Data;

namespace Terrenario.Api.Infrastructure.Retention;

/// <summary>
/// MVP-504 (B-3) — Cerrojo que impide que dos instancias de la API purguen a la vez.
///
/// Vive aparte del <see cref="RetentionPurgeWorker"/> para poder probarse: es la pieza que solo falla
/// contra el motor real —la forma de la consulta escalar de EF y el comportamiento del cerrojo son de
/// PostgreSQL—, así que dejarla dentro de un servicio en segundo plano la habría vuelto inverificable.
/// </summary>
public static class RetentionAdvisoryLock
{
    /// <summary>
    /// Clave del cerrojo. Arbitraria pero fija: solo tiene que ser distinta de la de cualquier otra
    /// rutina que en el futuro use el mismo mecanismo.
    /// </summary>
    public const long Key = 504_041L;

    /// <summary>
    /// Intenta cogerlo dentro de la transacción en curso.
    ///
    /// <c>pg_try_advisory_xact_lock</c> y no su versión bloqueante: si otra instancia está purgando,
    /// volver dentro de 24 horas es mejor que dejar un hilo esperando. Al ser de ámbito de
    /// transacción se libera solo al cerrarla, también si algo revienta por el camino.
    ///
    /// Sobre un proveedor que no sea PostgreSQL devuelve <c>true</c> y se sigue sin cerrojo: el
    /// expurgo es idempotente, así que dos pasadas simultáneas se pisan sin corromper nada.
    /// </summary>
    public static async Task<bool> TryAcquireAsync(TerrenarioDbContext db, CancellationToken ct = default)
    {
        if (!db.Database.IsNpgsql()) return true;

        // El alias `Value` es lo que EF espera de una consulta escalar con `SqlQueryRaw`.
        return await db.Database
            .SqlQueryRaw<bool>($"SELECT pg_try_advisory_xact_lock({Key}) AS \"Value\"")
            .SingleAsync(ct);
    }
}
