namespace Terrenario.Api.Infrastructure.Email;

/// <summary>
/// Aviso a un miembro de que el Workspace se ha dado de baja, con el enlace de un solo uso para
/// solicitar su traspaso y reactivación (MVP-206, CA-6).
/// </summary>
public sealed record WorkspaceClosedEmail(
    string ToEmail,
    string WorkspaceName,
    string? ClosedByDisplayName,
    string ReactivationUrl);

/// <summary>
/// Aviso a quien dio de baja el Workspace de que un miembro pide recuperarlo (MVP-206, HU-6). Es la
/// vía por la que se entera de que tiene una decisión pendiente; el enlace lleva a su bandeja, no a
/// una acción directa: autorizar exige entrar con su cuenta.
/// </summary>
public sealed record ReactivationRequestedEmail(
    string ToEmail,
    string WorkspaceName,
    string RequesterDisplayName,
    string AuthorizationsUrl);

/// <summary>
/// MVP-206 — Correos del ciclo de vida del Workspace. Segundo consumidor del <c>email-service</c>
/// tras las invitaciones (MVP-103); comparte transporte y configuración de cuenta.
/// </summary>
public interface IWorkspaceLifecycleEmailSender
{
    /// <summary><c>false</c> mientras no haya cuenta de envío configurada.</summary>
    bool IsEnabled { get; }

    /// <summary>Debe lanzar si el envío falla: el caso de uso lo refleja sin invalidar la baja.</summary>
    Task SendWorkspaceClosedAsync(WorkspaceClosedEmail message, CancellationToken ct = default);

    Task SendReactivationRequestedAsync(ReactivationRequestedEmail message, CancellationToken ct = default);
}
