using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Contexts;

public class SiglocDbContext : DbContext
{
    public SiglocDbContext(DbContextOptions<SiglocDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
  
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SiglocDbContext).Assembly);
    }
}
