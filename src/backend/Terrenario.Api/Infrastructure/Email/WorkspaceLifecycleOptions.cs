namespace Terrenario.Api.Infrastructure.Email;

/// <summary>
/// MVP-206 — Configuración del enlace de solicitud de traspaso y reactivación que reciben los
/// miembros cuando se da de baja un Workspace (CA-6). Mismo patrón que
/// <c>Invitations</c> (MVP-103): base pública del enlace y vigencia en días.
/// </summary>
public sealed class WorkspaceLifecycleOptions
{
    public const string SectionName = "WorkspaceLifecycle";

    /// <summary>Vigencia del enlace de reactivación (CA-10: de un solo uso y con caducidad).</summary>
    public int ReactivationLifetimeDays { get; set; } = 7;

    /// <summary>Base pública del enlace; se le añade <c>/{token}</c>.</summary>
    public string ReactivationBaseUrl { get; set; } = "http://localhost:5173/reactivations";

    public TimeSpan ReactivationLifetime => TimeSpan.FromDays(ReactivationLifetimeDays);

    public string BuildReactivationUrl(string token) => $"{ReactivationBaseUrl.TrimEnd('/')}/{token}";

    /// <summary>
    /// Bandeja de quien dio de baja el Workspace, donde autoriza o deniega las solicitudes (HU-6).
    /// No lleva token: la autorización se comprueba por la cuenta autenticada, no por el enlace.
    /// </summary>
    public string BuildAuthorizationsUrl() => ReactivationBaseUrl.TrimEnd('/');
}
