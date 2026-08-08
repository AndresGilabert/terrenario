using Terrenario.Api.Infrastructure.Email;

namespace Terrenario.Api.Infrastructure.Feedback;

/// <summary>
/// MVP-711 — Adaptador real del canal de feedback sobre el transporte SMTP común (ADR-0010) y la
/// plantilla común de correos (<c>MVP-715</c>). No decide nada: compone y entrega.
/// </summary>
public sealed class SmtpFeedbackEmailSender(SmtpMailer mailer, ProductEmailTemplate template)
    : IFeedbackEmailSender
{
    public bool IsEnabled => mailer.IsEnabled;

    public Task SendAsync(FeedbackEmail message, CancellationToken ct = default)
        => mailer.SendAsync(FeedbackEmailComposer.Compose(template, message), "feedback del usuario", ct);
}
