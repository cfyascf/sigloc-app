using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sigloc.Infrastructure.Contexts;

/// <summary>
/// Design-time factory used by the EF Core tools (migrations) so they don't have
/// to build the full application host. The app itself registers the DbContext via
/// <c>AddInfrastructure</c>; this only exists for <c>dotnet ef</c>. The connection
/// string is a placeholder: generating migrations builds the model diff offline
/// and does not open a database connection.
/// </summary>
public class SiglocDbContextFactory : IDesignTimeDbContextFactory<SiglocDbContext>
{
    public SiglocDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=sigloc;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<SiglocDbContext>()
            .UseNpgsql(connectionString);

        return new SiglocDbContext(optionsBuilder.Options);
    }
}
