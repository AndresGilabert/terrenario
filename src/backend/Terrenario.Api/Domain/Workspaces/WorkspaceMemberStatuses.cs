namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Estados de membresía del catálogo cerrado <c>worker_member_status</c>
/// (<c>docs/02-arquitectura/contratos-api.md</c>). Los valores van en español por ser
/// vocabulario de dominio (ADR-0009); el nombre del catálogo va en inglés.
/// </summary>
public static class WorkspaceMemberStatuses
{
    /// <summary>Persona con invitación pendiente que todavía no ha entrado al Workspace.</summary>
    public const string Invited = "invitado";

    /// <summary>Membresía vigente: es la única que da acceso al Workspace.</summary>
    public const string Active = "activo";

    /// <summary>Membresía retirada. No aparece en el selector ni resuelve contexto activo.</summary>
    public const string Revoked = "revocado";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Invited, Active, Revoked };

    public static bool IsValid(string? status) => status is not null && All.Contains(status);
}
