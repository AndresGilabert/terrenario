namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// El usuario intenta situarse en un Workspace en el que no tiene membresía activa. No se
/// distingue entre Workspace inexistente, ajeno o con la membresía revocada: revelar cuál de los
/// tres es delataría la existencia de Workspaces de otras explotaciones.
/// </summary>
public sealed class WorkspaceAccessDeniedException(string message) : Exception(message);
