namespace Terrenario.Api.Domain.Users;

public interface IUserRepository
{
    Task<User?> FindByGoogleSubAsync(string googleSub, CancellationToken ct = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>La comparación es insensible a mayúsculas: el email se guarda tal cual lo da Google.</summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
