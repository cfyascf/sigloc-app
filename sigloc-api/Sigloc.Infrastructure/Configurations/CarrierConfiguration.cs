using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class CarrierConfiguration : IEntityTypeConfiguration<Carrier>
{
    public void Configure(EntityTypeBuilder<Carrier> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Cnpj).IsRequired();
        builder.Property(c => c.CompanyName).IsRequired();

        builder.HasIndex(c => c.Cnpj).IsUnique();
    }
}
