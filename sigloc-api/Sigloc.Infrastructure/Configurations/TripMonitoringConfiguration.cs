using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class TripMonitoringConfiguration : IEntityTypeConfiguration<TripMonitoring>
{
    public void Configure(EntityTypeBuilder<TripMonitoring> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.LastProgressPercentage).IsRequired();
        builder.Property(m => m.LastCalculatedEta).IsRequired(false);
        builder.Property(m => m.Risk).HasMaxLength(30);
        builder.Property(m => m.LastObservationId).HasMaxLength(128);
        builder.Property(m => m.LastPingAt).IsRequired();

        // One monitoring snapshot per trip; removing the trip removes its snapshot.
        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(m => m.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.TripId).IsUnique();
    }
}
