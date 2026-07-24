namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Error de dominio de una invitación. Transporta el código de error del contrato de API
/// (<c>docs/02-arquitectura/contratos-api.md</c>); la traducción a código HTTP se hace en el
/// controlador, no en el dominio.
/// </summary>
public sealed class InvitationException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
