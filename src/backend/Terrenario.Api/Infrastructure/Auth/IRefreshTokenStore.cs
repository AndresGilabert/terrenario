namespace Terrenario.Api.Infrastructure.Auth;

public interface IRefreshTokenStore
{
    Task<string> CreateAsync(Guid userId, CancellationToken ct = default);
    Task<Guid> ValidateAndRotateAsync(string token, CancellationToken ct = default);
    Task RevokeAsync(string token, CancellationToken ct = default);
}
