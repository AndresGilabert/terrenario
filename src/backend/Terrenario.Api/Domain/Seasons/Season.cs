using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Domain.Seasons;

/// <summary>
/// Temporada (campaña) operativa de un Workspace. Es el eje temporal al que toda actividad, cosecha
/// y compra del MVP queda asociada (RN-021).
///
/// MVP-201 introdujo el agregado; MVP-203 lo completó como maestro. <b>MVP-209 separó dos conceptos que
/// el modelo fundía</b> en el antiguo booleano <c>is_active</c>:
/// <list type="bullet">
///   <item>El <b>estado</b> (informativo) de la temporada, que ahora vive aquí y se deriva de
///   <see cref="IsClosed"/> y de <see cref="StartDate"/> frente a hoy (<see cref="StatusOn"/>).</item>
///   <item>La <b>temporada de trabajo</b> —sobre cuál se registra por defecto—, que pasó a ser <b>por
///   usuario</b> y vive en <c>workspace_members.active_season_id</c>, fuera del agregado.</item>
/// </list>
/// El estado es independiente de la de trabajo: una campaña pasada no cerrada está <c>abierta</c>
/// (sigue recibiendo registros tardíos) aunque nadie trabaje sobre ella. Sobre las tres se puede añadir,
/// editar y borrar (RN-024).
/// </summary>
public sealed class Season
{
    public const int NameMaxLength = 120;

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsClosed { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Estado informativo derivado (MVP-209), <b>independiente</b> de la temporada de trabajo: cerrada
    /// si <see cref="IsClosed"/>; abierta si ya se inició (<c>start_date &lt;= reference</c>);
    /// planificada si aún no. Recibe la fecha de referencia («hoy») en vez de leer el reloj, para no
    /// acoplar el dominio al tiempo y poder probarlo de forma determinista.
    /// </summary>
    public SeasonStatus StatusOn(DateOnly reference) =>
        IsClosed ? SeasonStatus.Cerrada
        : StartDate <= reference ? SeasonStatus.Abierta
        : SeasonStatus.Planificada;

    private Season() { }

    /// <summary>
    /// Crea una temporada para el Workspace. Nace <b>abierta o planificada</b> según su fecha de inicio
    /// (nunca cerrada). Que además pase a ser la temporada de trabajo del creador (P-017, ya por usuario)
    /// lo decide el caso de uso, no el agregado: la temporada ya no conoce el concepto de «activa».
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
            IsClosed = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Edita los datos descriptivos de la temporada (MVP-203, HU-1/CA-2). No cierra ni reabre: para eso
    /// están <see cref="Close"/> y <see cref="Reopen"/>. La fecha de fin sigue siendo opcional y
    /// flexible (no se bloquea por rango operativo: RN-023 es un aviso de las historias operativas, no
    /// del maestro). Editar la fecha de inicio puede cambiar el estado derivado (abierta/planificada).
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
    /// Cierra la temporada (estado informativo <c>cerrada</c>, RN-024). No toca la temporada de trabajo
    /// de nadie: desde MVP-209 esa es una preferencia por usuario, no un flag del agregado. Cerrar
    /// significa «ya no espero más registros aquí», pero sigue siendo editable si hiciera falta.
    /// </summary>
    public void Close()
    {
        if (IsClosed) return;
        IsClosed = true;
        Touch();
    }

    /// <summary>
    /// Reabre una temporada cerrada. Vuelve a <c>abierta</c> o <c>planificada</c> según su fecha de
    /// inicio (lo decide <see cref="StatusOn"/>); no la fija como temporada de trabajo de nadie.
    /// </summary>
    public void Reopen()
    {
        if (!IsClosed) return;
        IsClosed = false;
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

    /// <summary>
    /// Normaliza y valida un nombre de temporada <b>sin mutar</b> ningún agregado. Se expone para que
    /// la comprobación de duplicados del maestro (MVP-207, CA-2) trabaje sobre el mismo texto que
    /// acabará persistido (mismo recorte de espacios) y pueda hacerse antes de tocar la entidad.
    /// </summary>
    public static string NormalizeName(string name)
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
