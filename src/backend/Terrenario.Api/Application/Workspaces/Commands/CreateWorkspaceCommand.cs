namespace Terrenario.Api.Application.Workspaces.Commands;

public sealed record CreateWorkspaceCommand(Guid UserId, string? DisplayName, string Name);

/// <summary>
/// Datos mínimos del Workspace que el cliente necesita para continuar el onboarding (CA-3).
/// </summary>
public sealed record WorkspaceSummary(Guid Id, string Name);

/// <summary>
/// El access token se reemite con el Workspace recién creado como contexto activo (CA-2),
/// de modo que el cliente no necesita un login o refresh adicional.
/// </summary>
public sealed record CreateWorkspaceResult(WorkspaceSummary Workspace, string AccessToken, int ExpiresIn);
