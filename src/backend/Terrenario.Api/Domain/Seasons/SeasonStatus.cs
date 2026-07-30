namespace Terrenario.Api.Domain.Seasons;

/// <summary>
/// Estado <b>informativo</b> de una temporada (MVP-209). Es un valor <b>derivado</b> de <c>is_closed</c>
/// y de la fecha de inicio frente a hoy —no se persiste una columna de estado— y es <b>independiente</b>
/// de cuál sea la temporada de trabajo del usuario: describe en qué punto de su vida está la campaña, no
/// si se está registrando sobre ella.
///
/// Sobre las tres se puede añadir, editar y borrar (RN-024): el estado no bloquea la operativa.
/// </summary>
public enum SeasonStatus
{
    /// <summary>No cerrada y aún no iniciada (<c>start_date &gt; hoy</c>): preparada, esperando.</summary>
    Planificada,

    /// <summary>
    /// No cerrada y ya iniciada (<c>start_date &lt;= hoy</c>). Incluye campañas pasadas no cerradas:
    /// siguen abiertas a registros que llegan tarde (p. ej. el rendimiento meses después).
    /// </summary>
    Abierta,

    /// <summary>Cerrada a mano (<c>is_closed</c>). Estado informativo (RN-024): no bloquea altas ni ediciones.</summary>
    Cerrada
}
