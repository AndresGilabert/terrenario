namespace Terrenario.Api.Domain.Seasons;

/// <summary>
/// Estado de una temporada en el maestro (MVP-203). Es un valor <b>derivado</b> de los booleanos
/// canónicos <c>is_active</c>/<c>is_closed</c> (no se persiste una columna de estado): así el maestro
/// formaliza la máquina de estados sin cambiar el esquema introducido en MVP-201.
/// </summary>
public enum SeasonStatus
{
    /// <summary>Ni activa ni cerrada: preparada pero no en uso operativo.</summary>
    Planificada,

    /// <summary>La temporada activa del Workspace (RN-021/RN-022). Solo una a la vez.</summary>
    Activa,

    /// <summary>Cerrada: estado informativo (RN-024). No bloquea altas ni ediciones.</summary>
    Cerrada
}
