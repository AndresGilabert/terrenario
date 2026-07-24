namespace Terrenario.Api.Infrastructure.Invitations;

/// <summary>
/// Los emails no se registran completos en logs
/// (<c>docs/07-seguridad/autenticacion-autorizacion.md</c>).
/// </summary>
public static class EmailMasking
{
    public static string Mask(string email)
    {
        var separatorIndex = email.IndexOf('@');

        return separatorIndex <= 0
            ? "***"
            : $"{email[0]}***{email[separatorIndex..]}";
    }
}
