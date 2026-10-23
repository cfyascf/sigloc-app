using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class ContractorConfiguration : IEntityTypeConfiguration<Contractor>
{
    public void Configure(EntityTypeBuilder<Contractor> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Cnpj).IsRequired();
        builder.Property(c => c.CompanyName).IsRequired();
        builder.Property(c => c.LogisticsContactName).HasMaxLength(120);
        builder.Property(c => c.LogisticsContactPhone).HasMaxLength(20);
        builder.Property(c => c.LogisticsContactEmail).HasMaxLength(254);

        builder.HasIndex(c => c.Cnpj).IsUnique();
    }
}
