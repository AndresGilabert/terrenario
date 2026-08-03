using Microsoft.EntityFrameworkCore;
using Terrenario.Api.Domain.Users;

namespace Terrenario.Api.Infrastructure.Data.Repositories;

public sealed class UserRepository(TerrenarioDbContext db) : IUserRepository
{
    /// <summary>
    /// MVP-505 (CA-3) — Una cuenta dada de baja **no se reconoce**: si la persona vuelve a entrar con
    /// la misma cuenta de Google, el login crea una cuenta nueva y limpia. La anonimización ya cambia
    /// el <c>google_sub</c>, así que este filtro es defensa en profundidad: que la supresión no
    /// dependa de un solo mecanismo.
    /// </summary>
    public Task<User?> FindByGoogleSubAsync(string googleSub, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.GoogleSub == googleSub && u.DeletedAt == null, ct);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    /// <summary>La aptitud de una invitación no puede resolverse contra una cuenta dada de baja.</summary>
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.DeletedAt == null, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await db.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
