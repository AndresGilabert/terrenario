namespace Terrenario.Api.Domain.Workspaces;

/// <summary>
/// Catálogo cerrado <c>invitation_channel</c>. Los valores son vocabulario de dominio y se
/// mantienen en español según <c>docs/04-ingenieria/estandares-codigo.md</c>.
/// </summary>
public static class InvitationChannels
{
    public const string Email = "email";
    public const string Link = "enlace";

    public static bool IsValid(string? channel) => channel is Email or Link;
}
