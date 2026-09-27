using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.NetFreightValue).HasPrecision(18, 2);
        builder.Property(b => b.TollValue).HasPrecision(18, 2);
        builder.Property(b => b.TotalValue).HasPrecision(18, 2);

        // Persist the status enum as its string name for readability.
        builder.Property(b => b.Status)
            .HasConversion<string>()
            .IsRequired();

        // A bid competes in exactly one auction; deleting the auction removes its bids.
        builder.HasOne<Auction>()
            .WithMany()
            .HasForeignKey(b => b.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Carrier>()
            .WithMany()
            .HasForeignKey(b => b.CarrierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Vehicle>()
            .WithMany()
            .HasForeignKey(b => b.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.AuctionId);
        builder.HasIndex(b => b.Status);
    }
}
