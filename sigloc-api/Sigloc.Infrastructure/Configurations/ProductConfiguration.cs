using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku).IsRequired();
        builder.Property(p => p.Name).IsRequired();

        // Persist enums as their string names for readability.
        builder.Property(p => p.Category)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.TransportEnvironment)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.PackagingType)
            .HasConversion<string>();

        // SKU is unique per contractor.
        builder.HasIndex(p => new { p.ContractorId, p.Sku }).IsUnique();
    }
}
