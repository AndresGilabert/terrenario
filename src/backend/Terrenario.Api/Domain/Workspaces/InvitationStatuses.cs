namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Catálogo cerrado <c>invitation_status</c>. La invitación caducada no es un estado
/// persistido: se deriva de <c>expires_at</c> para no depender de un proceso en segundo plano.
/// El rechazo (MVP-107) sí es un estado explícito: declina la invitación sin crear membresía.
///
/// <c>anulada</c> (MVP-207, CA-4) es la contrapartida del rechazo vista desde el otro lado: la
/// retira el <b>Workspace emisor</b>, no la persona invitada. Ambas dejan la invitación inservible,
/// pero son transiciones distintas y conviene distinguirlas para saber quién la cerró.
/// </summary>
public static class InvitationStatuses
{
    public const string Pending = "pendiente";
    public const string Accepted = "aceptada";
    public const string Rejected = "rechazada";
    public const string Cancelled = "anulada";
}
