using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;
namespace Sigloc.Infrastructure.Configurations;

public class TripStopConfiguration : IEntityTypeConfiguration<TripStop>
{
    public void Configure(EntityTypeBuilder<TripStop> b)
    {
        b.ToTable("TripStop"); b.HasKey(x => x.Id);
        b.Property(x => x.City).HasMaxLength(200); b.Property(x => x.State).HasMaxLength(100);
        b.Property(x => x.Address).HasMaxLength(500); b.Property(x => x.CompletionSource).HasMaxLength(30);
        b.HasOne<Trip>().WithMany(t => t.Stops).HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.TripId, x.Sequence }).IsUnique();
    }
}
public class TripStopActionConfiguration : IEntityTypeConfiguration<TripStopAction>
{
    public void Configure(EntityTypeBuilder<TripStopAction> b)
    {
        b.ToTable("TripStopAction"); b.HasKey(x => x.Id);
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ProductName).HasMaxLength(500); b.Property(x => x.CompletionSource).HasMaxLength(30);
        b.HasOne<TripStop>().WithMany(s => s.Actions).HasForeignKey(x => x.TripStopId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<RouteSegment>().WithMany().HasForeignKey(x => x.SegmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TripStopId, x.SegmentId, x.Kind, x.ProductId }).IsUnique().AreNullsDistinct(false);
    }
}
public class TripTelemetryConfiguration : IEntityTypeConfiguration<TripTelemetry>
{
    public void Configure(EntityTypeBuilder<TripTelemetry> b)
    {
        b.ToTable("TripTelemetry"); b.HasKey(x => x.Id);
        b.Property(x => x.ObservationId).HasMaxLength(128);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.TripId, x.DeviceId, x.ObservationId }).IsUnique();
        b.HasIndex(x => new { x.TripId, x.FixTime });
    }
}
public class TripMonitoringEventConfiguration : IEntityTypeConfiguration<TripMonitoringEvent>
{
    public void Configure(EntityTypeBuilder<TripMonitoringEvent> b)
    {
        b.ToTable("TripMonitoringEvent"); b.HasKey(x => x.Id);
        b.Property(x => x.Kind).HasMaxLength(40); b.Property(x => x.Description).HasMaxLength(500);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.TripId, x.OccurredAt });
        b.HasIndex(x => new { x.TripId, x.RefreshId, x.Kind }).IsUnique();
    }
}
public class VehicleMonitoringConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> b) => b.Property(x => x.DriverPhone).HasMaxLength(40);
}
