using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.AgreedValue).HasPrecision(18, 2);
        builder.Property(t => t.FinalAnttFloor).HasPrecision(18, 2);

        // Persist the status enum as its string name for readability.
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.HasOne<ConsolidatedRoute>()
            .WithMany()
            .HasForeignKey(t => t.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Auction>()
            .WithMany()
            .HasForeignKey(t => t.AuctionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Carrier>()
            .WithMany()
            .HasForeignKey(t => t.CarrierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Vehicle>()
            .WithMany()
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        // One trip per auction (the award closes the auction exactly once).
        builder.HasIndex(t => t.AuctionId).IsUnique();
        builder.HasIndex(t => t.CarrierId);
        builder.HasIndex(t => new { t.RouteId, t.Status, t.CreatedAt });
    }
}
