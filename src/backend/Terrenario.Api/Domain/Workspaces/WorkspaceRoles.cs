namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Roles de membresía definidos en <c>docs/07-seguridad/autenticacion-autorizacion.md</c>.
/// En MVP los permisos son planos (RN-034): el rol es informativo y no restringe operaciones.
/// </summary>
public static class WorkspaceRoles
{
    public const string Owner = "workspace_owner";
    public const string Member = "workspace_member";
}
