namespace Terrenario.Api.Infrastructure.Feedback;

/// <summary>
/// MVP-711 — Catálogo cerrado <c>feedback_kind</c>. Los valores son vocabulario de negocio y van en
/// español (ADR-0009); el nombre del catálogo, en inglés.
///
/// Solo dos, y no una lista de categorías: quien está atascado no debería tener que clasificar su
/// problema. La única distinción que aporta algo al triaje es «algo no funciona» frente a «me
/// gustaría que».
/// </summary>
public static class FeedbackKinds
{
    public const string Incident = "incidencia";
    public const string Suggestion = "sugerencia";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Incident, Suggestion };
}

/// <summary>
/// MVP-711 — El contexto técnico que acompaña al reporte (HU-2), y <b>solo</b> ese.
///
/// Lo que hay aquí responde a dos preguntas: <i>dónde estaba</i> y <i>qué petición falló</i>. Lo que
/// deliberadamente no hay es <b>nada de la explotación</b>: ni Workspace, ni temporada, ni filtros,
/// ni identificadores de registros. Reproducir un fallo de interfaz no necesita saber cuántos kilos
/// se cosecharon, y un canal de soporte no es una vía lateral para sacar datos operativos a un buzón
/// de correo.
/// </summary>
/// <param name="AppVersion">Versión que sirve la instancia, resuelta en servidor.</param>
/// <param name="Path">
/// Ruta del cliente desde la que se envía (<c>/app/diario</c>). <b>Sin query ni fragmento</b>: los
/// filtros del panel viajan en la URL desde <c>MVP-403</c> y llevan identificadores de terreno, que
/// son datos del Workspace.
/// </param>
/// <param name="LastFailedRequestId">
/// <c>X-Request-Id</c> de la última petición fallida de la sesión, si la hubo. Es lo que permite
/// saltar del reporte a la traza del servidor sin una conversación de ida y vuelta (P-006).
/// </param>
/// <param name="UserAgent">
/// Cadena de agente de usuario, leída de la <b>cabecera de la petición</b> y no de un campo que
/// mande el cliente: es un dato que el servidor ya tiene y que así no se puede falsear en el cuerpo.
/// </param>
public sealed record FeedbackContext(
    string AppVersion,
    string? Path,
    string? LastFailedRequestId,
    string? UserAgent);

/// <summary>MVP-711 — Todo lo que el correo del canal necesita para componerse.</summary>
public sealed record FeedbackEmail
{
    public required string ToEmail { get; init; }

    /// <summary>Uno de <see cref="FeedbackKinds"/>.</summary>
    public required string Kind { get; init; }

    /// <summary>Lo que ha escrito la persona, tal cual. La plantilla se encarga de escaparlo.</summary>
    public required string Message { get; init; }

    public required string ReporterDisplayName { get; init; }

    /// <summary>
    /// Dirección de la cuenta que reporta. Va en el correo para poder <b>responder</b>: un canal de
    /// soporte del que no se puede contestar deja de serlo en cuanto haga falta una aclaración. Se
    /// avisa en el propio formulario de que se envía, para que no sea una sorpresa.
    /// </summary>
    public required string ReporterEmail { get; init; }

    public required FeedbackContext Context { get; init; }
}
