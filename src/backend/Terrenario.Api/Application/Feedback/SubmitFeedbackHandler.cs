using Microsoft.Extensions.Options;
using Terrenario.Api.Common;
using Terrenario.Api.Domain.Users;
using Terrenario.Api.Infrastructure.Feedback;

namespace Terrenario.Api.Application.Feedback;

/// <summary>Lo que el cliente aporta de un reporte. El resto del contexto lo pone el servidor.</summary>
/// <param name="Kind">Uno de <see cref="FeedbackKinds"/>.</param>
/// <param name="Message">Texto libre, ya validado por el controller.</param>
/// <param name="Path">Ruta del cliente desde la que se envía, ya saneada.</param>
/// <param name="LastFailedRequestId">Correlación de la última petición fallida, ya validada.</param>
/// <param name="UserAgent">Cabecera <c>User-Agent</c> de esta misma petición.</param>
public sealed record FeedbackSubmission(
    string Kind,
    string Message,
    string? Path,
    string? LastFailedRequestId,
    string? UserAgent);

/// <summary>
/// MVP-711 (HU-1/HU-2) — Envía el reporte al buzón de operación.
///
/// No persiste nada. Es una decisión, no una carencia: el spec deja fuera estados, asignación y
/// seguimiento del reporte dentro del producto, así que una tabla de reportes sería un almacén de
/// texto libre escrito por personas —con lo que eso implica en retención y en derechos— sin nadie
/// que lo consulte. El correo <b>es</b> el registro.
/// </summary>
public sealed class SubmitFeedbackHandler(
    IUserRepository users,
    IFeedbackEmailSender sender,
    IOptions<FeedbackOptions> options,
    ILogger<SubmitFeedbackHandler> logger)
{
    /// <summary>
    /// Hacen falta las dos cosas: un buzón al que escribir y una cuenta desde la que enviar. Sin
    /// cualquiera de ellas el canal se declara no disponible en vez de aceptar el reporte y perderlo.
    /// </summary>
    public bool IsChannelAvailable => options.Value.IsConfigured && sender.IsEnabled;

    /// <summary>
    /// Compone y entrega el reporte. Devuelve <c>false</c> si la cuenta de la sesión ya no existe o
    /// está dada de baja, que es el único caso en el que no hay nada que enviar.
    ///
    /// Los fallos de transporte <b>se propagan</b>, igual que en el resto de emisores (ADR-0010): a
    /// diferencia de una invitación, donde la operación ya se ejecutó y el correo es un extra, aquí
    /// el correo <i>es</i> la operación. Si no sale, hay que decirlo, no confirmar un envío que no
    /// ocurrió (CA-3).
    /// </summary>
    public async Task<bool> HandleAsync(Guid userId, FeedbackSubmission submission, CancellationToken ct)
    {
        var reporter = await users.FindByIdAsync(userId, ct);
        if (reporter is null || reporter.IsDeleted) return false;

        await sender.SendAsync(new FeedbackEmail
        {
            ToEmail = options.Value.Recipient,
            Kind = submission.Kind,
            Message = submission.Message,
            ReporterDisplayName = reporter.DisplayName,
            ReporterEmail = reporter.Email,
            Context = new FeedbackContext(
                DeployedVersion.Current,
                submission.Path,
                submission.LastFailedRequestId,
                submission.UserAgent)
        }, ct);

        // Ni el texto del reporte ni la dirección de quien lo manda: el primero es contenido escrito
        // por una persona y la segunda es PII, y ninguno de los dos pinta nada en la traza. Lo que se
        // deja es lo que sirve para saber que el canal funciona.
        logger.LogInformation("feedback.sent kind={Kind} path={Path}", submission.Kind, submission.Path);

        return true;
    }
}
