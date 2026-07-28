namespace Terrenario.Api.Infrastructure.Auth;

public sealed record IssuedAccessToken(string Token, int ExpiresIn);

/// <summary>Claims propietarios emitidos por Terrenario, además de los registrados en JWT.</summary>
public static class TerrenarioClaims
{
    /// <summary>Workspace activo de la sesión. Ausente mientras el usuario no tenga ninguno.</summary>
    public const string WorkspaceId = "workspace_id";
}

public interface IJwtService
{
    IssuedAccessToken IssueAccessToken(Guid userId, string? displayName, Guid? workspaceId = null);
    Guid? ValidateAccessToken(string token);
}
