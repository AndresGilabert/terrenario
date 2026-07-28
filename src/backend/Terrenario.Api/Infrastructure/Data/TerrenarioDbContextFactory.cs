using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Terrenario.Api.Infrastructure.Data;

public sealed class TerrenarioDbContextFactory : IDesignTimeDbContextFactory<TerrenarioDbContext>
{
    public TerrenarioDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TerrenarioDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=terrenario_dev;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new TerrenarioDbContext(optionsBuilder.Options);
    }
}
