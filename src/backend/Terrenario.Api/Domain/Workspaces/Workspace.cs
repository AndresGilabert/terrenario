using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Contenedor de negocio multi-tenant. Toda entidad operativa del MVP cuelga de un Workspace.
/// </summary>
public sealed class Workspace
{
    public const int NameMaxLength = 120;

    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Marca de baja lógica (MVP-206, CA-2). El Workspace nunca se borra físicamente: dejar de
    /// resolver contexto y de aparecer en el selector se decide por este campo.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>
    /// Quién dio de baja el Workspace. Es la única persona que puede autorizar su reactivación
    /// (CA-7/CA-10), por lo que la referencia se conserva mientras exista la cuenta.
    /// </summary>
    public Guid? DeletedByUserId { get; private set; }

    /// <summary>Atajo de lectura; no se persiste como columna propia.</summary>
    public bool IsDeleted => DeletedAt.HasValue;

    private Workspace() { }

    public static Workspace Create(Guid ownerId, string name)
    {
        if (ownerId == Guid.Empty)
            throw new WorkspaceValidationException(
                ErrorCodes.ValidationRequiredWorkspaceOwner,
                "El Workspace necesita un propietario válido.");

        var normalizedName = NormalizeName(name);
        var now = DateTimeOffset.UtcNow;

        return new Workspace
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = normalizedName,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Crea la membresía del propietario. Se emite desde el agregado para garantizar que
    /// ningún Workspace pueda existir sin miembro activo (CA-2 de MVP-102).
    /// </summary>
    public WorkspaceMember CreateOwnerMembership() => WorkspaceMember.CreateOwner(Id, OwnerId);

    /// <summary>
    /// MVP-206 (HU-1, CA-1) — Cambia el nombre con las mismas validaciones del alta (MVP-102).
    /// Permisos planos (RN-034): lo puede hacer cualquier miembro activo. No toca la sesión: el
    /// nombre no viaja en el token, así que el cambio se refleja sin reemitirla.
    /// </summary>
    public void Rename(string name)
    {
        EnsureNotDeleted();

        var normalizedName = NormalizeName(name);
        if (normalizedName == Name) return;

        Name = normalizedName;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// MVP-206 (HU-2, CA-2) — Baja lógica. Los datos siguen en base de datos; lo único que cambia es
    /// que el Workspace deja de resolver contexto y de aparecer en el selector (CA-8).
    /// </summary>
    public void SoftDelete(Guid deletedByUserId, DateTimeOffset moment)
    {
        EnsureNotDeleted();

        if (deletedByUserId == Guid.Empty)
            throw new WorkspaceValidationException(
                ErrorCodes.ValidationRequiredWorkspaceOwner,
                "La baja del Workspace necesita un usuario válido.");

        DeletedAt = moment;
        DeletedByUserId = deletedByUserId;
        UpdatedAt = moment;
    }

    /// <summary>
    /// MVP-206 (HU-6, CA-7) — Reactiva un Workspace dado de baja al autorizarse una solicitud de
    /// traspaso. Vuelve a ser visible y operable, con los datos que nunca se llegaron a borrar.
    /// </summary>
    public void Reactivate()
    {
        if (!IsDeleted)
            throw new WorkspaceMemberException(
                ErrorCodes.BusinessRuleWorkspaceNotDeleted,
                "Este Workspace no está dado de baja.");

        DeletedAt = null;
        DeletedByUserId = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// MVP-206 (HU-3/HU-4, CA-4/CA-5) — Traspasa la propiedad a otra persona. El agregado solo
    /// gobierna <c>owner_id</c>; el caso de uso actualiza en la misma transacción los roles de las
    /// membresías implicadas, de forma que el Workspace nunca queda sin propietario.
    /// </summary>
    public void TransferOwnershipTo(Guid newOwnerUserId)
    {
        if (newOwnerUserId == Guid.Empty)
            throw new WorkspaceValidationException(
                ErrorCodes.ValidationRequiredWorkspaceOwner,
                "El Workspace necesita un propietario válido.");

        if (newOwnerUserId == OwnerId)
            throw new WorkspaceMemberException(
                ErrorCodes.BusinessRuleOwnershipTransferToSelf,
                "Esa persona ya es la propietaria del Workspace.");

        OwnerId = newOwnerUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new WorkspaceMemberException(
                ErrorCodes.BusinessRuleWorkspaceDeleted,
                "Este Workspace está dado de baja.");
    }

    private static string NormalizeName(string name)
    {
        var normalizedName = (name ?? string.Empty).Trim();

        if (normalizedName.Length == 0)
            throw new WorkspaceValidationException(
                ErrorCodes.ValidationRequiredWorkspaceName,
                "El nombre del Workspace es obligatorio.");

        if (normalizedName.Length > NameMaxLength)
            throw new WorkspaceValidationException(
                ErrorCodes.ValidationWorkspaceNameLength,
                $"El nombre del Workspace no puede superar {NameMaxLength} caracteres.");

        return normalizedName;
    }
}
