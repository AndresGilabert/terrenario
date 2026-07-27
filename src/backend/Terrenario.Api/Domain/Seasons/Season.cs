using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Domain.Seasons;

/// <summary>
/// Temporada (campaña) operativa de un Workspace. Es el eje temporal al que toda actividad, cosecha
/// y compra del MVP queda asociada (RN-021). En MVP solo puede haber una temporada activa por
/// Workspace (RN-022): la invariante la refuerza además un índice único parcial en persistencia.
///
/// MVP-201 introdujo el agregado y la creación de la (primera) temporada activa. MVP-203 lo completa
/// como maestro: alta de varias, edición, la máquina de estados planificada/activa/cerrada y el
/// cambio de temporada activa. No hay temporada por defecto: la creación es siempre un acto explícito
/// del usuario (cancelable en la UI).
///
/// Los estados se derivan de dos booleanos canónicos (<see cref="IsActive"/>, <see cref="IsClosed"/>,
/// convención `is_` del modelo de datos), sin columna de estado ni cambio de esquema (RN-022 sigue
/// materializada por el índice único parcial de "una activa por Workspace"):
/// <list type="bullet">
///   <item><c>cerrada</c> ≡ <see cref="IsClosed"/> (informativa, no bloquea — RN-024).</item>
///   <item><c>activa</c> ≡ <see cref="IsActive"/> y no cerrada.</item>
///   <item><c>planificada</c> ≡ ni activa ni cerrada.</item>
/// </list>
/// Las tres son mutuamente excluyentes porque las transiciones mantienen la invariante
/// "activa ⇒ no cerrada": <see cref="Close"/> desactiva y <see cref="Activate"/> reabre.
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

    /// <summary>Estado derivado de los booleanos canónicos (planificada/activa/cerrada).</summary>
    public SeasonStatus Status =>
        IsClosed ? SeasonStatus.Cerrada
        : IsActive ? SeasonStatus.Activa
        : SeasonStatus.Planificada;

    private Season() { }

    /// <summary>
    /// Crea una temporada activa para el Workspace (MVP-201). Nace activa (RN-021/RN-022) y abierta.
    /// La fecha de fin es estimada y opcional. La invariante de "una sola activa por Workspace" se
    /// garantiza al persistir (índice único parcial + <c>ISeasonRepository.ActivateExclusivelyAsync</c>),
    /// no en el agregado, que no conoce a las demás temporadas.
    /// </summary>
    public static Season Create(Guid workspaceId, string name, DateOnly startDate, DateOnly? endDate)
    {
        if (workspaceId == Guid.Empty)
            throw new SeasonValidationException(
                ErrorCodes.ValidationRequiredSeasonWorkspace,
                "La temporada necesita un Workspace válido.");

        var normalizedName = NormalizeName(name);
        ValidateRange(startDate, endDate);

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

    /// <summary>
    /// Edita los datos descriptivos de la temporada (MVP-203, HU-1/CA-2). No cambia el estado: para
    /// eso están <see cref="Activate"/>, <see cref="Close"/> y <see cref="Reopen"/>. La fecha de fin
    /// sigue siendo opcional y flexible (no se bloquea por rango operativo: RN-023 es un aviso de las
    /// historias operativas, no del maestro).
    /// </summary>
    public void UpdateDetails(string name, DateOnly startDate, DateOnly? endDate)
    {
        var normalizedName = NormalizeName(name);
        ValidateRange(startDate, endDate);

        Name = normalizedName;
        StartDate = startDate;
        EndDate = endDate;
        Touch();
    }

    /// <summary>
    /// Marca la temporada como activa (RN-022, MVP-203 HU-2). Reabre si estaba cerrada (una activa no
    /// puede estar cerrada). El desbanque de la activa anterior lo orquesta el repositorio para no
    /// violar el índice único parcial; el agregado solo fija su propio estado.
    /// </summary>
    public void Activate()
    {
        if (IsActive && !IsClosed) return;
        IsActive = true;
        IsClosed = false;
        Touch();
    }

    /// <summary>
    /// Cierra la temporada (estado informativo <c>cerrada</c>, RN-024). Cerrar la activa libera el
    /// hueco de temporada activa del Workspace (decisión de producto MVP-203): el Workspace queda sin
    /// activa y la UI ofrece activar otra o crear una nueva (coherente con la oferta de MVP-201).
    /// </summary>
    public void Close()
    {
        if (IsClosed && !IsActive) return;
        IsClosed = true;
        IsActive = false;
        Touch();
    }

    /// <summary>
    /// Reabre una temporada cerrada devolviéndola a <c>planificada</c> (no la activa por sí sola, para
    /// no provocar un cambio de activa inesperado; el usuario la activa explícitamente si quiere).
    /// </summary>
    public void Reopen()
    {
        if (!IsClosed) return;
        IsClosed = false;
        IsActive = false;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private static void ValidateRange(DateOnly startDate, DateOnly? endDate)
    {
        if (endDate is { } end && end < startDate)
            throw new SeasonValidationException(
                ErrorCodes.ValidationSeasonDateRange,
                "La fecha de fin no puede ser anterior a la fecha de inicio.");
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
