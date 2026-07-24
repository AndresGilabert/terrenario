namespace Terrenario.Api.Infrastructure.Invitations;

public sealed class InvitationOptions
{
    public const string SectionName = "Invitations";

    /// <summary>Vigencia de la invitación. MVP-103 deja fuera reenvíos y expiraciones configurables por usuario.</summary>
    public int LifetimeDays { get; set; } = 7;

    /// <summary>Base pública del enlace de aceptación; se le añade <c>/{token}</c>.</summary>
    public string AcceptBaseUrl { get; set; } = "http://localhost:5173/invitations";

    public TimeSpan Lifetime => TimeSpan.FromDays(LifetimeDays);

    public string BuildAcceptUrl(string token) => $"{AcceptBaseUrl.TrimEnd('/')}/{token}";
}
