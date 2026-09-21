using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class RouteSegmentConfiguration : IEntityTypeConfiguration<RouteSegment>
{
    public void Configure(EntityTypeBuilder<RouteSegment> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.OriginAddress).IsRequired();
        builder.Property(s => s.DestinationAddress).IsRequired();
        builder.Property(s => s.OriginCoordinate).IsRequired();
        builder.Property(s => s.DestinationCoordinate).IsRequired();

        // Persist the status enum as its string name for readability.
        builder.Property(s => s.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.HasIndex(s => s.ContractorId);
        builder.HasIndex(s => s.Status);

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.RouteSegmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
