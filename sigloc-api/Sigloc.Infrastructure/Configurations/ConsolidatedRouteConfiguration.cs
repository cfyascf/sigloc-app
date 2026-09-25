using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class ConsolidatedRouteConfiguration : IEntityTypeConfiguration<ConsolidatedRoute>
{
    public void Configure(EntityTypeBuilder<ConsolidatedRoute> builder)
    {
        builder.HasKey(r => r.Id);

        // Persist the status enum as its string name for readability.
        builder.Property(r => r.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.HasIndex(r => r.ContractorId);
        builder.HasIndex(r => r.Status);
    }
}
