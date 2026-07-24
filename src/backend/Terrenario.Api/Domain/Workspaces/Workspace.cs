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
