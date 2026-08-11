using System.Text;

namespace Terrenario.Api.Domain.Masters;

/// <summary>
/// Cuántos registros referencian a una fila de maestro, <b>desglosado por tipo de referencia</b>
/// (MVP-806, CA-2).
///
/// El desglose no es adorno: el mensaje de error tiene que decir <i>cuántos</i> registros lo
/// referencian, y «3 registros» no orienta a nadie que tenga que ir a buscarlos. «2 actividades y 1
/// cosecha» sí. Además es lo que hace visible en la propia respuesta si una de las referencias se
/// hubiera dejado sin comprobar.
/// </summary>
/// <param name="References">
/// Una entrada por tipo de referencia con recuento mayor que cero, en el orden declarado en el mapa de
/// referencias.
/// </param>
public sealed record MasterUsage(IReadOnlyList<MasterUsageReference> References)
{
    public static readonly MasterUsage None = new([]);

    public int Total => References.Sum(r => r.Count);

    /// <summary>
    /// ¿Hay algo que impida el borrado físico? Solo cuentan las referencias <b>operativas</b>: la
    /// temporada de trabajo de un miembro (<c>workspace_members.active_season_id</c>) es una
    /// preferencia con <c>ON DELETE SET NULL</c>, no histórico, y su desaparición se resuelve sola
    /// cayendo al defecto (<c>WorkingSeasonPolicy</c>).
    /// </summary>
    public bool IsUsed => Total > 0;

    /// <summary>
    /// Desglose legible: «2 actividades y 1 cosecha». Va tal cual en el mensaje del error 422, que es
    /// lo que el contrato promete y lo que la UI muestra sin reescribir.
    /// </summary>
    public string Describe()
    {
        if (References.Count == 0) return "ningún registro";

        var parts = References.Select(r => $"{r.Count} {(r.Count == 1 ? r.SingularLabel : r.PluralLabel)}").ToList();
        if (parts.Count == 1) return parts[0];

        var text = new StringBuilder(string.Join(", ", parts.Take(parts.Count - 1)));
        text.Append(" y ").Append(parts[^1]);
        return text.ToString();
    }
}

/// <param name="SingularLabel">«actividad»</param>
/// <param name="PluralLabel">«actividades»</param>
public sealed record MasterUsageReference(string SingularLabel, string PluralLabel, int Count);

/// <summary>
/// Lo mínimo que la depuración necesita saber de una fila de maestro, sea del maestro que sea: cómo se
/// llama —para poder nombrarla en la confirmación— y si su identidad la gobierna una cuenta.
/// </summary>
/// <param name="IsIdentityManaged">
/// Cierto solo en un responsable con cuenta (MVP-208): su nombre lo fija Google (RN-036) y su
/// disponibilidad la membresía, así que ni se borra a mano ni puede ser el absorbido de una fusión.
/// </param>
public sealed record MasterRecord(Guid Id, string Name, bool IsIdentityManaged);
