using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class PartnershipInviteConfiguration : IEntityTypeConfiguration<PartnershipInvite>
{
    public void Configure(EntityTypeBuilder<PartnershipInvite> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Token).IsRequired();

        builder.Property(i => i.InviteeEmail).HasMaxLength(320);

        builder.HasIndex(i => i.Token).IsUnique();
        builder.HasIndex(i => i.ContractorId);
    }
}
