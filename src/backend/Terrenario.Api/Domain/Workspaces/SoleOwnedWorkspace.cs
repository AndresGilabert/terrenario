namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// MVP-206 (HU-3, CA-9) — Workspace vivo cuyo único propietario activo es el usuario consultado.
/// Es la unidad de la regla de no-orfandad: la baja de cuenta no puede completarse mientras quede
/// alguno sin resolver. <see cref="OtherActiveMembers"/> anticipa si cabe el traspaso (hay a quién
/// dársela) o si la única salida es la baja lógica.
/// </summary>
public sealed record SoleOwnedWorkspace(Guid WorkspaceId, string Name, int OtherActiveMembers);
