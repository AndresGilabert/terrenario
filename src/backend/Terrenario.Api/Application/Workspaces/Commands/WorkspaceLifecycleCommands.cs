using Terrenario.Api.Domain.Workspaces;

namespace Terrenario.Api.Application.Workspaces.Commands;

/// <summary>
/// MVP-206 — Casos del árbol de decisión de la baja de un Workspace. Los calcula el servidor para
/// que la UI no tenga que reimplementar la regla de propiedad y pueda **exigir la decisión** al
/// propietario único (CA-3) con el diálogo correcto.
/// </summary>
public static class WorkspaceClosureModes
{
    /// <summary>Hay otros propietarios activos: la baja reasigna y el solicitante sale (CA-5).</summary>
    public const string AutoTransfer = "auto_transfer";

    /// <summary>Propietario único con más miembros: hay que elegir entre traspasar o dar de baja (CA-3).</summary>
    public const string Choose = "choose";

    /// <summary>Propietario único sin nadie más: solo cabe la baja lógica.</summary>
    public const string OnlyDelete = "only_delete";

    /// <summary>Quien consulta no es propietario: no puede dar de baja ni traspasar.</summary>
    public const string NotOwner = "not_owner";
}

/// <summary>
/// Persona a la que se puede traspasar la propiedad: miembro activo distinto de quien actúa (CA-4).
/// </summary>
public sealed record OwnershipCandidate(Guid UserId, string DisplayName, string Email, string Role);

/// <summary>
/// Qué puede hacer quien consulta con el Workspace activo (MVP-206, HU-2/HU-3/HU-4).
/// <see cref="SuccessorDisplayName"/> solo viene en <c>auto_transfer</c>: es el copropietario al que
/// pasaría el Workspace, para poder decirlo en la confirmación en vez de dar a entender un borrado.
/// </summary>
public sealed record WorkspaceClosureOptions(
    Guid WorkspaceId,
    string WorkspaceName,
    bool IsOwner,
    string Mode,
    int ActiveOwners,
    string? SuccessorDisplayName,
    IReadOnlyList<OwnershipCandidate> Candidates);

/// <summary>Resultado de la baja: distingue el traspaso automático (CA-5) de la baja lógica (CA-2).</summary>
public sealed record WorkspaceClosureResult(
    string Outcome,
    Guid WorkspaceId,
    string WorkspaceName,
    string? NewOwnerDisplayName,
    int NotifiedMembers,
    int EmailsSent);

public static class WorkspaceClosureOutcomes
{
    /// <summary>El Workspace sigue vivo con otro propietario; quien actuó ha salido (CA-5).</summary>
    public const string Transferred = "transferred";

    /// <summary>Baja lógica ejecutada; los miembros reciben el enlace de reactivación (CA-2/CA-6).</summary>
    public const string Deleted = "deleted";
}

/// <summary>Traspaso explícito elegido por el propietario único (CA-4): elige a quién.</summary>
public sealed record TransferOwnershipCommand(
    Guid WorkspaceId,
    Guid ActingUserId,
    Guid NewOwnerUserId);

/// <summary>Baja del Workspace activo lanzada por su propietario (HU-2/HU-4).</summary>
public sealed record CloseWorkspaceCommand(
    Guid WorkspaceId,
    string WorkspaceName,
    Guid ActingUserId,
    string? ActingDisplayName);

/// <summary>
/// MVP-206 (HU-3, CA-9) — Workspaces que la baja de cuenta obliga a resolver antes de completarse.
/// <see cref="SoleOwnedWorkspace.OtherActiveMembers"/> indica si cabe traspasar o si solo queda la
/// baja lógica.
/// </summary>
public sealed record OwnershipObligations(IReadOnlyList<SoleOwnedWorkspace> Workspaces)
{
    public bool IsClear => Workspaces.Count == 0;
}
