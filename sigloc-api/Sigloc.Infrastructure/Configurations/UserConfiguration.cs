using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();

        // Persist the profile enum as its string name for readability.
        builder.Property(u => u.ProfileType)
            .HasConversion<string>()
            .IsRequired();

        // Email is the login credential and must be globally unique.
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
