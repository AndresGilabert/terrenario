namespace Terrenario.Api.Infrastructure.Email;

/// <summary>
/// MVP-206 — Adaptador real de los correos del ciclo de vida del Workspace sobre el transporte SMTP
/// común (ADR-0010). Como en las invitaciones, las excepciones se propagan: el caso de uso decide
/// que un fallo del proveedor no invalida la baja ya ejecutada.
/// </summary>
public sealed class SmtpWorkspaceLifecycleEmailSender(SmtpMailer mailer, ProductEmailTemplate template)
    : IWorkspaceLifecycleEmailSender
{
    public bool IsEnabled => mailer.IsEnabled;

    public Task SendWorkspaceClosedAsync(WorkspaceClosedEmail message, CancellationToken ct = default)
        => mailer.SendAsync(
            WorkspaceLifecycleEmailComposer.ComposeWorkspaceClosed(template, message),
            "baja de Workspace",
            ct);

    public Task SendReactivationRequestedAsync(ReactivationRequestedEmail message, CancellationToken ct = default)
        => mailer.SendAsync(
            WorkspaceLifecycleEmailComposer.ComposeReactivationRequested(template, message),
            "solicitud de reactivación",
            ct);
}
