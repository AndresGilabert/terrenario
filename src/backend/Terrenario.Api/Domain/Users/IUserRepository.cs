namespace Terrenario.Api.Domain.Users;

public interface IUserRepository
{
    Task<User?> FindByGoogleSubAsync(string googleSub, CancellationToken ct = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
