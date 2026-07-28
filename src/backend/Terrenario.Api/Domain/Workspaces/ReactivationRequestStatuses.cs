namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Catálogo cerrado <c>reactivation_request_status</c> (MVP-206). Como en las invitaciones, la
/// caducidad no es un estado persistido: se deriva de <c>expires_at</c> para no depender de un
/// proceso en segundo plano. Los valores van en español por ser vocabulario de dominio (ADR-0009).
/// </summary>
public static class ReactivationRequestStatuses
{
    /// <summary>Enlace emitido y todavía sin usar: nadie ha solicitado la reactivación aún.</summary>
    public const string Pending = "pendiente";

    /// <summary>La persona destinataria usó el enlace y pidió el traspaso: falta autorizarlo.</summary>
    public const string Requested = "solicitada";

    /// <summary>Quien dio de baja autorizó: el Workspace se reactivó y la propiedad pasó al solicitante.</summary>
    public const string Authorized = "autorizada";

    /// <summary>Quien dio de baja rechazó la solicitud. El Workspace sigue dado de baja.</summary>
    public const string Denied = "denegada";

    /// <summary>
    /// El enlace deja de servir sin que nadie lo rechazara: el Workspace ya volvió por otra
    /// solicitud. Se distingue de <see cref="Denied"/> para no atribuir una decisión que no hubo.
    /// </summary>
    public const string Closed = "cerrada";
}
