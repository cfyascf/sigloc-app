using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class BlockedBidAttemptConfiguration : IEntityTypeConfiguration<BlockedBidAttempt>
{
    public void Configure(EntityTypeBuilder<BlockedBidAttempt> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AttemptedAt).IsRequired();

        // Persist the reason enum as its string name for readability.
        builder.Property(a => a.Reason)
            .HasConversion<string>()
            .IsRequired();

        builder.HasOne<Contractor>()
            .WithMany()
            .HasForeignKey(a => a.ContractorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Auction>()
            .WithMany()
            .HasForeignKey(a => a.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite index supports the monthly per-contractor count query.
        builder.HasIndex(a => new { a.ContractorId, a.AttemptedAt });
    }
}
