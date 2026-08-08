namespace Terrenario.Api.Infrastructure.Feedback;

/// <summary>
/// MVP-711 — A dónde llega lo que la gente cuenta desde el producto.
///
/// <b>El destinatario es un secreto de despliegue, no configuración versionada</b>, por el mismo
/// motivo que <c>Ops:AlertEmail</c>: el repositorio es público y una dirección commiteada se queda
/// en el historial de git, donde la recogen los rastreadores de spam. En <c>appsettings.json</c>
/// queda vacío: la sección declara su forma, no su valor.
///
/// Vacío el canal <b>no existe</b> (la API responde que no está disponible) en vez de tragarse los
/// reportes en silencio, y el arranque lo advierte igual que con la cuenta de envío y con el
/// destinatario de alertas.
/// </summary>
public sealed class FeedbackOptions
{
    public const string SectionName = "Feedback";

    /// <summary>
    /// Buzón de operación que recibe las incidencias y sugerencias. Puede ser el mismo que
    /// <c>Ops:AlertEmail</c> o uno distinto: no se deriva de él a propósito, porque una bandeja de
    /// alertas automáticas y una de mensajes escritos por personas se atienden de forma distinta.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Recipient);
}
