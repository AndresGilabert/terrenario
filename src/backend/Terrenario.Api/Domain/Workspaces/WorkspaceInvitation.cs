using System.Net.Mail;
using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// MVP-103 — Invitación a un Workspace por email o por enlace compartible (RN-035).
/// El token solo se persiste como hash: quien emite la invitación es el único que ve el valor
/// en claro, y una vez entregado no puede reconstruirse desde la base de datos.
/// </summary>
public sealed class WorkspaceInvitation
{
    public const int EmailMaxLength = 320;

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid InvitedByUserId { get; private set; }
    public string Channel { get; private set; } = InvitationChannels.Link;
    public string? Email { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string Status { get; private set; } = InvitationStatuses.Pending;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public Guid? AcceptedByUserId { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public Guid? RejectedByUserId { get; private set; }

    private WorkspaceInvitation() { }

    public static WorkspaceInvitation Create(
        Guid workspaceId,
        Guid invitedByUserId,
        string channel,
        string? email,
        string tokenHash,
        TimeSpan lifetime)
    {
        if (workspaceId == Guid.Empty || invitedByUserId == Guid.Empty)
            throw new InvitationException(
                ErrorCodes.ValidationRequiredInvitationContext,
                "La invitación necesita un Workspace y un emisor válidos.");

        if (!InvitationChannels.IsValid(channel))
            throw new InvitationException(
                ErrorCodes.ValidationInvitationChannelInvalid,
                "El canal de invitación debe ser 'email' o 'enlace'.");

        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        var now = DateTimeOffset.UtcNow;

        return new WorkspaceInvitation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            InvitedByUserId = invitedByUserId,
            Channel = channel,
            // El enlace compartible no va dirigido a nadie en concreto: no guarda destinatario.
            Email = channel == InvitationChannels.Email ? NormalizeEmail(email) : null,
            TokenHash = tokenHash,
            Status = InvitationStatuses.Pending,
            ExpiresAt = now.Add(lifetime),
            CreatedAt = now
        };
    }

    public bool IsExpiredAt(DateTimeOffset moment) => moment >= ExpiresAt;

    /// <summary>
    /// Indica si la cuenta autenticada es apta para actuar sobre esta invitación. El enlace
    /// compartible (MVP-103) no va dirigido a nadie, así que lo acepta cualquier usuario; la
    /// invitación por email solo la acepta su destinatario. Se usa para informar en el preview
    /// (MVP-107, R-C) sin filtrar el email destinatario: solo se compara, no se revela.
    /// </summary>
    public bool IsAddressedTo(string? userEmail) =>
        Channel != InvitationChannels.Email || Email == Canonicalize(userEmail);

    /// <summary>
    /// Marca la invitación como aceptada por un usuario autenticado. No crea la membresía:
    /// el caso de uso decide si hay que emitirla o si el usuario ya era miembro del Workspace.
    /// </summary>
    public void Accept(Guid userId, string userEmail, DateTimeOffset moment)
    {
        EnsurePending();

        if (IsExpiredAt(moment))
            throw new InvitationException(
                ErrorCodes.BusinessRuleInvitationExpired,
                "Esta invitación ha caducado. Pide una nueva a quien te invitó.");

        // Una invitación por email va dirigida a una persona concreta: reenviar el correo no
        // debe abrir la puerta a un tercero. El enlace compartible sí acepta a cualquier
        // usuario autenticado, que es justo su propósito.
        if (!IsAddressedTo(userEmail))
            throw new InvitationException(
                ErrorCodes.AuthInvitationEmailMismatch,
                "Esta invitación está dirigida a otra cuenta de correo.");

        Status = InvitationStatuses.Accepted;
        AcceptedAt = moment;
        AcceptedByUserId = userId;
    }

    /// <summary>
    /// Declina la invitación sin crear membresía (MVP-107, HU-2/punto 6). No cierra sesión ni
    /// toca el Workspace: solo cambia el estado. Es idempotente ante un segundo rechazo del mismo
    /// destinatario (doble clic), pero una cuenta ajena sigue chocando con el desajuste de email.
    /// Rechazar una invitación caducada se permite: limpia la bandeja sin efecto colateral.
    /// </summary>
    public void Reject(Guid userId, string userEmail, DateTimeOffset moment)
    {
        if (Status == InvitationStatuses.Accepted)
            throw new InvitationException(
                ErrorCodes.BusinessRuleInvitationAlreadyAccepted,
                "Esta invitación ya se ha utilizado.");

        // El desajuste de email se comprueba antes de la idempotencia: un tercero con el correo
        // reenviado no debe poder declinar la invitación de otra persona.
        if (!IsAddressedTo(userEmail))
            throw new InvitationException(
                ErrorCodes.AuthInvitationEmailMismatch,
                "Esta invitación está dirigida a otra cuenta de correo.");

        if (Status == InvitationStatuses.Rejected) return;

        Status = InvitationStatuses.Rejected;
        RejectedAt = moment;
        RejectedByUserId = userId;
    }

    private void EnsurePending()
    {
        if (Status == InvitationStatuses.Accepted)
            throw new InvitationException(
                ErrorCodes.BusinessRuleInvitationAlreadyAccepted,
                "Esta invitación ya se ha utilizado.");

        if (Status == InvitationStatuses.Rejected)
            throw new InvitationException(
                ErrorCodes.BusinessRuleInvitationAlreadyRejected,
                "Esta invitación se ha rechazado y ya no está disponible.");
    }

    private static string Canonicalize(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static string NormalizeEmail(string? email)
    {
        var normalizedEmail = Canonicalize(email);

        if (normalizedEmail.Length == 0)
            throw new InvitationException(
                ErrorCodes.ValidationRequiredInvitationEmail,
                "El email de la persona invitada es obligatorio.");

        if (normalizedEmail.Length > EmailMaxLength || !IsWellFormed(normalizedEmail))
            throw new InvitationException(
                ErrorCodes.ValidationInvitationEmailInvalid,
                "El email de la persona invitada no tiene un formato válido.");

        return normalizedEmail;
    }

    private static bool IsWellFormed(string email) =>
        MailAddress.TryCreate(email, out var address) &&
        address.Address == email &&
        address.Host.Contains('.');
}
