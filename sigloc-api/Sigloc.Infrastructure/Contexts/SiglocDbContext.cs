using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Contexts;

public class SiglocDbContext : DbContext
{
    public SiglocDbContext(DbContextOptions<SiglocDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Contractor> Contractors => Set<Contractor>();
    public DbSet<Carrier> Carriers => Set<Carrier>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PartnershipInvite> PartnershipInvites => Set<PartnershipInvite>();
    public DbSet<PartnerConnection> PartnerConnections => Set<PartnerConnection>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<RouteSegment> RouteSegments => Set<RouteSegment>();
    public DbSet<ProductRouteSegment> ProductRouteSegments => Set<ProductRouteSegment>();
    public DbSet<ConsolidatedRoute> ConsolidatedRoutes => Set<ConsolidatedRoute>();
    public DbSet<Auction> Auctions => Set<Auction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
  
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SiglocDbContext).Assembly);

        // Converting every DateTimeOffset property to UTC before saving to the database
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var dateTimeOffsetProperties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?));

            foreach (var property in dateTimeOffsetProperties)
            {
                property.SetValueConverter(
                    new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, DateTimeOffset>(
                        v => v.ToUniversalTime(),
                        v => v
                    )
                );
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTimeOffset.UtcNow;

        var modifiedEntries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

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
