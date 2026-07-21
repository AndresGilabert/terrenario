namespace Terrenario.Api.Infrastructure.Auth;

public sealed record IssuedAccessToken(string Token, int ExpiresIn);

public interface IJwtService
{
    IssuedAccessToken IssueAccessToken(Guid userId, string? displayName);
    Guid? ValidateAccessToken(string token);
}
