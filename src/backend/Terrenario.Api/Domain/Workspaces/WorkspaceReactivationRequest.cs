using Terrenario.Api.Common.Errors;

namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// MVP-206 (HU-5/HU-6, CA-6/CA-7/CA-10) — Vía por la que un Workspace dado de baja puede volver.
/// Al darse de baja se emite una de estas solicitudes por cada miembro activo al que se notifica,
/// con un enlace de **un solo uso** y caducidad. Quien recibe el enlace puede **solicitar** el
/// traspaso y la reactivación; la solicitud solo la puede **autorizar quien dio de baja** el
/// Workspace. Reutiliza el patrón de tokens de las invitaciones (MVP-103): en base de datos vive
/// únicamente el hash, el valor en claro solo viaja en el email.
/// </summary>
public sealed class WorkspaceReactivationRequest
{
    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }

    /// <summary>Persona a la que se envió el enlace: la única que puede usarlo (CA-10).</summary>
    public Guid RecipientUserId { get; private set; }

    /// <summary>Quien dio de baja el Workspace: la única persona que puede autorizar (CA-10).</summary>
    public Guid AuthorizerUserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;
    public string Status { get; private set; } = ReactivationRequestStatuses.Pending;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RequestedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    private WorkspaceReactivationRequest() { }

    public static WorkspaceReactivationRequest Issue(
        Guid workspaceId,
        Guid recipientUserId,
        Guid authorizerUserId,
        string tokenHash,
        TimeSpan lifetime)
    {
        if (workspaceId == Guid.Empty || recipientUserId == Guid.Empty || authorizerUserId == Guid.Empty)
            throw new WorkspaceMemberException(
                ErrorCodes.ValidationRequiredReactivationContext,
                "La solicitud de reactivación necesita un Workspace, un destinatario y un autorizador válidos.");

        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        var now = DateTimeOffset.UtcNow;

        return new WorkspaceReactivationRequest
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            RecipientUserId = recipientUserId,
            AuthorizerUserId = authorizerUserId,
            TokenHash = tokenHash,
            Status = ReactivationRequestStatuses.Pending,
            ExpiresAt = now.Add(lifetime),
            CreatedAt = now
        };
    }

    public bool IsExpiredAt(DateTimeOffset moment) => moment >= ExpiresAt;

    /// <summary>
    /// Consume el enlace (un solo uso, CA-10) y deja la solicitud a la espera de autorización.
    /// Solo la persona destinataria puede hacerlo; un segundo intento ya no encuentra el estado
    /// <c>pendiente</c> y se rechaza.
    /// </summary>
    public void Submit(Guid requestedByUserId, DateTimeOffset moment)
    {
        if (requestedByUserId != RecipientUserId)
            throw new WorkspaceMemberException(
                ErrorCodes.ReactivationRequestNotFound,
                "Este enlace de reactivación está dirigido a otra persona.");

        if (Status != ReactivationRequestStatuses.Pending)
            throw new WorkspaceMemberException(
                ErrorCodes.BusinessRuleReactivationAlreadyUsed,
                "Este enlace de reactivación ya se ha utilizado.");

        if (IsExpiredAt(moment))
            throw new WorkspaceMemberException(
                ErrorCodes.BusinessRuleReactivationExpired,
                "Este enlace de reactivación ha caducado.");

        Status = ReactivationRequestStatuses.Requested;
        RequestedAt = moment;
    }

    /// <summary>
    /// Autoriza el traspaso y la reactivación (CA-7). Solo quien dio de baja el Workspace; nadie
    /// más puede reactivarlo por esta vía (CA-10). El caso de uso aplica el efecto sobre el
    /// Workspace y las membresías en la misma transacción.
    /// </summary>
    public void Authorize(Guid actingUserId, DateTimeOffset moment)
    {
        EnsureResolvableBy(actingUserId);

        Status = ReactivationRequestStatuses.Authorized;
        ResolvedAt = moment;
    }

    /// <summary>Deniega la solicitud (HU-6): el Workspace sigue dado de baja.</summary>
    public void Deny(Guid actingUserId, DateTimeOffset moment)
    {
        EnsureResolvableBy(actingUserId);

        Status = ReactivationRequestStatuses.Denied;
        ResolvedAt = moment;
    }

    /// <summary>
    /// Invalida el enlace sin decisión de nadie: el Workspace ya volvió por otra solicitud. Evita
    /// que un enlace antiguo pueda encadenar una segunda reactivación (CA-10).
    /// </summary>
    public void Close(DateTimeOffset moment)
    {
        if (Status is not (ReactivationRequestStatuses.Pending or ReactivationRequestStatuses.Requested))
            return;

        Status = ReactivationRequestStatuses.Closed;
        ResolvedAt = moment;
    }

    private void EnsureResolvableBy(Guid actingUserId)
    {
        // Se oculta como "no encontrada" para no revelar solicitudes de Workspaces ajenos.
        if (actingUserId != AuthorizerUserId)
            throw new WorkspaceMemberException(
                ErrorCodes.ReactivationRequestNotFound,
                "Esta solicitud de reactivación no existe o no puedes resolverla.");

        if (Status != ReactivationRequestStatuses.Requested)
            throw new WorkspaceMemberException(
                ErrorCodes.BusinessRuleReactivationNotRequested,
                "Esta solicitud ya se ha resuelto o todavía no se ha pedido.");
    }
}
