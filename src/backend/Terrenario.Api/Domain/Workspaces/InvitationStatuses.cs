namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Catálogo cerrado <c>invitation_status</c>. La invitación caducada no es un estado
/// persistido: se deriva de <c>expires_at</c> para no depender de un proceso en segundo plano.
/// El rechazo (MVP-107) sí es un estado explícito: declina la invitación sin crear membresía.
/// </summary>
public static class InvitationStatuses
{
    public const string Pending = "pendiente";
    public const string Accepted = "aceptada";
    public const string Rejected = "rechazada";
}
