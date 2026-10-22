using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;
namespace Sigloc.Infrastructure.Configurations;

public class TripOccurrenceConfiguration : IEntityTypeConfiguration<TripOccurrence>
{
    public void Configure(EntityTypeBuilder<TripOccurrence> b)
    {
        b.ToTable("TripOccurrence"); b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Severity).HasMaxLength(20); b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.ResolutionSource).HasMaxLength(30);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<TripStop>().WithMany().HasForeignKey(x => x.BlockingStopId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ReportedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TripId, x.ResolvedAt });
    }
}
