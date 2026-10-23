using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;
namespace Sigloc.Infrastructure.Configurations;

public class TripProofOfDeliveryConfiguration : IEntityTypeConfiguration<TripProofOfDelivery>
{
    public void Configure(EntityTypeBuilder<TripProofOfDelivery> b)
    {
        b.ToTable("TripProofOfDelivery"); b.HasKey(x => x.Id);
        b.Property(x => x.StorageKey).HasMaxLength(300); b.Property(x => x.FileName).HasMaxLength(255);
        b.Property(x => x.ContentType).HasMaxLength(100);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<TripStop>().WithMany().HasForeignKey(x => x.TripStopId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.StorageKey).IsUnique();
        b.HasIndex(x => new { x.TripId, x.TripStopId });
    }
}
