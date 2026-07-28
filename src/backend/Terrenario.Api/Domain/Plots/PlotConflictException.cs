namespace Terrenario.Api.Domain.Plots;

/// <summary>
/// Conflicto del maestro de terrenos (MVP-207, CA-2): ya existe otro terreno con el mismo nombre en
/// el Workspace, ignorando mayúsculas y espacios sobrantes. El terreno es la unidad a la que se
/// asocia todo registro operativo (RN-001): dos parcelas «Prueba» hacen ambigua cualquier actividad,
/// cosecha o compra imputada después.
///
/// La guarda es sobre <see cref="Plot.Name"/>, no sobre el alias (que es un apodo libre y puede
/// repetirse), y cubre <b>todo</b> el maestro, también los terrenos inactivos.
///
/// Se traduce a <c>409 Conflict</c> en el borde de transporte.
/// </summary>
public sealed class PlotConflictException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
