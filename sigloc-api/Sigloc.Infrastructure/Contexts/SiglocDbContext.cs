using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Contexts;

public class SiglocDbContext : DbContext
{
    public SiglocDbContext(DbContextOptions<SiglocDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ConsolidatedRoute> ConsolidatedRoutes => Set<ConsolidatedRoute>();
    public DbSet<Auction> Auctions => Set<Auction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
  
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SiglocDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
