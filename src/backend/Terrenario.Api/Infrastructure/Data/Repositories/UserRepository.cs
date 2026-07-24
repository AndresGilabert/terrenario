using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Users;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

public sealed class UserRepository(TerrenarioDbContext db) : IUserRepository
{
    public Task<User?> FindByGoogleSubAsync(string googleSub, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.GoogleSub == googleSub, ct);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await db.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
