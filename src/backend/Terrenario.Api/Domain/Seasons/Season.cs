using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Domain.Seasons;

/// <summary>
/// Temporada (campaña) operativa de un Workspace. Es el eje temporal al que toda actividad, cosecha
/// y compra del MVP queda asociada (RN-021). En MVP solo puede haber una temporada activa por
/// Workspace (RN-022): la invariante la refuerza además un índice único parcial en persistencia.
///
/// MVP-201 introduce el agregado y la creación de la (primera) temporada activa. No hay temporada
/// por defecto: la creación es siempre un acto explícito del usuario (cancelable en la UI). El
/// maestro completo (alta de varias, edición, listado y la máquina de estados
/// planificada/activa/cerrada) es alcance de MVP-203; aquí los estados se representan con dos
/// booleanos canónicos (<see cref="IsActive"/>, <see cref="IsClosed"/>, convención `is_` del modelo
/// de datos), sobre los que MVP-203 podrá derivar esos estados sin cambiar el esquema:
/// <list type="bullet">
///   <item><c>cerrada</c> ≡ <see cref="IsClosed"/> (informativa, no bloquea — RN-024).</item>
///   <item><c>activa</c> ≡ <see cref="IsActive"/> y no cerrada.</item>
///   <item><c>planificada</c> ≡ ni activa ni cerrada.</item>
/// </list>
/// </summary>
public sealed class Season
{
    public const int NameMaxLength = 120;

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsClosed { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Season() { }

    /// <summary>
    /// Crea una temporada activa para el Workspace (MVP-201). Es la (primera) temporada del
    /// Workspace: nace activa (RN-021/RN-022) y abierta. La fecha de fin es estimada y opcional.
    /// </summary>
    public static Season Create(Guid workspaceId, string name, DateOnly startDate, DateOnly? endDate)
    {
        if (workspaceId == Guid.Empty)
            throw new SeasonValidationException(
                ErrorCodes.ValidationRequiredSeasonWorkspace,
                "La temporada necesita un Workspace válido.");

        var normalizedName = NormalizeName(name);

        if (endDate is { } end && end < startDate)
            throw new SeasonValidationException(
                ErrorCodes.ValidationSeasonDateRange,
                "La fecha de fin no puede ser anterior a la fecha de inicio.");

        var now = DateTimeOffset.UtcNow;

        return new Season
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = normalizedName,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
            IsClosed = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static string NormalizeName(string name)
    {
        var normalizedName = (name ?? string.Empty).Trim();

        if (normalizedName.Length == 0)
            throw new SeasonValidationException(
                ErrorCodes.ValidationRequiredSeasonName,
                "El nombre de la temporada es obligatorio.");

        if (normalizedName.Length > NameMaxLength)
            throw new SeasonValidationException(
                ErrorCodes.ValidationSeasonNameLength,
                $"El nombre de la temporada no puede superar {NameMaxLength} caracteres.");

        return normalizedName;
    }
}
