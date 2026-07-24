using Terrenario.Api.Application.Workspaces.Commands;

namespace Terrenario.Api.Application.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken);

public sealed record RefreshTokenResult(string AccessToken, int ExpiresIn, WorkspaceSummary? Workspace);
