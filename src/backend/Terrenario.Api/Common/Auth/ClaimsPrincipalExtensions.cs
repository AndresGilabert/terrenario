using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Terrenario.Api.Infrastructure.Auth;

namespace Terrenario.Api.Common.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
        => ParseGuid(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

    /// <summary>Workspace activo de la sesión; <c>null</c> mientras el usuario no tenga ninguno.</summary>
    public static Guid? GetWorkspaceId(this ClaimsPrincipal principal)
        => ParseGuid(principal.FindFirst(TerrenarioClaims.WorkspaceId)?.Value);

    public static string? GetDisplayName(this ClaimsPrincipal principal)
        => principal.FindFirst(JwtRegisteredClaimNames.Name)?.Value;

    private static Guid? ParseGuid(string? value)
        => Guid.TryParse(value, out var parsed) ? parsed : null;
}
