using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class PartnerConnectionConfiguration : IEntityTypeConfiguration<PartnerConnection>
{
    public void Configure(EntityTypeBuilder<PartnerConnection> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .IsRequired();

        // A contractor and a carrier can only be linked once.
        builder.HasIndex(c => new { c.ContractorId, c.CarrierId }).IsUnique();
    }
}
