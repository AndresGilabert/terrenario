namespace Terrenario.Api.Infrastructure.Feedback;

/// <summary>
/// MVP-711 — Salida del canal de feedback, con el mismo trato que los otros emisores del producto
/// (<c>IInvitationEmailSender</c>, <c>IWorkspaceLifecycleEmailSender</c>): el caso de uso conoce la
/// intención —«manda esto»— y no el transporte.
///
/// A diferencia de aquellos, aquí <b>los fallos se propagan hasta la respuesta</b>: en una invitación
/// el correo acompaña a una operación que ya se ejecutó, mientras que aquí el correo <i>es</i> la
/// operación, y confirmar un envío que no ocurrió sería mentirle a quien acaba de pedir ayuda.
/// </summary>
public interface IFeedbackEmailSender
{
    /// <summary>Hay cuenta de envío configurada.</summary>
    bool IsEnabled { get; }

    Task SendAsync(FeedbackEmail message, CancellationToken ct = default);
}
