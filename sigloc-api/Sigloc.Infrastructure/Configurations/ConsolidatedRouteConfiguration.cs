using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class ConsolidatedRouteConfiguration : IEntityTypeConfiguration<ConsolidatedRoute>
{
    public void Configure(EntityTypeBuilder<ConsolidatedRoute> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.ConsolidatedBudgetCeiling).HasPrecision(12, 2);
        builder.Property(r => r.EstimatedAnttFloor).HasPrecision(12, 2);

        builder.HasIndex(r => r.ContractorId);
        builder.HasIndex(r => r.Status);
    }
}
