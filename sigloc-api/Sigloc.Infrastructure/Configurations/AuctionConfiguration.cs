using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.HasKey(a => a.Id);

        // Optional custom label; null falls back to the itinerary-derived name.
        builder.Property(a => a.Name)
            .HasMaxLength(200);

        // Persist the status enum as its string name for readability.
        builder.Property(a => a.Status)
            .HasConversion<string>()
            .IsRequired();

        // One auction per consolidated route (1:1).
        builder.HasOne<ConsolidatedRoute>()
            .WithOne()
            .HasForeignKey<Auction>(a => a.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.RouteId).IsUnique();
        builder.HasIndex(a => a.Status);
    }
}
