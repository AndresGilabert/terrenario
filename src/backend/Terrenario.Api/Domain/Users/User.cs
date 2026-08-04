namespace Terrenario.Api.Domain.Users;

public sealed class User
{
    public Guid Id { get; private set; }
    public string GoogleSub { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    /// <summary>
    /// Último Workspace que el usuario dejó activo. Es lo que mantiene el contexto entre
    /// renovaciones de sesión y nuevos logins (MVP-104), donde el claim ya no viaja.
    /// </summary>
    public Guid? ActiveWorkspaceId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// MVP-505 (CA-3) — Instante en que la persona ejerció su derecho de supresión. La fila
    /// <b>sobrevive anonimizada</b>, no se borra: cada actividad, cosecha y compra guarda quién la
    /// registró, y borrarla dejaría el histórico operativo del Workspace sin autoría o lo arrastraría
    /// en cascada. Lo que desaparece son los <b>datos personales</b>, que es lo que el derecho exige.
    ///
    /// La fecha marca además el inicio del plazo de retención (RN-041): a su vencimiento la fila se
    /// purga físicamente.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    private User() { }

    public static User Create(string googleSub, string displayName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(googleSub);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return new User
        {
            Id = Guid.NewGuid(),
            GoogleSub = googleSub,
            DisplayName = displayName,
            Email = email,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateProfile(string displayName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        DisplayName = displayName;
        Email = email;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Texto con el que se sustituye el nombre de una cuenta eliminada, allí donde se muestre.</summary>
    public const string AnonymizedDisplayName = "Cuenta eliminada";

    /// <summary>
    /// MVP-505 (CA-3) — Ejercicio del <b>derecho de supresión</b>: borra los datos personales de la
    /// cuenta conservando la fila como referencia de autoría.
    ///
    /// Los tres campos identificativos se sustituyen por valores <b>derivados del id</b>, no vacíos,
    /// por dos motivos: siguen cumpliendo los índices únicos, y no pueden colisionar entre sí ni con
    /// una cuenta real.
    ///
    /// El <c>google_sub</c> es el importante: al dejar de coincidir con el que devuelve Google, si la
    /// persona vuelve a entrar con la misma cuenta el login <b>no la reconoce</b> y crea una cuenta
    /// nueva y limpia. Es lo que hace que la supresión sea de verdad y no una desactivación.
    /// El dominio <c>.invalid</c> está reservado por el RFC 2606: ese correo no puede existir.
    /// </summary>
    public void Anonymize(DateTimeOffset now)
    {
        if (IsDeleted) return;

        GoogleSub = $"deleted:{Id}";
        DisplayName = AnonymizedDisplayName;
        Email = $"deleted+{Id}@terrenario.invalid";
        IsActive = false;
        ActiveWorkspaceId = null;
        DeletedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Recuerda el Workspace sobre el que opera el usuario. La pertenencia se valida antes de
    /// llamar aquí: el agregado solo guarda la preferencia, no concede acceso.
    /// </summary>
    public void SetActiveWorkspace(Guid workspaceId)
    {
        if (ActiveWorkspaceId == workspaceId) return;

        ActiveWorkspaceId = workspaceId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
