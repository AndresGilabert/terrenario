namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Proyección de lectura de la membresía junto al Workspace al que da acceso. Alimenta el
/// selector de Workspace activo (MVP-104, HU-1) sin exponer el agregado completo.
/// </summary>
public sealed record WorkspaceMembership(
    Guid WorkspaceId,
    string Name,
    string Role,
    string Status,
    DateTimeOffset JoinedAt);
