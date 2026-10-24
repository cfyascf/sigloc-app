using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type).HasMaxLength(40).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(120).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(500).IsRequired();
        builder.Property(n => n.Link).HasMaxLength(200);

        // The recipient is a contractor or a carrier (two tables), so there is no foreign key.
        // The index serves the bell's "newest first" list and its unread count.
        builder.HasIndex(n => new { n.RecipientCompanyId, n.CreatedAt });
    }
}
