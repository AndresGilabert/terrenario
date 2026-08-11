using Terrenario.Api.Domain.Users;

namespace Terrenario.Api.Domain.Operations;

/// <summary>
/// MVP-804 (<c>RU-21</c>) — Autoría de un registro operativo: quién lo apuntó, quién hizo la
/// <b>última</b> corrección y cuándo.
///
/// Las cuatro vistas de lectura operativas —<c>ActivityView</c>, <c>HarvestView</c>,
/// <c>PurchaseView</c> y <c>ConsumptionView</c>— la implementan, de modo que la regla de cómo se
/// nombra a un autor vive en <see cref="RecordAuthor"/> y no en cuatro sitios.
///
/// Pesa por <c>RN-034</c>: en el MVP los permisos son planos, así que cualquier miembro puede corregir
/// el registro de cualquier otro. Sin esto, ante una cifra que no cuadra no hay forma de saber quién la
/// apuntó salvo preguntar uno por uno.
///
/// <b>No se guarda histórico</b>: <c>RU-21</c> lo excluye expresamente. Solo la última edición.
/// </summary>
public interface IAuthoredRecord
{
    /// <summary>
    /// Nombre de la cuenta que creó el registro, o <c>null</c> cuando ya <b>no hay a quién nombrar</b>.
    /// Lo resuelve la proyección de lectura; ver <see cref="RecordAuthor.NameOf"/> para los dos casos
    /// en que llega nulo.
    /// </summary>
    string? CreatedByAccountName { get; }

    DateTimeOffset CreatedAt { get; }

    /// <summary>Igual que <see cref="CreatedByAccountName"/>, para la última corrección.</summary>
    string? UpdatedByAccountName { get; }

    DateTimeOffset UpdatedAt { get; }
}

/// <summary>
/// MVP-804 — Cómo se nombra al autor de un registro operativo.
///
/// La única regla que hay, y por eso está en un solo sitio: <b>sin cuenta viva que nombrar, «Cuenta
/// eliminada»</b>. Nunca un hueco y nunca el nombre que la cuenta tuvo.
/// </summary>
public static class RecordAuthor
{
    /// <summary>
    /// Nombre visible del autor.
    ///
    /// <paramref name="accountName"/> llega nulo en los <b>dos</b> casos en que la cuenta ya no
    /// identifica a nadie, y los dos se rotulan igual:
    /// <list type="number">
    /// <item><b>Cuenta dada de baja</b> (<c>MVP-505</c>): la fila sobrevive anonimizada justo para que
    /// el histórico operativo de terceros no pierda su autoría, pero sus datos personales ya no
    /// existen.</item>
    /// <item><b>Cuenta purgada</b> al vencer el plazo de <c>RN-041</c>: las tablas operativas no tienen
    /// FK hacia <c>users</c>, así que <c>created_by</c> puede quedar apuntando a una fila que ya no
    /// está. Un <c>LEFT JOIN</c> devuelve nulo y aquí acaba igual que el caso anterior.</item>
    /// </list>
    ///
    /// La proyección devuelve nulo <b>en cuanto la cuenta está dada de baja</b>, sin mirar qué guarda
    /// su <c>display_name</c>. Es deliberadamente redundante con
    /// <see cref="User.Anonymize"/> —que ya escribe ahí este mismo texto—: una funcionalidad de lectura
    /// nueva es justo por donde se escapa un dato personal, y así el camino de lectura no depende de
    /// que la escritura de la baja hiciera su trabajo.
    /// </summary>
    public static string NameOf(string? accountName)
        => string.IsNullOrWhiteSpace(accountName) ? User.AnonymizedDisplayName : accountName;

    /// <summary>Nombre de quien creó el registro (<c>CA-1</c>).</summary>
    public static string CreatedByName(this IAuthoredRecord record) => NameOf(record.CreatedByAccountName);

    /// <summary>Nombre de quien hizo la última corrección (<c>CA-1</c>).</summary>
    public static string UpdatedByName(this IAuthoredRecord record) => NameOf(record.UpdatedByAccountName);
}
