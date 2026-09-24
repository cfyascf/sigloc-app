using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Plate).IsRequired();
        builder.Property(v => v.Model).IsRequired();

        // Persist enums as their string names for readability.
        builder.Property(v => v.BodyType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(v => v.RefrigerationLevel)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .IsRequired();

        // Plate is unique per carrier.
        builder.HasIndex(v => new { v.CarrierId, v.Plate }).IsUnique();
    }
}
